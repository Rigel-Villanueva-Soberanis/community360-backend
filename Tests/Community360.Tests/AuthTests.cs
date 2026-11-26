using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Api.Configuration;
using Api.Controllers;
using Api.Middlewares;
using Application.Common.ErrorCatalog;
using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.Interfaces;
using Application.Services;
using Domain;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Community360.Tests;

/// <summary>Repositorio en memoria (hash con costo 4 para que las pruebas sean rápidas).</summary>
public class RepoFalso : IUsuarioSistemaRepository
{
    public readonly List<UsuarioSistema> Cuentas = [];
    public List<string> Consultas { get; } = [];

    public RepoFalso()
    {
        Cuentas.Add(Cuenta(1, "admin", "ClaveAdmin123", RolesSistema.Administrador));
        Cuentas.Add(Cuenta(2, "ana", "ClaveAnalista1", RolesSistema.Analista));
        Cuentas.Add(Cuenta(3, "baja", "ClaveBaja1234", RolesSistema.Analista, activo: false));
    }

    public static UsuarioSistema Cuenta(int id, string nombre, string pass, string rol, bool activo = true) => new()
    { Id = id, NombreUsuario = nombre, ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(pass, 4), Rol = rol, Activo = activo };

    public Task<UsuarioSistema?> BuscarPorNombreAsync(string nombre, CancellationToken ct)
    {
        Consultas.Add(nombre);
        return Task.FromResult(Cuentas.FirstOrDefault(c => c.NombreUsuario == nombre));
    }
    public Task<int> ContarAsync(CancellationToken ct) => Task.FromResult(Cuentas.Count);
    public Task<bool> CrearAsync(string nombre, string hash, string rol, CancellationToken ct)
    {
        Cuentas.Add(new UsuarioSistema { Id = Cuentas.Count + 1, NombreUsuario = nombre, ContrasenaHash = hash, Rol = rol, Activo = true });
        return Task.FromResult(true);
    }
}

public static class Util
{
    public const string Clave = "una-clave-de-prueba-de-mas-de-32-caracteres-123";
    public static JwtSettings Settings(int minutos = 60) => new("community360", "community360-clientes", Clave, minutos);
}

public class AuthServiceTests
{
    private readonly RepoFalso _repo = new();
    private readonly TokenRevocadoStore _store = new();
    private AuthService Servicio() => new(_repo, new JwtGenerator(Util.Settings()), _store);

    private static LoginRequest Req(string u, string p) => new() { NombreUsuario = u, Contrasena = p };

    [Fact]
    public async Task LoginCorrectoDevuelveTokenYRol()
    {
        var r = await Servicio().LoginAsync(Req("admin", "ClaveAdmin123"), default);
        Assert.Equal("admin", r.NombreUsuario);
        Assert.Equal("administrador", r.Rol);
        Assert.True(r.ExpiraUtc > DateTime.UtcNow);
        Assert.Equal(3, r.Token.Split('.').Length);
    }

    [Fact]
    public async Task ContrasenaMalaYUsuarioInexistenteYCuentaInactivaDanElMismoMensaje()
    {
        var a = await Assert.ThrowsAsync<DomainException>(() => Servicio().LoginAsync(Req("admin", "mala-mala-mala"), default));
        var b = await Assert.ThrowsAsync<DomainException>(() => Servicio().LoginAsync(Req("noexiste", "ClaveAdmin123"), default));
        var c = await Assert.ThrowsAsync<DomainException>(() => Servicio().LoginAsync(Req("baja", "ClaveBaja1234"), default));
        Assert.All(new[] { a, b, c }, e => Assert.Equal(401, e.StatusCode));
        Assert.Equal(a.Message, b.Message);
        Assert.Equal(a.Message, c.Message);
    }

    [Theory]
    [InlineData("admin' OR '1'='1")]
    [InlineData("{\"$ne\":null}")]
    [InlineData("'; DROP TABLE usuario_sistema;--")]
    public async Task UsuarioConInyeccionEsSoloUnUsuarioQueNoExiste(string usuario)
    {
        var e = await Assert.ThrowsAsync<DomainException>(() => Servicio().LoginAsync(Req(usuario, "ClaveAdmin123"), default));
        Assert.Equal(401, e.StatusCode);
    }

