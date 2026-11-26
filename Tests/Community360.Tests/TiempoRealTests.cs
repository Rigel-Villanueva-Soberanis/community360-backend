using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Api.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Community360.Tests;

public class TiempoRealTests : ApiTestHost
{
    private async Task<WebSocket> Conectar(string? token, string? origen = null)
    {
        var cliente = App.GetTestServer().CreateWebSocketClient();
        if (origen is not null) cliente.ConfigureRequest = r => r.Headers["Origin"] = origen;
        var uri = new Uri(App.GetTestServer().BaseAddress, token is null ? "/ws" : $"/ws?access_token={token}");
        return await cliente.ConnectAsync(uri, CancellationToken.None);
    }

    private static async Task<JsonElement> Recibir(WebSocket ws)
    {
        var buf = new byte[64 * 1024];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var r = await ws.ReceiveAsync(buf, cts.Token);
        return JsonDocument.Parse(Encoding.UTF8.GetString(buf, 0, r.Count)).RootElement.Clone();
    }

    private const string Alta = """{"idUsuario":"U000001","plataforma":"Telegram","tipo":"comentario","texto":"secreto-no-viaja","tema":"musica","sentimiento":0.62,"dispositivo":"Web","etiquetas":["musica"]}""";

    [Fact]
    public async Task Sin_token_el_canal_responde_401()
    {
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => Conectar(null));
        Assert.Contains("401", ex.ToString());
    }

    [Fact]
    public async Task Token_basura_responde_401() =>
        Assert.Contains("401", (await Assert.ThrowsAnyAsync<Exception>(() => Conectar("abc.def.ghi"))).ToString());

    [Fact]
    public async Task Con_token_llega_conexion_establecida_con_metricas()
    {
        using var ws = await Conectar(TokenAnalista());
        var m = await Recibir(ws);
        Assert.Equal("conexion.establecida", m.GetProperty("tipo").GetString());
        Assert.Equal(207, m.GetProperty("metricas").GetProperty("resumen").GetProperty("totalInteracciones").GetInt32());
    }

    [Fact]
    public async Task Token_revocado_tras_logout_responde_401()
    {
        var t = TokenAnalista();
        Assert.Equal(HttpStatusCode.OK, (await Post("/api/auth/logout", "", t)).StatusCode);
        Assert.Contains("401", (await Assert.ThrowsAnyAsync<Exception>(() => Conectar(t))).ToString());
    }

    [Fact]
    public async Task Origen_de_otro_sitio_es_rechazado() =>
        Assert.Contains("403", (await Assert.ThrowsAnyAsync<Exception>(() => Conectar(TokenAdmin(), "https://malo.example"))).ToString());

    [Fact]
    public async Task Alta_modificacion_y_baja_se_difunden_sin_texto_ni_ubicacion()
    {
        using var ws = await Conectar(TokenAdmin());
        await Recibir(ws);

        Assert.Equal(HttpStatusCode.Created, (await Post("/api/interacciones", Alta, TokenAdmin())).StatusCode);
        var creada = await Recibir(ws);
        Assert.Equal("interaccion.creada", creada.GetProperty("tipo").GetString());
        var datos = creada.GetProperty("datos");
        Assert.Equal("Telegram", datos.GetProperty("plataforma").GetString());
        Assert.DoesNotContain("secreto-no-viaja", creada.GetRawText());
        Assert.False(datos.TryGetProperty("ciudad", out _));
        Assert.True(creada.TryGetProperty("metricas", out _));

        var id = datos.GetProperty("idInteraccion").GetString();
        Assert.Equal(HttpStatusCode.OK, (await Put($"/api/interacciones/{id}", """{"texto":"x","tema":"musica","sentimiento":-0.3,"etiquetas":[]}""", TokenAdmin())).StatusCode);
        Assert.Equal("interaccion.modificada", (await Recibir(ws)).GetProperty("tipo").GetString());

        Assert.Equal(HttpStatusCode.OK, (await Delete($"/api/interacciones/{id}", TokenAdmin())).StatusCode);
        Assert.Equal("interaccion.eliminada", (await Recibir(ws)).GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task Un_rechazo_422_no_difunde_nada_y_el_estado_cuenta_conexiones()
    {
        using var ws = await Conectar(TokenAdmin());
        await Recibir(ws);
        var estado = (await Json(await Get("/api/tiempo-real/estado", TokenAnalista()))).GetProperty("datos");
        Assert.Equal(1, estado.GetProperty("conexionesActivas").GetInt32());

        await Post("/api/interacciones", Alta.Replace("0.62", "9"), TokenAdmin());
        var pendiente = ws.ReceiveAsync(new byte[1024], new CancellationTokenSource(300).Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendiente);
    }

    [Fact]
    public async Task Estado_requiere_sesion() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/tiempo-real/estado")).StatusCode);
}

public class CorsTests : ApiTestHost
{
    private Task<HttpResponseMessage> Preflight(string origen, string metodo = "POST")
    {
        var r = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        r.Headers.Add("Origin", origen);
        r.Headers.Add("Access-Control-Request-Method", metodo);
        r.Headers.Add("Access-Control-Request-Headers", "content-type");
        return Http.SendAsync(r);
    }

    [Theory]
    [InlineData("https://localhost:5173")] [InlineData("http://localhost:5173")]
    public async Task El_origen_de_Vue_se_permite_con_credenciales(string origen)
    {
        var r = await Preflight(origen);
        Assert.Equal(origen, r.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", r.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Theory]
    [InlineData("https://malo.example")] [InlineData("https://localhost:5174")] [InlineData("null")]
    public async Task Otros_origenes_no_reciben_permiso(string origen) =>
        Assert.False((await Preflight(origen)).Headers.Contains("Access-Control-Allow-Origin"));

    [Fact]
    public async Task Metodo_no_permitido_no_aparece_en_la_lista_de_metodos()
    {
        var r = await Preflight("https://localhost:5173", "PATCH");
        var metodos = r.Headers.TryGetValues("Access-Control-Allow-Methods", out var v) ? string.Join(",", v) : "";
        Assert.DoesNotContain("PATCH", metodos);
    }

    [Fact]
    public void Origen_con_comodin_o_invalido_impide_arrancar()
    {
        foreach (var malo in new[] { "*", "https://*.example.com", "localhost:5173", "https://x.com/ruta" })
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:OrigenesPermitidos:0"] = malo }).Build();
            Assert.Throws<InvalidOperationException>((Action)(() => OrigenesPermitidos.Leer(cfg)));
        }
    }
}
