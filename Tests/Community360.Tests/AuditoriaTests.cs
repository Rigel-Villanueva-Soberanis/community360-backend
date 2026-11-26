using System.Net;
using Xunit;

namespace Community360.Tests;

public class AuditoriaTests : ApiTestHost
{
    [Theory]
    [InlineData("resumen")] [InlineData("cuarentena")] [InlineData("cuarentena/I0000001")] [InlineData("diario-cambios")] [InlineData("bitacora-carga")] [InlineData("bitacora-api")]
    public async Task Admin_200_analista_403_sin_token_401(string ruta)
    {
        Assert.Equal(HttpStatusCode.OK, (await Get($"/api/auditoria/{ruta}", TokenAdmin())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Get($"/api/auditoria/{ruta}", TokenAnalista())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get($"/api/auditoria/{ruta}")).StatusCode);
    }

    [Fact]
    public async Task Resumen_trae_los_conteos_de_las_cuatro_colecciones()
    {
        var d = (await Json(await Get("/api/auditoria/resumen", TokenAdmin()))).GetProperty("datos");
        Assert.Equal(793, d.GetProperty("cuarentena").GetInt64());
        Assert.Equal(171, d.GetProperty("diario_cambios").GetInt64());
        Assert.Equal(1, d.GetProperty("bitacora_carga").GetInt64());
    }

    [Fact]
    public async Task Cuarentena_nunca_muestra_ubicacion_y_deja_el_texto_literal()
    {
        var item = (await Json(await Get("/api/auditoria/cuarentena", TokenAdmin()))).GetProperty("datos").GetProperty("items")[0];
        foreach (var campo in new[] { "pais", "estado", "ciudad", "latitud", "longitud", "id_ciudad" })
            Assert.False(item.TryGetProperty(campo, out _), campo);
        Assert.Equal("pendiente", item.GetProperty("estado_revision").GetString());
        Assert.Equal("<b>hola</b>", item.GetProperty("texto").GetString());
    }

    [Fact]
    public async Task Cuarentena_por_id_sin_ubicacion_inexistente_404_y_formato_invalido_422()
    {
        var d = (await Json(await Get("/api/auditoria/cuarentena/I0000001", TokenAdmin()))).GetProperty("datos");
        Assert.False(d.TryGetProperty("ciudad", out _));
        Assert.Equal(HttpStatusCode.NotFound, (await Get("/api/auditoria/cuarentena/I0999999", TokenAdmin())).StatusCode);
        Assert.Equal((HttpStatusCode)422, (await Get("/api/auditoria/cuarentena/abc", TokenAdmin())).StatusCode);
        var r = await Get("/api/auditoria/cuarentena/..%2F..%2Fx", TokenAdmin());
        Assert.True(r.StatusCode is HttpStatusCode.NotFound or (HttpStatusCode)422, r.StatusCode.ToString());
    }

    [Fact]
    public async Task Estado_valido_llega_al_repositorio_y_la_paginacion_se_traduce()
    {
        await Get("/api/auditoria/cuarentena?estadoRevision=PENDIENTE&pagina=3&tamano=10", TokenAdmin());
        Assert.Equal("pendiente", AuditoriaFalsa.UltimoEstado);
        Assert.Equal(10, AuditoriaFalsa.UltimoLimite);
        Assert.Equal(20, AuditoriaFalsa.UltimoOffset);
    }

    [Theory]
    [InlineData("/api/auditoria/cuarentena?estadoRevision=pendiente' OR '1'='1")]
    [InlineData("/api/auditoria/cuarentena?estadoRevision=x\" OR 1=1 --")]
    [InlineData("/api/auditoria/bitacora-api?operacion=alta' OR '1'='1")]
    [InlineData("/api/auditoria/cuarentena?tamano=100000")]
    [InlineData("/api/auditoria/cuarentena?pagina=0")]
    public async Task Valores_fuera_de_lista_son_422(string url) =>
        Assert.Equal((HttpStatusCode)422, (await Get(url, TokenAdmin())).StatusCode);

    [Fact]
    public async Task Operador_nosql_en_el_filtro_no_llega_al_repositorio()
    {
        var r = await Get("/api/auditoria/cuarentena?estadoRevision[$ne]=x", TokenAdmin());
        Assert.True(r.StatusCode is HttpStatusCode.BadRequest or (HttpStatusCode)422 || AuditoriaFalsa.UltimoEstado is null, r.StatusCode.ToString());
        Assert.Null(AuditoriaFalsa.UltimoEstado is { } e && e.Contains('$') ? e : null);
    }

    [Fact]
    public async Task Operacion_valida_pasa()
    {
        Assert.Equal(HttpStatusCode.OK, (await Get("/api/auditoria/bitacora-api?operacion=Rechazo", TokenAdmin())).StatusCode);
        Assert.Equal("rechazo", AuditoriaFalsa.UltimaOperacion);
    }
}
