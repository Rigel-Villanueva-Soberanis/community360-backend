using System.Net;
using Application.Common.ErrorCatalog;
using Application.Common.Utilidades;
using Application.Services;
using Infrastructure.Repositories;
using Xunit;

namespace Community360.Tests;

public class UsuariosHttpTests : ApiTestHost
{
    [Fact]
    public async Task ListarDa200ConPaginaTotalYValoresPorDefecto()
    {
        var r = await Get("/api/usuarios", TokenAnalista());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = (await Json(r)).GetProperty("datos");
        Assert.Equal(3, d.GetProperty("items").GetArrayLength());
        Assert.Equal(1, d.GetProperty("pagina").GetInt32());
        Assert.Equal(25, d.GetProperty("tamano").GetInt32());
        Assert.Equal(3, d.GetProperty("total").GetInt32());
        Assert.Equal(1, d.GetProperty("totalPaginas").GetInt32());
        Assert.Equal("alias", UsuariosFalsos.UltimoFiltro!.Orden);
    }

    [Fact]
    public async Task SinConsentimientoNuncaSaleLaUbicacionAunqueLaBaseLaTraiga()
    {
        var items = (await Json(await Get("/api/usuarios", TokenAdmin()))).GetProperty("datos").GetProperty("items");
        var sinConsent = items.EnumerateArray().Single(x => x.GetProperty("idUsuario").GetString() == "U000002");
        foreach (var campo in new[] { "ciudad", "latitud", "longitud", "zonaHoraria" })
            Assert.False(sinConsent.TryGetProperty(campo, out _), $"no debe salir {campo}");

        var conConsent = items.EnumerateArray().Single(x => x.GetProperty("idUsuario").GetString() == "U000001");
        Assert.Equal("Mérida", conConsent.GetProperty("ciudad").GetString());
        Assert.Equal("America/Merida", conConsent.GetProperty("zonaHoraria").GetString());
    }

    [Fact]
    public async Task FechasSalenComoFechaSinHora()
    {
        var u = (await Json(await Get("/api/usuarios/U000001", TokenAdmin()))).GetProperty("datos");
        Assert.Equal("2026-01-15", u.GetProperty("fechaIngreso").GetString());
        Assert.Equal("2026-11-01", u.GetProperty("ultimaActividad").GetString());
    }

    [Fact]
    public async Task DetalleTraeLosIntereses()
    {
        var u = (await Json(await Get("/api/usuarios/U000001", TokenAdmin()))).GetProperty("datos");
        var intereses = u.GetProperty("intereses");
        Assert.Equal(2, intereses.GetArrayLength());
        Assert.True(intereses[0].GetProperty("esPrincipal").GetBoolean());
    }

    [Fact]
    public async Task LaListaNoTrae_Intereses()
    {
        var items = (await Json(await Get("/api/usuarios", TokenAdmin()))).GetProperty("datos").GetProperty("items");
        Assert.False(items[0].TryGetProperty("intereses", out _));
    }

    [Theory]
    [InlineData("U999999", HttpStatusCode.NotFound)]
    [InlineData("XYZ", HttpStatusCode.UnprocessableEntity)]
    [InlineData("u000001", HttpStatusCode.UnprocessableEntity)]
    [InlineData("U000001'%20OR%20'1'='1", HttpStatusCode.UnprocessableEntity)]
    public async Task DetalleConIdInexistenteOMalFormado(string id, HttpStatusCode esperado)
    {
        var r = await Get($"/api/usuarios/{id}", TokenAdmin());
        Assert.Equal(esperado, r.StatusCode);
        Assert.DoesNotContain("OR", await r.Content.ReadAsStringAsync());   // no repite el id del cliente
    }

