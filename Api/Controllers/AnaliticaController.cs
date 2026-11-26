using Api.Common;
using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Route("api/analitica"), Authorize(Roles = RolesSistema.Ambos)]
public class AnaliticaController(MetricasService servicio) : ControllerBase
{
    private static ActionResult<ApiResponse<T>> Datos<T>(T d) => new OkObjectResult(ApiResponse<T>.Ok(d));

    [HttpGet("resumen")]
    public async Task<ActionResult<ApiResponse<ResumenResponse>>> Resumen(CancellationToken ct) => Datos(await servicio.ResumenAsync(ct));

    [HttpGet("interacciones-por-plataforma")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoSerieResponse>>>> PorPlataforma(CancellationToken ct) => Datos(await servicio.PorPlataformaAsync(ct));

    [HttpGet("interacciones-por-tipo")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoSerieResponse>>>> PorTipo(CancellationToken ct) => Datos(await servicio.PorTipoAsync(ct));

    [HttpGet("actividad-por-hora")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CeldaHeatmapResponse>>>> ActividadPorHora(CancellationToken ct) => Datos(await servicio.ActividadPorHoraAsync(ct));

    [HttpGet("sentimiento-por-tema")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoSerieResponse>>>> SentimientoPorTema(CancellationToken ct) => Datos(await servicio.SentimientoPorTemaAsync(ct));

    [HttpGet("etiquetas-top")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoSerieResponse>>>> EtiquetasTop(CancellationToken ct) => Datos(await servicio.EtiquetasTopAsync(ct));

    [HttpGet("usuarios-por-estatus")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoSerieResponse>>>> UsuariosPorEstatus(CancellationToken ct) => Datos(await servicio.UsuariosPorEstatusAsync(ct));

    [HttpGet("usuarios-por-plataforma")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoSerieResponse>>>> UsuariosPorPlataforma(CancellationToken ct) => Datos(await servicio.UsuariosPorPlataformaAsync(ct));

    [HttpGet("usuarios-por-rango-edad")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoSerieResponse>>>> UsuariosPorRangoEdad(CancellationToken ct) => Datos(await servicio.UsuariosPorRangoEdadAsync(ct));

    [HttpGet("usuarios-por-pais")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PuntoSerieResponse>>>> UsuariosPorPais(CancellationToken ct) => Datos(await servicio.UsuariosPorPaisAsync(ct));

    [HttpGet("tendencia-diaria")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TendenciaDiariaResponse>>>> TendenciaDiaria([FromQuery] RangoFechasRequest rango, CancellationToken ct)
        => Datos(await servicio.TendenciaDiariaAsync(rango, ct));
}
