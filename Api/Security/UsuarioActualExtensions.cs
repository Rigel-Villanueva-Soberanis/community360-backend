using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Infrastructure.Security;

namespace Api.Security;

/// <summary>Lee los datos de la sesión desde los claims del token (nombres cortos: sub, name, role, jti, exp).</summary>
public static class UsuarioActualExtensions
{
    public static int IdUsuario(this ClaimsPrincipal u) => int.Parse(u.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    public static string NombreUsuario(this ClaimsPrincipal u) => u.FindFirstValue(JwtGenerator.ClaimNombre)!;
    public static string Rol(this ClaimsPrincipal u) => u.FindFirstValue(JwtGenerator.ClaimRol)!;
    public static string Jti(this ClaimsPrincipal u) => u.FindFirstValue(JwtRegisteredClaimNames.Jti)!;
    public static DateTime ExpiraUtc(this ClaimsPrincipal u)
        => DateTimeOffset.FromUnixTimeSeconds(long.Parse(u.FindFirstValue(JwtRegisteredClaimNames.Exp)!)).UtcDateTime;
}