    [Theory]
    [InlineData("tamano=500")] [InlineData("tamano=0")] [InlineData("pagina=0")] [InlineData("pagina=-3")] [InlineData("pagina=99999999")]
    public async Task PaginacionFueraDeRangoDa422(string query)
        => Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Get($"/api/usuarios?{query}", TokenAdmin())).StatusCode);

    [Theory]
    [InlineData("tamano=abc")] [InlineData("esArtista=quizas")]
    public async Task TiposIncorrectosEnLaConsultaDan422(string query)
        => Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Get($"/api/usuarios?{query}", TokenAdmin())).StatusCode);

    [Theory]
    [InlineData("orden=alias;DROP%20TABLE%20usuario")]
    [InlineData("orden=u.alias")]
    [InlineData("orden=1")]
    public async Task OrdenFueraDeLaListaBlancaDa422(string query)
    {
        var r = await Get($"/api/usuarios?{query}", TokenAdmin());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.DoesNotContain("DROP", await r.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("estatus=raro")] [InlineData("rangoEdad=10-20")] [InlineData("plataforma=MySpace")] [InlineData("especie=Unicornio")]
    public async Task FiltrosConValorFueraDeCatalogoODeFormatoDan422(string query)
        => Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Get($"/api/usuarios?{query}", TokenAdmin())).StatusCode);

    [Fact]
    public async Task BuscarConInyeccionSeTrataComoTextoEscapado()
    {
        var r = await Get("/api/usuarios?buscar=%27%20OR%201%3D1--%20%25_", TokenAdmin());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(@"%' OR 1=1-- \%\_%", UsuariosFalsos.UltimoFiltro!.PatronBuscar);
    }

    [Fact]
    public async Task BuscarDemasiadoLargoDa422()
        => Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Get($"/api/usuarios?buscar={new string('a', 31)}", TokenAdmin())).StatusCode);

    [Fact]
    public async Task LosFiltrosDeCatalogoLlegaranComoId()
    {
        await Get("/api/usuarios?plataforma=instagram&especie=LOBO&estatus=en_riesgo&rangoEdad=39%2B&esArtista=true&orden=Sentimiento&descendente=true&pagina=2&tamano=10", TokenAdmin());
        var f = UsuariosFalsos.UltimoFiltro!;
        Assert.Equal(3, f.IdPlataforma);
        Assert.Equal(8, f.IdEspecie);
        Assert.Equal("en_riesgo", f.Estatus);
        Assert.Equal("39+", f.RangoEdad);
        Assert.True(f.EsArtista);
        Assert.Equal("sentimiento", f.Orden);
        Assert.True(f.Descendente);
        Assert.Equal(new Paginacion(2, 10), UsuariosFalsos.UltimaPagina);
    }

    [Fact]
    public async Task InteraccionesDelUsuarioSalenConEtiquetasYTextoLiteral()
    {
        var r = await Get("/api/usuarios/U000001/interacciones?pagina=1&tamano=5", TokenAnalista());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var i = (await Json(r)).GetProperty("datos").GetProperty("items")[0];
        Assert.Equal("'; DROP TABLE x;--", i.GetProperty("texto").GetString());
        Assert.Equal(new[] { "arte", "humor" }, i.GetProperty("etiquetas").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.EndsWith("Z", i.GetProperty("fechaHoraUtc").GetString());
    }

    [Theory]
    [InlineData("/api/usuarios/U999999/interacciones", HttpStatusCode.NotFound)]
    [InlineData("/api/usuarios/XYZ/interacciones", HttpStatusCode.UnprocessableEntity)]
    [InlineData("/api/usuarios/U000001/interacciones?tamano=101", HttpStatusCode.UnprocessableEntity)]
    public async Task InteraccionesConErrores(string url, HttpStatusCode esperado)
        => Assert.Equal(esperado, (await Get(url, TokenAdmin())).StatusCode);

    [Fact]
    public async Task SinTokenDa401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/usuarios/U000001")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/usuarios/U000001/interacciones")).StatusCode);
    }

    [Fact]
    public async Task NoSePuedeCrearEditarNiBorrarUsuarios()
    {
        var t = TokenAdmin();
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Post("/api/usuarios", "{}", t)).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Delete("/api/usuarios/U000001", t)).StatusCode);
    }
}

[Collection("bd")]
public class UsuarioRepositoryBdTests : IAsyncLifetime
{
    private const string P = "U90";

