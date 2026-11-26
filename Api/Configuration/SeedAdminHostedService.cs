using Application.Common.Seguridad;
using Application.Interfaces;
using Application.Services;

namespace Api.Configuration;

/// <summary>
/// Crea las cuentas iniciales del API al arrancar, tomando usuario y contraseña de backend/.env
/// (SEED_ADMIN_* y, opcional, SEED_ANALISTA_* y SEED_PROFESOR_*). Solo crea la cuenta si ese nombre no existe; nunca la pisa.
/// Corre en segundo plano: si la base no responde, el API arranca igual y lo deja en el log (sin credenciales).
/// </summary>
public class SeedAdminHostedService(IServiceProvider sp, IConfiguration cfg, ILogger<SeedAdminHostedService> log) : BackgroundService
{
    public const int LargoMinimoContrasena = 8;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Sembrar("Seed:AdminUsuario", "Seed:AdminPassword", RolesSistema.Administrador, ct);
            await Sembrar("Seed:AnalistaUsuario", "Seed:AnalistaPassword", RolesSistema.Analista, ct);
            await Sembrar("Seed:ProfesorUsuario", "Seed:ProfesorPassword", RolesSistema.Administrador, ct);   // cuenta del revisor del proyecto
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            log.LogWarning("No se pudieron crear las cuentas iniciales ({Tipo}). Revisa la conexión a TiDB.", ex.GetType().Name);
        }
    }

    private async Task Sembrar(string claveUsuario, string clavePassword, string rol, CancellationToken ct)
    {
        var usuario = cfg[claveUsuario]?.Trim();
        var password = cfg[clavePassword];
        if (string.IsNullOrWhiteSpace(usuario) && string.IsNullOrEmpty(password)) return;   // no configurada

        if (string.IsNullOrWhiteSpace(usuario) || usuario.Length is < 3 or > 50 || string.IsNullOrEmpty(password) || password.Length < LargoMinimoContrasena)
        {
            log.LogWarning("Cuenta inicial de rol {Rol} omitida: usuario de 3 a 50 caracteres y contraseña de al menos {Min}.", rol, LargoMinimoContrasena);
            return;
        }

        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUsuarioSistemaRepository>();
        if (await repo.BuscarPorNombreAsync(usuario, ct) is not null) return;

        var creada = await repo.CrearAsync(usuario, AuthService.HashContrasena(password), rol, ct);
        if (creada) log.LogInformation("Cuenta inicial creada: {Usuario} ({Rol}).", usuario, rol);
        else log.LogWarning("No se creó la cuenta {Usuario}: el rol {Rol} no existe en la tabla rol.", usuario, rol);
    }
}
