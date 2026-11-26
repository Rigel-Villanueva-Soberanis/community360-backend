using Application.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>Escribe en la colección bitacora_api de Couchbase (hay que crearla en Capella: bucket community360, scope _default).</summary>
public class BitacoraApiRepository(CouchbaseContext cb) : IBitacoraApiRepository
{
    public const string Coleccion = "bitacora_api";

    public async Task RegistrarAsync(string operacion, string? idInteraccion, string realizadoPor,
        IReadOnlyDictionary<string, object?>? antes, IReadOnlyDictionary<string, object?>? despues,
        IReadOnlyDictionary<string, object?>? detalle, CancellationToken ct)
    {
        var col = await cb.ColeccionAsync(Coleccion);
        var ahora = DateTime.UtcNow;
        // Clave con formato fijo: la fecha, la operación (constante del código), un id ya validado y un sufijo aleatorio.
        var clave = $"{ahora:yyyyMMddHHmmssfff}::{operacion}::{idInteraccion ?? "sin-id"}::{Guid.NewGuid().ToString("N")[..6]}";
        await col.UpsertAsync(clave, new Dictionary<string, object?>
        {
            ["operacion"] = operacion,
            ["id_interaccion"] = idInteraccion,
            ["realizado_por"] = realizadoPor,
            ["fecha_utc"] = ahora.ToString("O"),
            ["origen"] = "api",
            ["antes"] = antes,
            ["despues"] = despues,
            ["detalle"] = detalle
        });
    }
}