    public async Task InitializeAsync()
    {
        if (!BdPrueba.Disponible) return;
        await BdPrueba.LimpiarAsync(P);
        await BdPrueba.InsertarUsuarioAsync("U900001", "ana_lobo", plataforma: 1, especie: 8, estatus: "activo", rango: "18-24", artista: true, sentimiento: 0.9m, ultima: "2026-11-10");
        await BdPrueba.InsertarUsuarioAsync("U900002", "beto_zorro", consentimiento: "no", plataforma: 3, especie: 12, estatus: "en_riesgo", rango: "25-31", sentimiento: -0.2m, ultima: "2026-10-01");
        await BdPrueba.InsertarUsuarioAsync("U900003", "100%_real", plataforma: 3, especie: 8, estatus: "inactivo", rango: "39+", sentimiento: 0.1m, ultima: "2026-09-01");
        await BdPrueba.InsertarUsuarioAsync("U900004", "x100Xreal", plataforma: 1, especie: 8, sentimiento: 0.3m, ultima: "2026-11-05");
        await BdPrueba.InsertarInteresAsync("U900001", 4, true);
        await BdPrueba.InsertarInteresAsync("U900001", 1, false);
        await BdPrueba.InsertarInteraccionAsync("I9000001", "U900001", "2026-11-01 10:00:00", texto: "primero '; DROP TABLE usuario;--", etiquetas: [4, 1]);
        await BdPrueba.InsertarInteraccionAsync("I9000002", "U900001", "2026-11-02 10:00:00", tipo: 3, texto: null, ciudad: null);
    }

    public async Task DisposeAsync() { if (BdPrueba.Disponible) await BdPrueba.LimpiarAsync(P); }

    private static UsuarioRepository Repo() => new(BdPrueba.Factory());
    private static Application.Interfaces.FiltroUsuarios F(int? pl = null, int? esp = null, string? est = null, string? rango = null, bool? art = null,
        string? buscar = null, string orden = "alias", bool desc = false)
        => new(pl, esp, est, rango, art, buscar, orden, desc);

    [FactBd]
    public async Task ListaConFiltrosCombinadosYTotal()
    {
        // Plataforma Discord + especie Lobo + artista: solo U900001 cumple las tres.
        var (items, total) = await Repo().ListarAsync(F(pl: 1, esp: 8, art: true), new Paginacion(1, 100), default);
        Assert.Equal(["U900001"], items.Select(x => x.IdUsuario).ToArray());
        Assert.Equal(1, total);
    }

    [FactBd]
    public async Task FiltraPorPlataformaEspecieEstatusRangoYArtista()
    {
        var r = Repo();
        Assert.Equal(["U900001", "U900004"], (await r.ListarAsync(F(pl: 1, esp: 8), new Paginacion(1, 100), default)).Items.Where(x => x.IdUsuario.StartsWith("U90")).Select(x => x.IdUsuario).ToArray());
        Assert.Equal(["U900002"], (await r.ListarAsync(F(est: "en_riesgo"), new Paginacion(1, 100), default)).Items.Where(x => x.IdUsuario.StartsWith("U90")).Select(x => x.IdUsuario).ToArray());
        Assert.Equal(["U900003"], (await r.ListarAsync(F(rango: "39+"), new Paginacion(1, 100), default)).Items.Where(x => x.IdUsuario.StartsWith("U90")).Select(x => x.IdUsuario).ToArray());
        Assert.Equal(["U900001"], (await r.ListarAsync(F(art: true), new Paginacion(1, 100), default)).Items.Where(x => x.IdUsuario.StartsWith("U90")).Select(x => x.IdUsuario).ToArray());
    }

    [FactBd]
    public async Task BuscarEscapadoTomaPorcentajeYGuionBajoComoTextoLiteral()
    {
        var r = Repo();
        // "100%_" literal: solo coincide con el alias "100%_real", no con "x100Xreal" (que sí coincidiría si % y _ fueran comodines)
        var (items, _) = await r.ListarAsync(F(buscar: SqlLikeHelper.Contiene("100%_")), new Paginacion(1, 100), default);
        Assert.Equal(["U900003"], items.Select(x => x.IdUsuario).ToArray());
    }

