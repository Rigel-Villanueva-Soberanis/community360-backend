using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Common.ErrorsAuditoria;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;

namespace Application.Services;

public class AuditoriaService(IAuditoriaRepository repo)
{
    public static readonly IReadOnlyDictionary<string, string> Estados = new Dictionary<string, string>
    { ["pendiente"] = "pendiente", ["descartada"] = "descartada", ["recuperada"] = "recuperada" };

    public static readonly IReadOnlyDictionary<string, string> Operaciones = new Dictionary<string, string>
    { ["alta"] = "alta", ["modificacion"] = "modificacion", ["baja"] = "baja", ["rechazo"] = "rechazo" };

    /// <summary>Datos de ubicación: nunca se muestran, aunque algún día aparezcan en un documento.</summary>
    private static readonly string[] CamposUbicacion = ["pais", "estado", "ciudad", "latitud", "longitud", "id_ciudad"];

    public async Task<PaginaResponse<JsonElement>> CuarentenaAsync(FiltroAuditoriaRequest f, CancellationToken ct)
    {
        var pag = Paginacion.Crear(f.Pagina, f.Tamano);
        string? estado = string.IsNullOrWhiteSpace(f.EstadoRevision) ? null : ListaBlanca.Resolver(Estados, f.EstadoRevision, "pendiente", "estadoRevision");
        var (items, total) = await repo.ListarCuarentenaAsync(estado, pag.Tamano, pag.Offset, ct);
        return new PaginaResponse<JsonElement>(items.Select(SinUbicacion).ToList(), pag.Pagina, pag.Tamano, (int)Math.Min(total, int.MaxValue));
    }

    public async Task<JsonElement> CuarentenaPorIdAsync(string? id, CancellationToken ct)
    {
        var idValido = Identificadores.Interaccion(id);
        var doc = await repo.ObtenerCuarentenaAsync(idValido, ct) ?? throw AuditoriaErrors.NoExiste();
        return SinUbicacion(doc);
    }

    public async Task<PaginaResponse<JsonElement>> DiarioAsync(FiltroAuditoriaRequest f, CancellationToken ct)
    {
        var pag = Paginacion.Crear(f.Pagina, f.Tamano);
        var (items, total) = await repo.ListarDiarioAsync(pag.Tamano, pag.Offset, ct);
        return new PaginaResponse<JsonElement>(items, pag.Pagina, pag.Tamano, (int)Math.Min(total, int.MaxValue));
    }

    public Task<IReadOnlyList<JsonElement>> BitacoraCargaAsync(CancellationToken ct) => repo.ListarBitacoraCargaAsync(100, ct);

    public async Task<PaginaResponse<JsonElement>> BitacoraApiAsync(FiltroAuditoriaRequest f, CancellationToken ct)
    {
        var pag = Paginacion.Crear(f.Pagina, f.Tamano);
        string? op = string.IsNullOrWhiteSpace(f.Operacion) ? null : ListaBlanca.Resolver(Operaciones, f.Operacion, "alta", "operacion");
        var (items, total) = await repo.ListarBitacoraApiAsync(op, pag.Tamano, pag.Offset, ct);
        return new PaginaResponse<JsonElement>(items, pag.Pagina, pag.Tamano, (int)Math.Min(total, int.MaxValue));
    }

    public async Task<IReadOnlyDictionary<string, long>> ResumenAsync(CancellationToken ct) => await repo.ConteosAsync(ct);

    private static JsonElement SinUbicacion(JsonElement doc)
    {
        if (doc.ValueKind != JsonValueKind.Object) return doc;
        var obj = JsonNode.Parse(doc.GetRawText())!.AsObject();
        foreach (var campo in CamposUbicacion) obj.Remove(campo);
        return JsonSerializer.SerializeToElement(obj);
    }
}
