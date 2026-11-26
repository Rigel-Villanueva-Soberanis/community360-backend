using Application.Common.Utilidades;
using Domain;

namespace Application.Interfaces;

/// <param name="PatronBuscar">Patrón LIKE ya normalizado y escapado (SqlLikeHelper.Contiene), o null.</param>
/// <param name="Orden">Clave de orden ya validada contra la lista blanca (alias, ingreso, actividad, sentimiento).</param>
public record FiltroUsuarios(int? IdPlataforma, int? IdEspecie, string? Estatus, string? RangoEdad, bool? EsArtista,
    string? PatronBuscar, string Orden, bool Descendente);

public interface IUsuarioRepository
{
    Task<(IReadOnlyList<Usuario> Items, int Total)> ListarAsync(FiltroUsuarios filtro, Paginacion pag, CancellationToken ct);
    Task<Usuario?> ObtenerAsync(string idUsuario, CancellationToken ct);

    /// <summary>Consentimiento y ciudad del usuario (lo que necesita el alta de una interacción).</summary>
    Task<UsuarioBasico?> ObtenerBasicoAsync(string idUsuario, CancellationToken ct);
    Task<IReadOnlyList<InteresUsuario>> InteresesAsync(string idUsuario, CancellationToken ct);
    Task<(IReadOnlyList<InteraccionLectura> Items, int Total)> ListarInteraccionesAsync(string idUsuario, Paginacion pag, CancellationToken ct);
}
