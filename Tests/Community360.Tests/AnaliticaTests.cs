using System.Net;
using Application.Interfaces;
using Application.Services;
using Xunit;

namespace Community360.Tests;

public class AnaliticaTests : ApiTestHost
{
    [Theory]
    [InlineData("resumen")] [InlineData("interacciones-por-plataforma")] [InlineData("interacciones-por-tipo")] [InlineData("actividad-por-hora")]
    [InlineData("sentimiento-por-tema")] [InlineData("etiquetas-top")] [InlineData("usuarios-por-estatus")] [InlineData("usuarios-por-plataforma")]
    [InlineData("usuarios-por-rango-edad")] [InlineData("usuarios-por-pais")] [InlineData("tendencia-diaria?desde=2026-01-01&hasta=2026-01-31")]
    public async Task Cada_endpoint_responde_200_a_admin_y_analista(string ruta)
    {
        Assert.Equal(HttpStatusCode.OK, (await Get($"/api/analitica/{ruta}", TokenAdmin())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get($"/api/analitica/{ruta}", TokenAnalista())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get($"/api/analitica/{ruta}")).StatusCode);
    }

    [Fact]
    public async Task Resumen_devuelve_los_indicadores()
    {
        var d = (await Json(await Get("/api/analitica/resumen", TokenAdmin()))).GetProperty("datos");
        Assert.Equal(207, d.GetProperty("totalInteracciones").GetInt32());
        Assert.Equal(30.4m, d.GetProperty("porcentajeNegativo").GetDecimal());
    }

    [Fact]
    public async Task Sin_datos_devuelve_ceros_y_listas_vacias()
    {
        AnaliticaFalsa.Vacio = true;
        var d = (await Json(await Get("/api/analitica/resumen", TokenAdmin()))).GetProperty("datos");
        Assert.Equal(0, d.GetProperty("totalInteracciones").GetInt32());
        Assert.Equal(0m, d.GetProperty("sentimientoPromedio").GetDecimal());
        Assert.Equal(0, (await Json(await Get("/api/analitica/interacciones-por-plataforma", TokenAdmin()))).GetProperty("datos").GetArrayLength());
    }

    [Fact]
    public async Task La_dona_suma_exactamente_100()
    {
        var items = (await Json(await Get("/api/analitica/interacciones-por-plataforma", TokenAdmin()))).GetProperty("datos").EnumerateArray();
        Assert.Equal(100.0m, items.Sum(i => i.GetProperty("valor").GetDecimal()));
    }

    [Theory]
    [InlineData("desde=2026-01-02&hasta=2026-01-01")]
    [InlineData("desde=2026-01-01&hasta=2028-01-01")]
    [InlineData("hasta=2026-01-01")]
    [InlineData("desde=2026-01-01")]
    public async Task Rango_invalido_es_422(string q) =>
        Assert.Equal((HttpStatusCode)422, (await Get($"/api/analitica/tendencia-diaria?{q}", TokenAdmin())).StatusCode);

    [Theory]
    [InlineData("desde=' OR 1=1--&hasta=2026-01-01")]
    [InlineData("desde=2026-01-01&hasta=2026-01-01;DROP TABLE x")]
    [InlineData("desde[$ne]=1&hasta=2026-01-01")]
    public async Task Fecha_con_inyeccion_es_rechazada(string q)
    {
        var r = await Get($"/api/analitica/tendencia-diaria?{q}", TokenAdmin());
        Assert.True(r.StatusCode is HttpStatusCode.BadRequest or (HttpStatusCode)422, r.StatusCode.ToString());
    }

    [Fact]
    public async Task La_tendencia_incluye_el_dia_final()
    {
        await Get("/api/analitica/tendencia-diaria?desde=2026-01-01&hasta=2026-01-31", TokenAdmin());
        Assert.Equal(new DateTime(2026, 1, 1), AnaliticaFalsa.Desde);
        Assert.Equal(new DateTime(2026, 2, 1), AnaliticaFalsa.HastaExclusivo);
    }

    [Theory]
    [InlineData(new[] { 1, 1, 1 })] [InlineData(new[] { 1, 2, 4 })] [InlineData(new[] { 207 })] [InlineData(new[] { 3, 3, 3, 3, 3, 3, 3 })] [InlineData(new[] { 1, 1000 })]
    public void Porcentajes_siempre_suma_100(int[] conteos)
    {
        var p = MetricasService.Porcentajes(conteos.Select((c, i) => new FilaAgrupada($"x{i}", c, c)).ToList());
        Assert.Equal(100.0m, p.Sum(x => x.Valor));
    }

