using Domain;

namespace Application.Interfaces;

public interface ICatalogoRepository
{
    /// <summary>Nombres de catálogo permitidos (lista blanca).</summary>
    IReadOnlyList<string> Claves { get; }

    /// <summary>Valores del catálogo, o null si la clave no está en la lista blanca.</summary>
    Task<IReadOnlyList<ItemCatalogo>?> ListarAsync(string clave, CancellationToken ct);
}
