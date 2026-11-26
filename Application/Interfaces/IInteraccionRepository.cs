using Application.Common.Utilidades;
using Domain;

namespace Application.Interfaces;

/// <param name="Orden">Clave ya validada: fecha, sentimiento o reacciones.</param>
public record FiltroInteracciones(string? IdUsuario, int? IdPlataforma, int? IdTipo, int? IdTema, int? IdEtiqueta,
    DateTime? Desde, DateTime? Hasta, string Orden, bool Descendente);

public interface IInteraccionRepository
{
    Task<(IReadOnlyList<InteraccionLectura> Items, int Total)> ListarAsync(FiltroInteracciones filtro, Paginacion pag, CancellationToken ct);
    Task<InteraccionLectura?> ObtenerAsync(string idInteraccion, CancellationToken ct);

    /// <summary>Inserta interacción y etiquetas en una transacción. Devuelve el id (el solicitado o uno generado). Lanza 409 si ya existe.</summary>
    Task<string> InsertarAsync(Interaccion interaccion, string? idSolicitado, IReadOnlyCollection<int> idsEtiquetas, CancellationToken ct);

    /// <summary>Actualiza campos y reemplaza etiquetas en una transacción. false si no existe.</summary>
    Task<bool> ActualizarAsync(string idInteraccion, InteraccionCambios cambios, IReadOnlyCollection<int> idsEtiquetas, CancellationToken ct);

    /// <summary>Borra etiquetas e interacción en una transacción. false si no existe.</summary>
    Task<bool> EliminarAsync(string idInteraccion, CancellationToken ct);
}