    [Fact] public void Porcentajes_sin_datos_es_vacio() => Assert.Empty(MetricasService.Porcentajes([]));
}

[Collection("bd")]
public class AnaliticaBdTests : IAsyncLifetime
{
    private readonly Infrastructure.Repositories.AnaliticaRepository _repo = new(BdPrueba.Factory());
    private static async Task Limpiar()
    {
        await BdPrueba.EjecutarAsync("DELETE FROM interaccion_etiqueta WHERE id_interaccion LIKE 'I92%'");
        await BdPrueba.LimpiarAsync("U92");
    }
    public async Task InitializeAsync() { if (BdPrueba.Disponible) await Limpiar(); }
    public async Task DisposeAsync() { if (BdPrueba.Disponible) await Limpiar(); }

    [FactBd]
    public async Task Resumen_coincide_con_conteos_directos_y_recalcula_tras_un_alta()
    {
        var antes = await _repo.ResumenAsync(default);
        Assert.Equal(await BdPrueba.EscalarAsync<int>("SELECT COUNT(*) FROM interaccion"), antes.TotalInteracciones);

        await BdPrueba.InsertarUsuarioAsync("U920001", "bd_analitica");
        await BdPrueba.InsertarInteraccionAsync("I920001", "U920001", "2026-11-01 12:00:00", sentimiento: -0.9m, etiquetas: [1]);
        var despues = await _repo.ResumenAsync(default);
        Assert.Equal(antes.TotalInteracciones + 1, despues.TotalInteracciones);

        await BdPrueba.EjecutarAsync("DELETE FROM interaccion_etiqueta WHERE id_interaccion = 'I920001'");
        await BdPrueba.EjecutarAsync("DELETE FROM interaccion WHERE id_interaccion = 'I920001'");
        Assert.Equal(antes.TotalInteracciones, (await _repo.ResumenAsync(default)).TotalInteracciones);
    }

    [FactBd]
    public async Task Las_agrupaciones_suman_el_total()
    {
        var total = (await _repo.ResumenAsync(default)).TotalInteracciones;
        Assert.Equal(total, (await _repo.InteraccionesPorPlataformaAsync(default)).Sum(f => f.Total));
        Assert.Equal(total, (await _repo.InteraccionesPorTipoAsync(default)).Sum(f => f.Total));
        Assert.Equal(total, (await _repo.SentimientoPorTemaAsync(default)).Sum(f => f.Total));
        Assert.Equal(total, (await _repo.ActividadPorHoraAsync(default)).Sum(c => c.Total));
        Assert.Equal(await BdPrueba.EscalarAsync<int>("SELECT COUNT(*) FROM usuario"), (await _repo.UsuariosPorEstatusAsync(default)).Sum(f => f.Total));
        Assert.True((await _repo.EtiquetasTopAsync(3, default)).Count <= 3);
    }

    [FactBd]
    public async Task Usuarios_por_pais_solo_cuenta_consentimiento_si()
    {
        var porPais = (await _repo.UsuariosPorPaisAsync(default)).Sum(f => f.Total);
        Assert.Equal(await BdPrueba.EscalarAsync<int>("SELECT COUNT(*) FROM usuario WHERE consentimiento_datos = 'si' AND id_ciudad IS NOT NULL"), porPais);
    }

    [FactBd]
    public async Task Tendencia_filtra_por_rango_y_excluye_el_limite_superior()
    {
        await BdPrueba.InsertarUsuarioAsync("U920002", "bd_tendencia");
        await BdPrueba.InsertarInteraccionAsync("I920002", "U920002", "2099-05-01 23:59:59", sentimiento: 0.5m);
        await BdPrueba.InsertarInteraccionAsync("I920003", "U920002", "2099-05-02 00:00:00", sentimiento: 0.1m);
        var r = await _repo.TendenciaDiariaAsync(new DateTime(2099, 5, 1), new DateTime(2099, 5, 2), default);
        var dia = Assert.Single(r);
        Assert.Equal(new DateOnly(2099, 5, 1), dia.Fecha);
        Assert.Equal(1, dia.Total);
    }
}
