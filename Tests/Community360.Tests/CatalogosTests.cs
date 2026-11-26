using System.Net;
using Application.Common.ErrorCatalog;
using Application.Services;
using Xunit;

namespace Community360.Tests;

public class CatalogosHttpTests : ApiTestHost
{
    [Fact]
    public async Task TodosJuntosDa200ConLosSieteCatalogos()
    {
        var r = await Get("/api/catalogos", TokenAnalista());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = (await Json(r)).GetProperty("datos");
        Assert.Equal(7, d.EnumerateObject().Count());
        Assert.Equal(6, d.GetProperty("plataformas").GetArrayLength());
    }

    [Fact]
    public async Task TiposDeInteraccionTraenAdmiteTextoYLosDemasNo()
    {
        var d = (await Json(await Get("/api/catalogos", TokenAdmin()))).GetProperty("datos");
        var reaccion = d.GetProperty("tipos-interaccion").EnumerateArray().Single(x => x.GetProperty("nombre").GetString() == "reaccion");
        Assert.False(reaccion.GetProperty("admiteTexto").GetBoolean());
        Assert.False(d.GetProperty("temas")[0].TryGetProperty("admiteTexto", out _));
    }

    [Fact]
    public async Task UnCatalogoPorNombreSinImportarMayusculas()
    {
        var r = await Get("/api/catalogos/PLATAFORMAS", TokenAdmin());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("Discord", (await Json(r)).GetProperty("datos")[0].GetProperty("nombre").GetString());
    }

    [Theory]
    [InlineData("usuario;DROP TABLE x")]
    [InlineData("%27%20OR%201%3D1")]
    [InlineData("usuario_sistema")]
    [InlineData("..%2F..%2Fetc")]
    public async Task CatalogoInexistenteDa404SinRepetirElTextoDelCliente(string nombre)
    {
        var r = await Get($"/api/catalogos/{nombre}", TokenAdmin());
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        var cuerpo = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("DROP", cuerpo);
        Assert.DoesNotContain("usuario_sistema", cuerpo);
        Assert.Contains("plataformas", cuerpo);   // sí lista los válidos
    }

    [Fact]
    public async Task SinTokenDa401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/catalogos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/catalogos/temas")).StatusCode);
    }

    [Fact]
    public async Task SoloLecturaNoHayPostPutNiDelete()
    {
        var t = TokenAdmin();
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Post("/api/catalogos/temas", "{}", t)).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Delete("/api/catalogos/temas", t)).StatusCode);
    }
}

public class CatalogoServiceTests
{
    private static CatalogoService Servicio() => new(new CatalogoFalso());

    [Theory]
    [InlineData("Instagram", 3)] [InlineData("instagram", 3)] [InlineData("  INSTAGRAM ", 3)] [InlineData("Twitter/X", 6)]
    public async Task ResuelveElNombreSinImportarMayusculasNiEspacios(string nombre, int id)
        => Assert.Equal(id, (await Servicio().PlataformaAsync(nombre, "plataforma", default)).Id);

    [Theory]
    [InlineData("MySpace")] [InlineData("")] [InlineData(null)] [InlineData("X'; DROP TABLE interaccion;--")]
    public async Task ValorFueraDelCatalogoDa422ConLasOpciones(string? nombre)
    {
        var e = await Assert.ThrowsAsync<DomainException>(() => Servicio().PlataformaAsync(nombre, "plataforma", default));
        Assert.Equal(422, e.StatusCode);
        Assert.Equal("plataforma", e.Errores![0].Campo);
        Assert.Contains("Instagram", e.Errores[0].Mensaje);
        Assert.DoesNotContain("DROP", e.Errores[0].Mensaje);
    }

    [Fact]
    public async Task TipoTraeAdmiteTexto()
    {
        Assert.False((await Servicio().TipoAsync("reaccion", "tipo", default)).AdmiteTexto);
        Assert.True((await Servicio().TipoAsync("Comentario", "tipo", default)).AdmiteTexto);
    }

    [Fact]
    public async Task EtiquetasSeQuitanRepetidasYVacias()
    {
        var r = await Servicio().EtiquetasAsync(["arte", "ARTE", " ", "humor"], "etiquetas", default);
        Assert.Equal([1, 4], r.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task UnaEtiquetaInvalidaDa422()
        => Assert.Equal(422, (await Assert.ThrowsAsync<DomainException>(() => Servicio().EtiquetasAsync(["arte", "inventada"], "etiquetas", default))).StatusCode);

    [Fact]
    public async Task SinEtiquetasDaListaVacia()
        => Assert.Empty(await Servicio().EtiquetasAsync(null, "etiquetas", default));
}
