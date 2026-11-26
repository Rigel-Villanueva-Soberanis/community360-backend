using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsUsuarios;

public static class UsuarioErrors
{
    /// <summary>404. No repite el id que mandó el cliente.</summary>
    public static DomainException NoExiste() => ApiErrors.NotFound("El usuario");
}
