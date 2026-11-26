using Api.Common;
using Api.Security;
using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Route("api/interacciones"), Authorize]
public class InteraccionesController(InteraccionService servicio) : ControllerBase
{
    [HttpGet, Authorize(Roles = RolesSistema.Ambos)]
    public async Task<ActionResult<ApiResponse<PaginaResponse<InteraccionResponse>>>> Listar([FromQuery] FiltroInteraccionesRequest filtro, CancellationToken ct)
        => Ok(ApiResponse<PaginaResponse<InteraccionResponse>>.Ok(await servicio.ListarAsync(filtro, ct)));

    [HttpGet("{id}"), Authorize(Roles = RolesSistema.Ambos)]
    public async Task<ActionResult<ApiResponse<InteraccionResponse>>> ObtenerPorId(string id, CancellationToken ct)
        => Ok(ApiResponse<InteraccionResponse>.Ok(await servicio.ObtenerAsync(id, ct)));

    [HttpPost, Authorize(Roles = RolesSistema.Administrador)]
    public async Task<ActionResult<ApiResponse<InteraccionResponse>>> Crear(CreateInteraccionRequest req, CancellationToken ct)
    {
        var r = await servicio.CrearAsync(req, User.NombreUsuario(), ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = r.IdInteraccion }, ApiResponse<InteraccionResponse>.Ok(r, "Interacción creada."));
    }

    [HttpPut("{id}"), Authorize(Roles = RolesSistema.Administrador)]
    public async Task<ActionResult<ApiResponse<InteraccionResponse>>> Actualizar(string id, UpdateInteraccionRequest req, CancellationToken ct)
        => Ok(ApiResponse<InteraccionResponse>.Ok(await servicio.ActualizarAsync(id, req, User.NombreUsuario(), ct), "Interacción actualizada."));

    [HttpDelete("{id}"), Authorize(Roles = RolesSistema.Administrador)]
    public async Task<ActionResult<ApiResponse<InteraccionResponse>>> Eliminar(string id, CancellationToken ct)
        => Ok(ApiResponse<InteraccionResponse>.Ok(await servicio.EliminarAsync(id, User.NombreUsuario(), ct), "Interacción eliminada."));
}
