namespace Application.Interfaces;

/// <summary>Bitácora de operaciones del API (colección bitacora_api de Couchbase).</summary>
public interface IBitacoraApiRepository
{
    /// <param name="operacion">alta, modificacion, baja o rechazo.</param>
    Task RegistrarAsync(string operacion, string? idInteraccion, string realizadoPor,
        IReadOnlyDictionary<string, object?>? antes, IReadOnlyDictionary<string, object?>? despues,
        IReadOnlyDictionary<string, object?>? detalle, CancellationToken ct);
}