    [Fact]
    public async Task CaracteresDeControlEnElUsuarioSeQuitanPeroSinLaContrasenaNoEntra()
    {
        var e = await Assert.ThrowsAsync<DomainException>(() => Servicio().LoginAsync(Req("admin\0", "mala-mala-mala"), default));
        Assert.Equal(401, e.StatusCode);
        Assert.Equal("admin", Assert.Single(_repo.Consultas));   // se buscó ya normalizado
    }

    [Theory]
    [InlineData("pass\0word")]
    [InlineData("' OR '1'='1")]
    [InlineData("\uD800rara")]
    public async Task ContrasenaRaraNuncaDa500(string pass)
    {
        var e = await Assert.ThrowsAsync<DomainException>(() => Servicio().LoginAsync(Req("admin", pass), default));
        Assert.Equal(401, e.StatusCode);
    }

    [Fact]
    public async Task ElNombreDeUsuarioViajaComoParametroSinAlterarse()
    {
        await Assert.ThrowsAsync<DomainException>(() => Servicio().LoginAsync(Req("x'; DROP TABLE t;--", "ClaveAdmin123"), default));
        Assert.Equal("x'; DROP TABLE t;--", Assert.Single(_repo.Consultas));
    }

    [Fact]
    public void ElHashEsBCrypt()
    {
        var h = AuthService.HashContrasena("UnaContrasenaFuerte1");
        Assert.StartsWith("$2", h);
        Assert.DoesNotContain("UnaContrasenaFuerte1", h);
        Assert.True(BCrypt.Net.BCrypt.Verify("UnaContrasenaFuerte1", h));
    }
}

public class JwtGeneratorTests
{
    private static readonly UsuarioSistema Usuario = RepoFalso.Cuenta(7, "ana", "x", RolesSistema.Analista);

    private static ClaimsPrincipal Validar(string token, string clave = Util.Clave)
    {
        var p = new TokenValidationParameters
        {
            ValidIssuer = "community360", ValidAudience = "community360-clientes",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
            ClockSkew = TimeSpan.Zero
        };
        return new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(token, p, out _);
    }

    [Fact]
    public void ElTokenTraeSubNombreRolYJtiUnico()
    {
        var g = new JwtGenerator(Util.Settings());
        var (t1, _) = g.Crear(Usuario);
        var (t2, _) = g.Crear(Usuario);
        var c = Validar(t1);
        Assert.Equal("7", c.FindFirstValue("sub"));
        Assert.Equal("ana", c.FindFirstValue("name"));
        Assert.Equal("analista", c.FindFirstValue("role"));
        Assert.NotEqual(c.FindFirstValue("jti"), Validar(t2).FindFirstValue("jti"));
    }

    [Fact]
    public void NoLlevaLaContrasenaNiElHash()
    {
        var (t, _) = new JwtGenerator(Util.Settings()).Crear(Usuario);
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(t.Split('.')[1].PadRight((t.Split('.')[1].Length + 3) / 4 * 4, '=').Replace('-', '+').Replace('_', '/')));
        Assert.DoesNotContain("$2", payload);
        Assert.DoesNotContain("ContrasenaHash", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConOtraClaveNoValida()
    {
        var (t, _) = new JwtGenerator(Util.Settings()).Crear(Usuario);
        Assert.ThrowsAny<SecurityTokenException>(() => Validar(t, "otra-clave-distinta-de-mas-de-32-caracteres-999"));
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("corta")]
    public void SinClaveSuficienteElArranqueFalla(string? clave)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSettings.Desde(k => k == "Jwt:Key" ? clave : null));
        Assert.Contains("JWT_KEY", ex.Message);
    }

    [Fact]
    public void MinutosInvalidosUsanElValorPorDefecto()
        => Assert.Equal(60, JwtSettings.Desde(k => k == "Jwt:Key" ? Util.Clave : k == "Jwt:MinutosVida" ? "-5" : null).MinutosVida);
}

[ApiController, Route("api/prueba")]
public class ProtegidoPruebaController : ControllerBase
{
    [HttpGet("lectura"), Authorize(Roles = RolesSistema.Ambos)] public IActionResult Lectura() => Ok();
    [HttpPost("escritura"), Authorize(Roles = RolesSistema.Administrador)] public IActionResult Escritura() => Ok();
}

