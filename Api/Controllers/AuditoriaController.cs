using System.Text.Json;
using Api.Common;
using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Route("api/auditoria"), Authorize(Roles = RolesSistema.Administrador)]
public class AuditoriaController(AuditoriaService servicio) : ControllerBase
{
    private static ActionResult<ApiResponse<T>> Datos<T>(T d) => new OkObjectResult(ApiResponse<T>.Ok(d));

    [HttpGet("cuarentena")]
    public async Task<ActionResult<ApiResponse<PaginaResponse<JsonElement>>>> Cuarentena([FromQuery] FiltroAuditoriaRequest filtro, CancellationToken ct)
        => Datos(await servicio.CuarentenaAsync(filtro, ct));

    [HttpGet("cuarentena/{idInteraccion}")]
    public async Task<ActionResult<ApiResponse<JsonElement>>> CuarentenaPorId(string idInteraccion, CancellationToken ct)
        => Datos(await servicio.CuarentenaPorIdAsync(idInteraccion, ct));

    [HttpGet("diario-cambios")]
    public async Task<ActionResult<ApiResponse<PaginaResponse<JsonElement>>>> DiarioCambios([FromQuery] FiltroAuditoriaRequest filtro, CancellationToken ct)
        => Datos(await servicio.DiarioAsync(filtro, ct));

    [HttpGet("bitacora-carga")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JsonElement>>>> BitacoraCarga(CancellationToken ct)
        => Datos(await servicio.BitacoraCargaAsync(ct));

    [HttpGet("bitacora-api")]
    public async Task<ActionResult<ApiResponse<PaginaResponse<JsonElement>>>> BitacoraApi([FromQuery] FiltroAuditoriaRequest filtro, CancellationToken ct)
        => Datos(await servicio.BitacoraApiAsync(filtro, ct));

    [HttpGet("resumen")]
    public async Task<ActionResult<ApiResponse<IReadOnlyDictionary<string, long>>>> Resumen(CancellationToken ct)
        => Datos(await servicio.ResumenAsync(ct));
}
