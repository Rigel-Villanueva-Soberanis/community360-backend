using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Common.Seguridad;
using Application.Interfaces;
using Domain;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security;

/// <summary>Claims con nombres cortos: sub (id), name, role y jti (id único del token, sirve para el logout).</summary>
public class JwtGenerator(JwtSettings cfg) : IJwtGenerator
{
    public const string ClaimNombre = "name";
    public const string ClaimRol = "role";

    public (string Token, DateTime ExpiraUtc) Crear(UsuarioSistema u)
    {
        var expira = DateTime.UtcNow.AddMinutes(cfg.MinutosVida);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim(ClaimNombre, u.NombreUsuario),
            new Claim(ClaimRol, u.Rol),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg.Key));
        var jwt = new JwtSecurityToken(cfg.Issuer, cfg.Audience, claims, notBefore: DateTime.UtcNow, expires: expira,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }
}