/// <summary>Servidor mínimo con la autenticación real del API y un repositorio en memoria.</summary>
public class AuthHttpTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _http = null!;
    private RepoFalso _repo = null!;

    public async Task InitializeAsync()
    {
        var b = WebApplication.CreateBuilder();
        b.WebHost.UseTestServer();
        b.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Jwt:Key"] = Util.Clave, ["Jwt:Issuer"] = "community360", ["Jwt:Audience"] = "community360-clientes", ["Jwt:MinutosVida"] = "60" });
        b.AddSeguridadBasica();
        _repo = new RepoFalso();
        b.Services.AddSingleton<IUsuarioSistemaRepository>(_repo);
        b.Services.AddScoped<AuthService>();
        b.Services.AddAutenticacion(b.Configuration);
        b.Services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly)
            .AddApplicationPart(typeof(ProtegidoPruebaController).Assembly);
        _app = b.Build();
        _app.UseSeguridadBasica();
        _app.UseMiddleware<ExceptionMiddleware>();
        _app.UseRateLimiter();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();
        _http = _app.GetTestClient();
    }

    public async Task DisposeAsync() { _http.Dispose(); await _app.DisposeAsync(); }

    private Task<HttpResponseMessage> Login(string json)
        => _http.PostAsync("/api/auth/login", new StringContent(json, Encoding.UTF8, "application/json"));

    private static string Cred(string u, string p) => JsonSerializer.Serialize(new { nombreUsuario = u, contrasena = p });

    private async Task<string> Token(string u, string p)
    {
        var r = await Login(Cred(u, p));
        r.EnsureSuccessStatusCode();
        return Cuerpo(await r.Content.ReadAsStringAsync()).GetProperty("datos").GetProperty("token").GetString()!;
    }

    private static JsonElement Cuerpo(string json) => JsonDocument.Parse(json).RootElement;

    private async Task<HttpResponseMessage> Enviar(HttpMethod m, string url, string? token = null)
    {
        var req = new HttpRequestMessage(m, url);
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(req);
    }

    [Fact]
    public async Task LoginCorrectoDa200TokenYCookieHttpOnly()
    {
        var r = await Login(Cred("admin", "ClaveAdmin123"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cookie = r.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("c360_token="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.False(Cuerpo(await r.Content.ReadAsStringAsync()).GetProperty("datos").TryGetProperty("contrasenaHash", out _));
    }

    [Fact]
    public async Task CredencialesMalasDan401ConElMismoMensaje()
    {
        var a = await Login(Cred("admin", "incorrecta1"));
        var b = await Login(Cred("fantasma", "incorrecta1"));
        Assert.Equal(HttpStatusCode.Unauthorized, a.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, b.StatusCode);
        Assert.Equal(await a.Content.ReadAsStringAsync(), await b.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ObjetoNoSqlEnElLoginDa401NoErrorDeServidor()
    {
        var r = await Login("""{"nombreUsuario":{"$ne":null},"contrasena":{"$ne":null}}""");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task InyeccionSqlEnElLoginDa401()
        => Assert.Equal(HttpStatusCode.Unauthorized, (await Login(Cred("admin' OR '1'='1", "x' OR '1'='1"))).StatusCode);

    [Fact]
    public async Task FaltanCamposDa422()
        => Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Login("""{"nombreUsuario":"admin"}""")).StatusCode);

    [Fact]
    public async Task LoginConJsonRotoDa400()
        => Assert.Equal(HttpStatusCode.BadRequest, (await Login("""{"nombreUsuario":""")).StatusCode);

    [Fact]
    public async Task SextoIntentoEnUnMinutoDa429ConJson()
    {
        for (var i = 0; i < AuthConfig.IntentosLoginPorMinuto; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(Cred("admin", "incorrecta1"))).StatusCode);
        var r = await Login(Cred("admin", "ClaveAdmin123"));
        Assert.Equal(HttpStatusCode.TooManyRequests, r.StatusCode);
        Assert.False(Cuerpo(await r.Content.ReadAsStringAsync()).GetProperty("exito").GetBoolean());
    }

    [Fact]
    public async Task MeSinTokenDa401ConJson()
    {
        var r = await Enviar(HttpMethod.Get, "/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.False(Cuerpo(await r.Content.ReadAsStringAsync()).GetProperty("exito").GetBoolean());
    }

    [Fact]
    public async Task MeConTokenDevuelveUsuarioYRol()
    {
        var r = await Enviar(HttpMethod.Get, "/api/auth/me", await Token("ana", "ClaveAnalista1"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = Cuerpo(await r.Content.ReadAsStringAsync()).GetProperty("datos");
        Assert.Equal("ana", d.GetProperty("nombreUsuario").GetString());
        Assert.Equal("analista", d.GetProperty("rol").GetString());
        Assert.Equal(2, d.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task MeFuncionaConLaCookieDelNavegador()
    {
        var token = await Token("admin", "ClaveAdmin123");
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        req.Headers.Add("Cookie", $"c360_token={token}");
        Assert.Equal(HttpStatusCode.OK, (await _http.SendAsync(req)).StatusCode);
    }

    [Fact]
    public async Task CuandoVienenCabeceraYCookieMandalaCabecera()
    {
        var admin = await Token("admin", "ClaveAdmin123");
        var ana = await Token("ana", "ClaveAnalista1");
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ana);
        req.Headers.Add("Cookie", $"c360_token={admin}");
        var d = System.Text.Json.JsonDocument.Parse(await (await _http.SendAsync(req)).Content.ReadAsStringAsync()).RootElement.GetProperty("datos");
        Assert.Equal("ana", d.GetProperty("nombreUsuario").GetString());     // la cookie del admin no se impone

        // Cookie vieja/revocada + cabecera válida: la cabecera sigue funcionando.
        await Enviar(HttpMethod.Post, "/api/auth/logout", admin);
        var req2 = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        req2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ana);
        req2.Headers.Add("Cookie", $"c360_token={admin}");
        Assert.Equal(HttpStatusCode.OK, (await _http.SendAsync(req2)).StatusCode);
    }

    [Fact]
    public async Task TokenFalsoOManipuladoDa401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/me", "esto.no.es-un-token")).StatusCode);

        var otra = new JwtGenerator(new JwtSettings("community360", "community360-clientes", "otra-clave-distinta-de-mas-de-32-caracteres-999", 60));
        var (falso, _) = otra.Crear(RepoFalso.Cuenta(1, "admin", "x", RolesSistema.Administrador));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/me", falso)).StatusCode);
    }

    private static string B64Url(string texto) => Convert.ToBase64String(Encoding.UTF8.GetBytes(texto)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string DeB64Url(string b64)
    {
        var t = b64.Replace('-', '+').Replace('_', '/');
        return Encoding.UTF8.GetString(Convert.FromBase64String(t.PadRight(t.Length + (4 - t.Length % 4) % 4, '=')));
    }

    [Fact]
    public async Task TokenSinFirmaAlgNoneDa401()
    {
        var payload = B64Url("{\"sub\":\"1\",\"name\":\"admin\",\"role\":\"administrador\",\"jti\":\"x\",\"iss\":\"community360\",\"aud\":\"community360-clientes\",\"exp\":" +
                              DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() + "}");
        foreach (var alg in new[] { "none", "None", "NONE" })
        {
            var sinFirma = $"{B64Url($"{{\"alg\":\"{alg}\",\"typ\":\"JWT\"}}")}.{payload}.";
            Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/me", sinFirma)).StatusCode);
        }
    }

    [Fact]
    public async Task PayloadModificadoConLaFirmaOriginalDa401()
    {
        var ana = await Token("ana", "ClaveAnalista1");
        var partes = ana.Split('.');
        var payload = DeB64Url(partes[1]);
        Assert.Contains("analista", payload);
        var ascendido = $"{partes[0]}.{B64Url(payload.Replace("analista", "administrador"))}.{partes[2]}";

        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/me", ascendido)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Post, "/api/prueba/escritura", ascendido)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar(HttpMethod.Post, "/api/prueba/escritura", ana)).StatusCode);   // el original sigue siendo solo analista
    }

    [Fact]
    public async Task TokenVencidoDa401()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Util.Clave));
        var vencido = new JwtSecurityToken("community360", "community360-clientes",
            [new Claim("sub", "1"), new Claim("name", "admin"), new Claim("role", "administrador"), new Claim("jti", "x")],
            notBefore: DateTime.UtcNow.AddHours(-2), expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Enviar(HttpMethod.Get, "/api/auth/me", new JwtSecurityTokenHandler().WriteToken(vencido))).StatusCode);
    }

    [Fact]
    public async Task TokenConOtraAudienciaDa401()
    {
        var otra = new JwtGenerator(new JwtSettings("community360", "otro-sistema", Util.Clave, 60));
        var (t, _) = otra.Crear(RepoFalso.Cuenta(1, "admin", "x", RolesSistema.Administrador));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/me", t)).StatusCode);
    }

    [Fact]
    public async Task LogoutInvalidaElTokenAunqueNoHayaVencido()
    {
        var token = await Token("admin", "ClaveAdmin123");
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/auth/me", token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Post, "/api/auth/logout", token)).StatusCode);

        var r = await Enviar(HttpMethod.Get, "/api/auth/me", token);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Contains("ya fue cerrada", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LogoutSoloAfectaALaSesionCerrada()
    {
        var a = await Token("admin", "ClaveAdmin123");
        var b = await Token("admin", "ClaveAdmin123");
        await Enviar(HttpMethod.Post, "/api/auth/logout", a);
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/auth/me", b)).StatusCode);
    }

    [Fact]
    public async Task AnalistaPuedeLeerPeroNoEscribir()
    {
        var token = await Token("ana", "ClaveAnalista1");
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/prueba/lectura", token)).StatusCode);
        var r = await Enviar(HttpMethod.Post, "/api/prueba/escritura", token);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.False(Cuerpo(await r.Content.ReadAsStringAsync()).GetProperty("exito").GetBoolean());
    }

    [Fact]
    public async Task AdministradorPuedeEscribir()
        => Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Post, "/api/prueba/escritura", await Token("admin", "ClaveAdmin123"))).StatusCode);

    [Fact]
    public async Task RutasProtegidasSinTokenDan401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/prueba/lectura")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Post, "/api/prueba/escritura")).StatusCode);
    }
}

