using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsAuth;

public static class AuthErrors
{
    public const string MensajeCredenciales = "Usuario o contraseña incorrectos.";
    public const string MensajeSesionCerrada = "La sesión ya fue cerrada.";
    public const string MensajeSesionInvalida = "Sin sesión válida: inicia sesión de nuevo.";
    public const string MensajeSinPermiso = "Tu rol no tiene permiso para esta operación.";

    // Mismo mensaje si el usuario no existe, la contraseña es mala o la cuenta está inactiva: no se revela cuál falló.
    public static DomainException CredencialesInvalidas() => new(401, "AUTH_INVALID", MensajeCredenciales);
    public static DomainException CuentaInactiva() => new(401, "AUTH_INACTIVE", MensajeCredenciales);
    public static DomainException TokenRevocado() => new(401, "AUTH_REVOKED", MensajeSesionCerrada);
}
