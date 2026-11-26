using Application.Common.ErrorsAuth;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;

namespace Application.Services;

public class AuthService(IUsuarioSistemaRepository repo, IJwtGenerator jwt, ITokenRevocadoStore revocados)
{
    /// <summary>Factor de trabajo de BCrypt para contraseñas nuevas.</summary>
    public const int CostoBCrypt = 12;

    // Hash de relleno: la respuesta tarda lo mismo exista o no el usuario (evita enumerar cuentas por tiempo).
    private static readonly string HashFalso = BCrypt.Net.BCrypt.HashPassword("relleno-no-valido", CostoBCrypt);

    public static string HashContrasena(string contrasena) => BCrypt.Net.BCrypt.HashPassword(contrasena, CostoBCrypt);

    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var nombre = NoSqlSanitizer.Limpiar(req.NombreUsuario, "nombreUsuario", 50);
        var u = nombre.Length == 0 ? null : await repo.BuscarPorNombreAsync(nombre, ct);

        var ok = Verificar(req.Contrasena, u?.ContrasenaHash ?? HashFalso);
        if (u is null || !ok) throw AuthErrors.CredencialesInvalidas();
        if (!u.Activo) throw AuthErrors.CuentaInactiva();

        var (token, expira) = jwt.Crear(u);
        return new LoginResponse(token, expira, u.NombreUsuario, u.Rol);
    }

    public Task CerrarSesionAsync(string jti, DateTime expiraUtc)
    {
        revocados.Revocar(jti, expiraUtc);
        return Task.CompletedTask;
    }

    // Una contraseña con caracteres raros nunca debe provocar un 500: simplemente no coincide.
    private static bool Verificar(string contrasena, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(contrasena, hash); }
        catch (Exception) { return false; }
    }
}