public class SeedTests
{
    private static async Task Ejecutar(RepoFalso repo, Dictionary<string, string?> cfg)
    {
        var sp = new ServiceCollection().AddSingleton<IUsuarioSistemaRepository>(repo).BuildServiceProvider();
        var c = new ConfigurationBuilder().AddInMemoryCollection(cfg).Build();
        var s = new SeedAdminHostedService(sp, c, Microsoft.Extensions.Logging.Abstractions.NullLogger<SeedAdminHostedService>.Instance);
        await s.StartAsync(default);
        await s.ExecuteTask!;
    }

    [Fact]
    public async Task CreaAdminYAnalistaConHashBCrypt()
    {
        var repo = new RepoFalso();
        repo.Cuentas.Clear();
        await Ejecutar(repo, new()
        {
            ["Seed:AdminUsuario"] = "root", ["Seed:AdminPassword"] = "UnaClaveFuerte1",
            ["Seed:AnalistaUsuario"] = "lector", ["Seed:AnalistaPassword"] = "OtraClaveFuerte2"
        });
        Assert.Equal(2, repo.Cuentas.Count);
        Assert.Equal("administrador", repo.Cuentas.Single(c => c.NombreUsuario == "root").Rol);
        Assert.Equal("analista", repo.Cuentas.Single(c => c.NombreUsuario == "lector").Rol);
        Assert.All(repo.Cuentas, c => Assert.StartsWith("$2", c.ContrasenaHash));
    }

