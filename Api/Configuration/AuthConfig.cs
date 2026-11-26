using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Api.Common;
using Application.Common.ErrorsAuth;
using Application.Common.Seguridad;
using Application.Interfaces;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Configuration;

public static class AuthConfig
{
    public const string CookieToken = "c360_token";
    public const string PoliticaLogin = "login";
    public const int IntentosLoginPorMinuto = 5;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddAutenticacion(this IServiceCollection s, IConfiguration cfg)
    {
        var jwt = JwtSettings.Desde(k => cfg[k]);   // lanza un mensaje claro si falta JWT_KEY
        s.AddSingleton(jwt);
        s.AddSingleton<ITokenRevocadoStore, TokenRevocadoStore>();
        s.AddSingleton<IJwtGenerator, JwtGenerator>();

        s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            o.MapInboundClaims = false;   // los claims conservan sus nombres cortos: sub, name, role, jti
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = JwtGenerator.ClaimNombre, RoleClaimType = JwtGenerator.ClaimRol
            };
            o.Events = new JwtBearerEvents
            {
                // Orden de lectura: cabecera Bearer (Postman, Swagger) → cookie HttpOnly (navegador) → ?access_token= solo en /ws
                OnMessageReceived = ctx =>
                {
                    // Si viene la cabecera Authorization: Bearer, manda ella (JwtBearer la lee solo cuando Token queda vacío).
                    var cabecera = ctx.Request.Headers.Authorization.ToString();
                    if (cabecera.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
                    if (string.IsNullOrEmpty(ctx.Token))
                    {
                        ctx.Token = ctx.Request.Cookies[CookieToken];
                        if (string.IsNullOrEmpty(ctx.Token) && ctx.Request.Path.StartsWithSegments("/ws"))
                            ctx.Token = ctx.Request.Query["access_token"];
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = ctx =>
                {
                    var jti = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                    var store = ctx.HttpContext.RequestServices.GetRequiredService<ITokenRevocadoStore>();
                    if (jti is null || store.EstaRevocado(jti)) ctx.Fail(AuthErrors.MensajeSesionCerrada);
                    return Task.CompletedTask;
                },
                OnChallenge = ctx =>
                {
                    ctx.HandleResponse();
                    var mensaje = ctx.AuthenticateFailure?.Message == AuthErrors.MensajeSesionCerrada
                        ? AuthErrors.MensajeSesionCerrada : AuthErrors.MensajeSesionInvalida;
                    return Responder(ctx.HttpContext, 401, mensaje);
                },
                OnForbidden = ctx => Responder(ctx.HttpContext, 403, AuthErrors.MensajeSinPermiso)
            };
        });
        s.AddAuthorization();

        // Límite de intentos de login: 5 por minuto por IP.
        s.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.OnRejected = (ctx, _) =>
            {
                ctx.HttpContext.Response.Headers.RetryAfter = "60";
                return new ValueTask(Responder(ctx.HttpContext, 429, "Demasiados intentos. Espera un minuto antes de reintentar."));
            };
            var secretoProxy = cfg["Proxy:Secreto"];
            o.AddPolicy(PoliticaLogin, ctx => RateLimitPartition.GetFixedWindowLimiter(
                IpCliente(ctx, secretoProxy),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = IntentosLoginPorMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
        });
        return s;
    }

    public const string CabeceraSecretoProxy = "X-Proxy-Secret";
    public const string CabeceraIpCliente = "X-Cliente-IP";

    /// <summary>
    /// IP con la que se cuentan los intentos de login. Detrás del proxy de Cloudflare todas las peticiones llegan desde
    /// los servidores de Cloudflare, así que el proxy manda la IP real en X-Cliente-IP junto con un secreto compartido
    /// (Proxy:Secreto). Solo se cree en esa cabecera cuando el secreto coincide; si no, vale la IP de la conexión.
    /// </summary>
    public static string IpCliente(HttpContext ctx, string? secretoProxy)
    {
        if (!string.IsNullOrEmpty(secretoProxy)
            && ctx.Request.Headers[CabeceraSecretoProxy].ToString() is { Length: > 0 } recibido
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recibido), Encoding.UTF8.GetBytes(secretoProxy))
            && ctx.Request.Headers[CabeceraIpCliente].ToString().Trim() is { Length: > 0 and <= 45 } ip)
            return ip;
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
    }

    private static Task Responder(HttpContext ctx, int status, string mensaje)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(ApiResponse<object>.Falla(mensaje), Json));
    }
}
