using System.Text.Json;

namespace Application.Interfaces;

/// <summary>Lectura de las colecciones de auditoría de Couchbase. Devuelve los documentos tal como están guardados.</summary>
public interface IAuditoriaRepository
{
    Task<(IReadOnlyList<JsonElement> Items, long Total)> ListarCuarentenaAsync(string? estadoRevision, int limite, int offset, CancellationToken ct);
    Task<JsonElement?> ObtenerCuarentenaAsync(string idInteraccion, CancellationToken ct);
    Task<(IReadOnlyList<JsonElement> Items, long Total)> ListarDiarioAsync(int limite, int offset, CancellationToken ct);
    Task<IReadOnlyList<JsonElement>> ListarBitacoraCargaAsync(int limite, CancellationToken ct);
    /// <summary>Si la colección todavía no existe en Capella devuelve vacío en lugar de fallar.</summary>
    Task<(IReadOnlyList<JsonElement> Items, long Total)> ListarBitacoraApiAsync(string? operacion, int limite, int offset, CancellationToken ct);
    Task<IReadOnlyDictionary<string, long>> ConteosAsync(CancellationToken ct);
}
