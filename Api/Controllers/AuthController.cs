using Api.Common;
using Api.Configuration;
using Api.Security;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting(AuthConfig.PoliticaLogin)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest req, CancellationToken ct)
    {
        var r = await auth.LoginAsync(req, ct);
        Response.Cookies.Append(AuthConfig.CookieToken, r.Token, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = r.ExpiraUtc
        });
        return Ok(ApiResponse<LoginResponse>.Ok(r, "Sesión iniciada."));
    }

    [HttpGet("me"), Authorize]
    public ActionResult<ApiResponse<UsuarioActualResponse>> Me()
        => Ok(ApiResponse<UsuarioActualResponse>.Ok(new(User.IdUsuario(), User.NombreUsuario(), User.Rol())));

    [HttpPost("logout"), Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        await auth.CerrarSesionAsync(User.Jti(), User.ExpiraUtc());
        Response.Cookies.Delete(AuthConfig.CookieToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Sesión cerrada."));
    }
}
