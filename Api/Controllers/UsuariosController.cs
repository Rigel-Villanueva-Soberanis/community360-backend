using Api.Common;
using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Route("api/usuarios"), Authorize(Roles = RolesSistema.Ambos)]
public class UsuariosController(UsuarioService servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginaResponse<UsuarioResponse>>>> Listar([FromQuery] FiltroUsuariosRequest filtro, CancellationToken ct)
        => Ok(ApiResponse<PaginaResponse<UsuarioResponse>>.Ok(await servicio.ListarAsync(filtro, ct)));

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UsuarioResponse>>> ObtenerPorId(string id, CancellationToken ct)
        => Ok(ApiResponse<UsuarioResponse>.Ok(await servicio.ObtenerAsync(id, ct)));

    [HttpGet("{id}/interacciones")]
    public async Task<ActionResult<ApiResponse<PaginaResponse<InteraccionResponse>>>> ListarInteracciones(string id, [FromQuery] PaginacionRequest paginacion, CancellationToken ct)
        => Ok(ApiResponse<PaginaResponse<InteraccionResponse>>.Ok(await servicio.ListarInteraccionesAsync(id, paginacion, ct)));
}