    [Fact]
    public async Task CreaLaCuentaDelProfesorComoAdministrador()
    {
        var repo = new RepoFalso();
        repo.Cuentas.Clear();
        await Ejecutar(repo, new() { ["Seed:ProfesorUsuario"] = "lizandro.reyes", ["Seed:ProfesorPassword"] = "ClaveProfesorDePrueba1!" });
        var cuenta = Assert.Single(repo.Cuentas);
        Assert.Equal("lizandro.reyes", cuenta.NombreUsuario);
        Assert.Equal("administrador", cuenta.Rol);
        Assert.True(BCrypt.Net.BCrypt.Verify("ClaveProfesorDePrueba1!", cuenta.ContrasenaHash));
    }

    [Fact]
    public async Task NoPisaUnaCuentaExistente()
    {
        var repo = new RepoFalso();
        var hashAntes = repo.Cuentas.Single(c => c.NombreUsuario == "admin").ContrasenaHash;
        await Ejecutar(repo, new() { ["Seed:AdminUsuario"] = "admin", ["Seed:AdminPassword"] = "OtraDistinta123" });
        Assert.Equal(3, repo.Cuentas.Count);
        Assert.Equal(hashAntes, repo.Cuentas.Single(c => c.NombreUsuario == "admin").ContrasenaHash);
    }