    [FactBd]
    public async Task BuscarConInyeccionNoDevuelveNadaYLaTablaSigueIntacta()
    {
        var (items, total) = await Repo().ListarAsync(F(buscar: SqlLikeHelper.Contiene("'; DROP TABLE usuario;-- ' OR '1'='1")), new Paginacion(1, 100), default);
        Assert.Empty(items);
        Assert.Equal(0, total);
        Assert.True(await BdPrueba.EscalarAsync<int>("SELECT COUNT(*) FROM usuario WHERE id_usuario LIKE 'U90%'") >= 4);
    }

    [FactBd]
    public async Task OrdenYPaginacionSonEstablesYDevuelvenElTotalReal()
    {
        var r = Repo();
        var pag1 = await r.ListarAsync(F(buscar: SqlLikeHelper.Contiene("")), new Paginacion(1, 3), default);   // "%%" = todos
        Assert.Equal(3, pag1.Items.Count);
        var pag2 = await r.ListarAsync(F(buscar: SqlLikeHelper.Contiene("")), new Paginacion(2, 3), default);
        Assert.Single(pag2.Items);
        Assert.Equal(4, pag1.Total);
        Assert.Empty(pag1.Items.Select(x => x.IdUsuario).Intersect(pag2.Items.Select(x => x.IdUsuario)));

        var porSentimiento = await r.ListarAsync(F(orden: "sentimiento", desc: true), new Paginacion(1, 100), default);
        var ids = porSentimiento.Items.Where(x => x.IdUsuario.StartsWith("U90")).Select(x => x.IdUsuario).ToArray();
        Assert.Equal(["U900001", "U900004", "U900003", "U900002"], ids);
    }

    [FactBd]
    public async Task OrdenDesconocidoCaeEnAliasSinRomperLaSentencia()
    {
        var (items, _) = await Repo().ListarAsync(F(orden: "x; DROP TABLE usuario"), new Paginacion(1, 100), default);
        Assert.NotEmpty(items);
        Assert.True(await BdPrueba.EscalarAsync<int>("SELECT COUNT(*) FROM usuario") >= 4);
    }

    [FactBd]
    public async Task ObtenerResuelveCatalogosYCiudadSoloSiHayDatos()
    {
        var a = await Repo().ObtenerAsync("U900001", default);
        Assert.Equal("Lobo", a!.Especie);
        Assert.Equal("Discord", a.Plataforma);
        Assert.NotNull(a.Ciudad);
        Assert.NotNull(a.Latitud);
        Assert.Equal(0.9m, a.SentimientoPromedio);
        Assert.Equal(new DateTime(2026, 11, 10), a.UltimaActividad);

        var b = await Repo().ObtenerAsync("U900002", default);
        Assert.Null(b!.Ciudad); Assert.Null(b.Latitud); Assert.Null(b.Longitud);
        Assert.Null(await Repo().ObtenerAsync("U999999", default));
        Assert.Null(await Repo().ObtenerAsync("U900001' OR '1'='1", default));
    }

    [FactBd]
    public async Task InteresesPrincipalPrimero()
    {
        var i = await Repo().InteresesAsync("U900001", default);
        Assert.Equal(["Cosplay", "Arte digital"], i.Select(x => x.Nombre).ToArray());
        Assert.True(i[0].EsPrincipal);
    }

    [FactBd]
    public async Task InteraccionesDelUsuarioMasRecientePrimeroConEtiquetasYSinTextoEnReaccion()
    {
        var (items, total) = await Repo().ListarInteraccionesAsync("U900001", new Paginacion(1, 10), default);
        Assert.Equal(2, total);
        Assert.Equal(["I9000002", "I9000001"], items.Select(x => x.IdInteraccion).ToArray());
        Assert.Equal("reaccion", items[0].Tipo);
        Assert.Null(items[0].Texto);
        Assert.Null(items[0].Ciudad);
        Assert.Equal("arte,humor", items[1].EtiquetasCsv);
        Assert.Equal("primero '; DROP TABLE usuario;--", items[1].Texto);
        var primera = await Repo().ListarInteraccionesAsync("U900001", new Paginacion(1, 1), default);
        Assert.Single(primera.Items);
        Assert.Equal(2, primera.Total);
    }
}
