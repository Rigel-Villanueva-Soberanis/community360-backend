using Domain;

namespace Application.Interfaces;

public interface IUsuarioSistemaRepository
{
    Task<UsuarioSistema?> BuscarPorNombreAsync(string nombre, CancellationToken ct);
    Task<int> ContarAsync(CancellationToken ct);
    /// <summary>Crea la cuenta; devuelve false si el rol no existe.</summary>
    Task<bool> CrearAsync(string nombre, string hash, string rol, CancellationToken ct);
}