    [Fact]
    public async Task OmiteContrasenasCortasYConfiguracionVacia()
    {
        var repo = new RepoFalso(); repo.Cuentas.Clear();
        await Ejecutar(repo, new() { ["Seed:AdminUsuario"] = "root", ["Seed:AdminPassword"] = "corta" });
        await Ejecutar(repo, new());
        Assert.Empty(repo.Cuentas);
    }
}

public class IpClienteTests
{
    private static Microsoft.AspNetCore.Http.DefaultHttpContext Ctx(string? secreto, string? ip, string conexion = "10.1.1.1")
    {
        var c = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        c.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(conexion);
        if (secreto is not null) c.Request.Headers[AuthConfig.CabeceraSecretoProxy] = secreto;
        if (ip is not null) c.Request.Headers[AuthConfig.CabeceraIpCliente] = ip;
        return c;
    }

    [Fact]
    public void ConSecretoCorrectoUsaLaIpDelProxy()
        => Assert.Equal("203.0.113.9", AuthConfig.IpCliente(Ctx("s3creto", "203.0.113.9"), "s3creto"));

    [Theory]
    [InlineData("otro")]
    [InlineData(null)]
    public void ConSecretoIncorrectoOAusenteUsaLaIpDeLaConexion(string? enviado)
        => Assert.Equal("10.1.1.1", AuthConfig.IpCliente(Ctx(enviado, "203.0.113.9"), "s3creto"));

    [Fact]
    public void SinSecretoConfiguradoIgnoraLaCabecera()
        => Assert.Equal("10.1.1.1", AuthConfig.IpCliente(Ctx("", "203.0.113.9"), ""));

    [Fact]
    public void RechazaUnaIpDemasiadoLarga()
        => Assert.Equal("10.1.1.1", AuthConfig.IpCliente(Ctx("s3creto", new string('1', 60)), "s3creto"));
}

public class EnvArchivoTests
{
    [Fact]
    public void LeeValoresSimplesConComillasYComentarios()
    {
        var d = EnvFileConfiguration.Interpretar(["# nota", "A=uno", "B=\"dos\"", "export C='tres'", "D=", "sin igual"]);
        Assert.Equal("uno", d["A"]);
        Assert.Equal("dos", d["B"]);
        Assert.Equal("tres", d["C"]);
        Assert.Equal("", d["D"]);
        Assert.Equal(4, d.Count);
    }

    [Fact]
    public void LeeUnCertificadoDeVariasLineasEntreComillas()
    {
        var d = EnvFileConfiguration.Interpretar([
            "ANTES=1",
            "TIDB_CA_PEM=\"-----BEGIN CERTIFICATE-----",
            "QUJD",
            "REVG",
            "-----END CERTIFICATE-----\"",
            "DESPUES=2"]);
        Assert.Equal("-----BEGIN CERTIFICATE-----\nQUJD\nREVG\n-----END CERTIFICATE-----", d["TIDB_CA_PEM"]);
        Assert.Equal("1", d["ANTES"]);
        Assert.Equal("2", d["DESPUES"]);
    }

    [Fact]
    public void ContrasenaConSignoDeDolarSeConservaTalCual()
        => Assert.Equal("Clave$Falsa123", EnvFileConfiguration.Interpretar(["P=Clave$Falsa123"])["P"]);
}
