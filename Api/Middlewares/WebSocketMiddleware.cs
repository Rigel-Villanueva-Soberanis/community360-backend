using Application.Common.Seguridad;
using Api.Security;
using Infrastructure.Realtime;

namespace Api.Middlewares;

/// <summary>
/// Canal /ws. UseAuthentication ya leyó el token (cabecera, cookie o ?access_token= solo aquí) y revisó que no esté revocado.
/// Aquí se exige sesión válida y rol, que el origen sea el mismo sitio (o no haya, como en Postman) y un tope de conexiones.
/// </summary>
public class WebSocketMiddleware(RequestDelegate next, WebSocketHub hub, Api.Configuration.OrigenesPermitidos origenes)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!ctx.Request.Path.Equals("/ws", StringComparison.OrdinalIgnoreCase)) { await next(ctx); return; }

        if (ctx.User.Identity?.IsAuthenticated != true) { await Rechazar(ctx, 401, "Token ausente, inválido o revocado."); return; }
        if (!ctx.User.IsInRole(RolesSistema.Administrador) && !ctx.User.IsInRole(RolesSistema.Analista)) { await Rechazar(ctx, 403, "Tu rol no puede abrir el canal."); return; }
        if (!ctx.WebSockets.IsWebSocketRequest) { await Rechazar(ctx, 400, "Se esperaba una conexión WebSocket."); return; }
        if (!OrigenPermitido(ctx, origenes)) { await Rechazar(ctx, 403, "Origen no permitido."); return; }
        if (!hub.HayCupo) { await Rechazar(ctx, 503, "Demasiadas conexiones abiertas."); return; }

        using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
        await hub.AtenderAsync(socket, ctx.User.NombreUsuario(), ctx.RequestAborted);
    }

    /// <summary>Sin cabecera Origin (Postman, scripts) se acepta; con ella debe ser el mismo host o uno de Cors:OrigenesPermitidos.</summary>
    internal static bool OrigenPermitido(HttpContext ctx, Api.Configuration.OrigenesPermitidos origenes)
    {
        var origen = ctx.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origen)) return true;
        if (origenes.Contiene(origen)) return true;      // el frontend en Vue (otro puerto)
        return Uri.TryCreate(origen, UriKind.Absolute, out var u) && string.Equals(u.Authority, ctx.Request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }

    private static Task Rechazar(HttpContext ctx, int codigo, string mensaje)
    {
        ctx.Response.StatusCode = codigo;
        return ctx.Response.WriteAsJsonAsync(new { exito = false, mensaje });
    }
}
