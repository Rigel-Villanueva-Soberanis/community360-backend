using System.Text.Json;
using Application.Common.Utilidades;
using Application.Interfaces;
using Couchbase;
using Couchbase.Query;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Lecturas N1QL de Couchbase. Las rutas de colección salen de constantes del código; todo valor del cliente
/// (estado, operación, id, límite, desplazamiento) viaja como parámetro con nombre ($estado, $limite...), nunca concatenado.
/// Cada documento se pide como texto JSON (ENCODE_JSON) para devolverlo tal cual, sin depender de su forma.
/// </summary>
public class AuditoriaRepository(CouchbaseContext cb) : IAuditoriaRepository
{
    private async Task<IReadOnlyList<JsonElement>> Filas(string n1ql, QueryOptions opt)
    {
        var cluster = await cb.ClusterAsync();
        var r = await cluster.QueryAsync<string>(n1ql, opt);
        var lista = new List<JsonElement>();
        await foreach (var s in r.Rows)
        {
            using var d = JsonDocument.Parse(s);
            lista.Add(d.RootElement.Clone());
        }
        return lista;
    }

    private async Task<long> Contar(string coleccion, string filtro, QueryOptions opt)
    {
        var cluster = await cb.ClusterAsync();
        var r = await cluster.QueryAsync<long>($"SELECT RAW COUNT(*) FROM {cb.Ruta(coleccion)} c {filtro}", opt);
        await foreach (var n in r.Rows) return n;
        return 0;
    }

    public async Task<(IReadOnlyList<JsonElement> Items, long Total)> ListarCuarentenaAsync(string? estado, int limite, int offset, CancellationToken ct)
    {
        var filtro = estado is null ? "" : "WHERE c.estado_revision = $estado";          // fragmento fijo
        var opt = new QueryOptions().Parameter("limite", limite).Parameter("offset", offset);
        var optTotal = new QueryOptions();
        if (estado is not null) { opt.Parameter("estado", estado); optTotal.Parameter("estado", estado); }
        var items = await Filas($"SELECT RAW ENCODE_JSON(c) FROM {cb.Ruta("cuarentena")} c {filtro} ORDER BY c.id_interaccion LIMIT $limite OFFSET $offset", opt);
        return (items, await Contar("cuarentena", filtro, optTotal));
    }

    public async Task<JsonElement?> ObtenerCuarentenaAsync(string id, CancellationToken ct)
    {
        if (!NoSqlSanitizer.ClaveValida(id)) return null;                               // clave validada antes de usarla
        var r = await Filas($"SELECT RAW ENCODE_JSON(c) FROM {cb.Ruta("cuarentena")} c USE KEYS [$id]", new QueryOptions().Parameter("id", id));
        return r.Count == 0 ? null : r[0];
    }

    public async Task<(IReadOnlyList<JsonElement> Items, long Total)> ListarDiarioAsync(int limite, int offset, CancellationToken ct)
    {
        var opt = new QueryOptions().Parameter("limite", limite).Parameter("offset", offset);
        var items = await Filas($"SELECT RAW ENCODE_JSON(c) FROM {cb.Ruta("diario_cambios")} c ORDER BY c.fecha DESC, META(c).id LIMIT $limite OFFSET $offset", opt);
        return (items, await Contar("diario_cambios", "", new QueryOptions()));
    }

    public Task<IReadOnlyList<JsonElement>> ListarBitacoraCargaAsync(int limite, CancellationToken ct)
        => Filas($"SELECT RAW ENCODE_JSON(c) FROM {cb.Ruta("bitacora_carga")} c ORDER BY c.inicio DESC LIMIT $limite", new QueryOptions().Parameter("limite", limite));

    public async Task<(IReadOnlyList<JsonElement> Items, long Total)> ListarBitacoraApiAsync(string? operacion, int limite, int offset, CancellationToken ct)
    {
        try
        {
            var filtro = operacion is null ? "" : "WHERE c.operacion = $operacion";
            var opt = new QueryOptions().Parameter("limite", limite).Parameter("offset", offset);
            var optTotal = new QueryOptions();
            if (operacion is not null) { opt.Parameter("operacion", operacion); optTotal.Parameter("operacion", operacion); }
            var items = await Filas($"SELECT RAW ENCODE_JSON(c) FROM {cb.Ruta(BitacoraApiRepository.Coleccion)} c {filtro} ORDER BY c.fecha_utc DESC, META(c).id LIMIT $limite OFFSET $offset", opt);
            return (items, await Contar(BitacoraApiRepository.Coleccion, filtro, optTotal));
        }
        catch (CouchbaseException ex) when (NoExiste(ex)) { return ([], 0); }
    }

    public async Task<IReadOnlyDictionary<string, long>> ConteosAsync(CancellationToken ct)
    {
        long ApiTotal;
        try { ApiTotal = await Contar(BitacoraApiRepository.Coleccion, "", new QueryOptions()); }
        catch (CouchbaseException ex) when (NoExiste(ex)) { ApiTotal = 0; }
        return new Dictionary<string, long>
        {
            ["cuarentena"] = await Contar("cuarentena", "", new QueryOptions()),
            ["diario_cambios"] = await Contar("diario_cambios", "", new QueryOptions()),
            ["bitacora_carga"] = await Contar("bitacora_carga", "", new QueryOptions()),
            ["bitacora_api"] = ApiTotal
        };
    }

    // Colección aún no creada en Capella (error 12003 "Keyspace not found" o colección desconocida).
    private static bool NoExiste(Exception ex) => ex.ToString() is var t && (t.Contains("12003") || t.Contains("Keyspace not found") || t.Contains("CollectionNotFound"));
}
