using System.Text.Json;
using Api.Common;
using Application.Common.ErrorCatalog;

namespace Api.Middlewares;

/// <summary>
/// Captura todas las excepciones. Los errores controlados (DomainException) salen con su código HTTP;
/// cualquier otro se registra y devuelve 500 sin detalles internos (no se filtran tablas ni trazas).
/// </summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (DomainException ex)
        {
            await Responder(ctx, ex.StatusCode, ApiResponse<object>.Falla(ex.Message, ex.Errores));
        }
        catch (BadHttpRequestException ex)   // JSON mal formado, cuerpo demasiado grande
        {
            log.LogWarning(ex, "Petición inválida");
            if (ex.StatusCode == 413)
                await Responder(ctx, 413, ApiResponse<object>.Falla("El cuerpo de la petición es demasiado grande."));
            else
                await Responder(ctx, 400, ApiResponse<object>.Falla("La petición no tiene un formato válido."));
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error no controlado en {Ruta}", ctx.Request.Path);
            await Responder(ctx, 500, ApiResponse<object>.Falla("Ocurrió un error interno."));
        }
    }

    private static Task Responder(HttpContext ctx, int status, object cuerpo)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, Json));
    }
}
