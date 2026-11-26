using Api.Common;
using Application.Common.Seguridad;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Route("api/tiempo-real"), Authorize(Roles = RolesSistema.Ambos)]
public class TiempoRealController(IConexionesTiempoReal conexiones) : ControllerBase
{
    public record EstadoTiempoReal(string Ruta, int ConexionesActivas, string[] Mensajes);

    /// <summary>El canal en sí (/ws) no es HTTP normal y no sale en Swagger; este endpoint lo documenta.</summary>
    [HttpGet("estado")]
    public ActionResult<ApiResponse<EstadoTiempoReal>> Estado()
        => Ok(ApiResponse<EstadoTiempoReal>.Ok(new EstadoTiempoReal("/ws", conexiones.Conectados,
            ["conexion.establecida", "interaccion.creada", "interaccion.modificada", "interaccion.eliminada"])));
}
