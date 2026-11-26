using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using Api.Configuration;
using Api.Middlewares;
using Application.Common.Utilidades;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Community360.Tests;

public record EcoReq([Required, StringLength(5)] string? Corto, string? Texto, int Numero);

[ApiController, Route("prueba")]
public class EcoPruebaController : ControllerBase
{
    [HttpPost("eco")]
    public IActionResult Eco(EcoReq r) => Ok(new { texto = NoSqlSanitizer.Limpiar(r.Texto, "texto", 500), r.Numero });
}

/// <summary>Servidor mínimo con las mismas protecciones del API (sin bases de datos) para probar el comportamiento HTTP.</summary>
public class HttpSeguridadTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _http = null!;

    public async Task InitializeAsync()
    {
        var b = WebApplication.CreateBuilder();
        b.WebHost.UseTestServer();
        b.AddSeguridadBasica();
        b.Services.AddControllers().AddApplicationPart(typeof(EcoPruebaController).Assembly);
        _app = b.Build();
        _app.UseSeguridadBasica();
        _app.UseMiddleware<ExceptionMiddleware>();
        _app.MapControllers();
        await _app.StartAsync();
        _http = _app.GetTestClient();
    }

    public async Task DisposeAsync() { _http.Dispose(); await _app.DisposeAsync(); }

    private Task<HttpResponseMessage> Post(string json)
        => _http.PostAsync("/prueba/eco", new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task<JsonElement> Cuerpo(HttpResponseMessage r)
        => JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task TextoConInyeccionSqlSeGuardaComoTexto()
    {
        var r = await Post("""{"corto":"ok","texto":"'; DROP TABLE interaccion;--","numero":1}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("'; DROP TABLE interaccion;--", (await Cuerpo(r)).GetProperty("texto").GetString());
    }

    [Fact]
    public async Task ObjetoConOperadorNoSqlEnCampoDeTextoSeVuelveTextoLiteral()
    {
        var r = await Post("""{"corto":"ok","texto":{"$ne":null},"numero":1}""");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("""{"$ne":null}""", (await Cuerpo(r)).GetProperty("texto").GetString());
    }

    [Fact]
    public async Task ArregloYNumeroEnCampoDeTextoTambienSeVuelvenTexto()
    {
        var r = await Post("""{"corto":"ok","texto":[1,{"$gt":""}],"numero":1}""");
        Assert.Equal("""[1,{"$gt":""}]""", (await Cuerpo(r)).GetProperty("texto").GetString());
        r = await Post("""{"corto":"ok","texto":123,"numero":1}""");
        Assert.Equal("123", (await Cuerpo(r)).GetProperty("texto").GetString());
    }

    [Fact]
    public async Task JsonMalFormadoDa400()
    {
        var r = await Post("""{"corto":"ok","texto":""");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.False((await Cuerpo(r)).GetProperty("exito").GetBoolean());
    }

    [Fact]
    public async Task TextoEnCampoNumericoDa400()
        => Assert.Equal(HttpStatusCode.BadRequest, (await Post("""{"corto":"ok","numero":"abc"}""")).StatusCode);

    [Fact]
    public async Task ReglaDeDatosIncumplidaDa422ConDetalle()
    {
        var r = await Post("""{"corto":"demasiado largo","numero":1}""");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.True((await Cuerpo(r)).GetProperty("errores").GetArrayLength() > 0);
    }

    [Fact]
    public async Task TextoMasLargoQueElMaximoDa422()
    {
        var r = await Post($$"""{"corto":"ok","texto":"{{new string('a', 501)}}","numero":1}""");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    [Fact]
    public async Task LasRespuestasLlevanCabecerasDeSeguridad()
    {
        var r = await Post("""{"corto":"ok","numero":1}""");
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", r.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", r.Headers.GetValues("Referrer-Policy").Single());
    }
}
