using Application.Interfaces;
using Application.Services;
using Infrastructure.Data;
using Infrastructure.Realtime;
using Infrastructure.Repositories;

namespace Api.Configuration;

public static class DependencyInjection
{
    /// <summary>Registra los servicios de la aplicación. Cada módulo añade aquí sus Service y Repository.</summary>
    public static IServiceCollection AddCommunity360(this IServiceCollection s, IConfiguration cfg)
    {
        s.AddSingleton<DbConnectionFactory>();
        s.AddSingleton<CouchbaseContext>();

        // Profiles de AutoMapper: busca en todo el ensamblado de Application (aún sin perfiles; se añaden por módulo)
        s.AddAutoMapper(c => c.AddMaps(typeof(Application.Common.ErrorCatalog.ApiErrors).Assembly));

        // Módulo Auth (paso 4). El token, su revocación y el JWT se registran en AddAutenticacion.
        s.AddScoped<IUsuarioSistemaRepository, UsuarioSistemaRepository>();
        s.AddScoped<AuthService>();
        s.AddHostedService<SeedAdminHostedService>();

        // Módulo Catálogos (paso 5). El repositorio es singleton porque guarda los catálogos en memoria.
        s.AddSingleton<ICatalogoRepository, CatalogoRepository>();
        s.AddScoped<CatalogoService>();

        // Módulo Usuarios (paso 6)
        s.AddScoped<IUsuarioRepository, UsuarioRepository>();
        s.AddScoped<UsuarioService>();

        // Módulo Interacciones (paso 7). El notificador en vivo se reemplaza en el paso 10.
        s.AddScoped<IInteraccionRepository, InteraccionRepository>();
        s.AddSingleton<IBitacoraApiRepository, BitacoraApiRepository>();
        // Tiempo real (paso 10): un solo hub hace de notificador y de contador de conexiones.
        s.AddSingleton<WebSocketHub>();
        s.AddSingleton<IRealtimeNotifier>(sp => sp.GetRequiredService<WebSocketHub>());
        s.AddSingleton<IConexionesTiempoReal>(sp => sp.GetRequiredService<WebSocketHub>());
        s.AddScoped<InteraccionService>();

        // Módulo Analítica (paso 8)
        s.AddScoped<IAnaliticaRepository, AnaliticaRepository>();
        s.AddScoped<MetricasService>();

        // Módulo Auditoría (paso 9)
        s.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        s.AddScoped<AuditoriaService>();

        // Siguiente módulo (paso 10): s.AddScoped<...>() de cada Service y Repository.
        return s;
    }
}
