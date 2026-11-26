using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Api.Configuration;
using Api.Controllers;
using Api.Middlewares;
using Application.Common.Seguridad;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Community360.Tests;

/// <summary>
/// Servidor de pruebas con las mismas protecciones, autenticación y controllers reales del API,
/// pero con repositorios falsos en memoria (sin bases de datos). Cada módulo registra los suyos en <see cref="Servicios"/>.
/// </summary>
public abstract class ApiTestHost : IAsyncLifetime
{
    protected WebApplication App = null!;
    protected HttpClient Http = null!;
    protected RepoFalso Cuentas = new();
    protected UsuarioRepoFalso UsuariosFalsos = new();
    protected InteraccionRepoFalso InteraccionesFalsas = new();
    protected BitacoraFalsa BitacoraFalsa = new();
    protected AnaliticaRepoFalso AnaliticaFalsa = new();
    protected AuditoriaRepoFalso AuditoriaFalsa = new();

    protected virtual void Servicios(IServiceCollection s) { }

    public async Task InitializeAsync()
    {
        var b = WebApplication.CreateBuilder();
        b.WebHost.UseTestServer();
        b.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Jwt:Key"] = Util.Clave, ["Jwt:Issuer"] = "community360", ["Jwt:Audience"] = "community360-clientes", ["Jwt:MinutosVida"] = "60" });
        b.AddSeguridadBasica();
        b.Services.AddCorsFrontend(b.Configuration);
        b.Services.AddSingleton<IUsuarioSistemaRepository>(Cuentas);
        b.Services.AddScoped<AuthService>();
        b.Services.AddSingleton<ICatalogoRepository>(new CatalogoFalso());
        b.Services.AddScoped<CatalogoService>();
        b.Services.AddAutoMapper(c => c.AddMaps(typeof(UsuarioService).Assembly));
        b.Services.AddSingleton<IUsuarioRepository>(UsuariosFalsos);
        b.Services.AddScoped<UsuarioService>();
        b.Services.AddSingleton<IInteraccionRepository>(InteraccionesFalsas);
        b.Services.AddSingleton<IBitacoraApiRepository>(BitacoraFalsa);
        b.Services.AddSingleton<Infrastructure.Realtime.WebSocketHub>();
        b.Services.AddSingleton<IRealtimeNotifier>(sp => sp.GetRequiredService<Infrastructure.Realtime.WebSocketHub>());
        b.Services.AddSingleton<IConexionesTiempoReal>(sp => sp.GetRequiredService<Infrastructure.Realtime.WebSocketHub>());
        b.Services.AddScoped<InteraccionService>();
        b.Services.AddSingleton<IAnaliticaRepository>(AnaliticaFalsa);
        b.Services.AddScoped<MetricasService>();
        b.Services.AddSingleton<IAuditoriaRepository>(AuditoriaFalsa);
        b.Services.AddScoped<AuditoriaService>();
        Servicios(b.Services);
        b.Services.AddAutenticacion(b.Configuration);
        b.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        App = b.Build();
        App.UseSeguridadBasica();
        App.UseMiddleware<ExceptionMiddleware>();
        App.UseCors(CorsConfig.Politica);
        App.UseRateLimiter();
        App.UseAuthentication();
        App.UseAuthorization();
        App.UseWebSockets();
        App.UseMiddleware<Api.Middlewares.WebSocketMiddleware>();
        App.MapControllers();
        await ConfigurarApp(App);
        await App.StartAsync();
        Http = App.GetTestClient();
    }

    protected virtual Task ConfigurarApp(WebApplication app) => Task.CompletedTask;

    public async Task DisposeAsync() { Http.Dispose(); await App.DisposeAsync(); }

    private static readonly JwtGenerator Generador = new(Util.Settings());

    protected string TokenAdmin() => Generador.Crear(Cuentas.Cuentas.Single(c => c.NombreUsuario == "admin")).Token;
    protected string TokenAnalista() => Generador.Crear(Cuentas.Cuentas.Single(c => c.NombreUsuario == "ana")).Token;

    protected Task<HttpResponseMessage> Enviar(HttpMethod metodo, string url, string? token = null, string? json = null)
    {
        var req = new HttpRequestMessage(metodo, url);
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (json is not null) req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return Http.SendAsync(req);
    }

    protected Task<HttpResponseMessage> Get(string url, string? token = null) => Enviar(HttpMethod.Get, url, token);
    protected Task<HttpResponseMessage> Post(string url, string json, string? token = null) => Enviar(HttpMethod.Post, url, token, json);
    protected Task<HttpResponseMessage> Put(string url, string json, string? token = null) => Enviar(HttpMethod.Put, url, token, json);
    protected Task<HttpResponseMessage> Delete(string url, string? token = null) => Enviar(HttpMethod.Delete, url, token);

    protected static async Task<JsonElement> Json(HttpResponseMessage r)
        => JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
}
