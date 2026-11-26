using Api.Common;
using Application.Common.Seguridad;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Route("api/catalogos"), Authorize(Roles = RolesSistema.Ambos)]
public class CatalogosController(CatalogoService servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyDictionary<string, IReadOnlyList<ItemCatalogoResponse>>>>> ObtenerTodos(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyDictionary<string, IReadOnlyList<ItemCatalogoResponse>>>.Ok(await servicio.ListarTodosAsync(ct)));

    [HttpGet("{nombre}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemCatalogoResponse>>>> ObtenerPorNombre(string nombre, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<ItemCatalogoResponse>>.Ok(await servicio.ListarAsync(nombre, ct)));
}
