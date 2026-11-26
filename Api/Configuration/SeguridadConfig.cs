using System.Text.Json;
using Api.Common;
using Application.Common.ErrorCatalog;
using Microsoft.AspNetCore.Mvc;

namespace Api.Configuration;

/// <summary>Protecciones transversales a todos los endpoints (paso 3).</summary>
public static class SeguridadConfig
{
    public const int MaxCuerpoBytes = 64 * 1024;

    public static WebApplicationBuilder AddSeguridadBasica(this WebApplicationBuilder builder)
    {
        // Cuerpos pequeños: nadie necesita mandar más de 64 KB a esta API.
        builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = MaxCuerpoBytes);

        builder.Services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new TextoLiteralJsonConverter()));

        builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ctx =>
        {
            var errores = ctx.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .SelectMany(e => e.Value!.Errors.Select(x => (Campo: e.Key, Error: x)))
                .ToList();

            // JSON mal formado o con tipo incorrecto en un campo numérico/fecha → 400. Reglas de datos → 422.
            var jsonInvalido = errores.Any(x => x.Error.Exception is JsonException
                                             || x.Error.Exception?.InnerException is JsonException
                                             || x.Campo.StartsWith('$'));   // ruta JSON: "$" o "$.campo"
            if (jsonInvalido)
                return new BadRequestObjectResult(ApiResponse<object>.Falla("La petición no tiene un formato válido."));

            var detalle = errores.Select(x => new ErrorDetalle(x.Campo, x.Error.ErrorMessage)).ToList();
            return new UnprocessableEntityObjectResult(ApiResponse<object>.Falla("Hay datos que no cumplen las reglas.", detalle));
        });

        return builder;
    }

    public static WebApplication UseSeguridadBasica(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) app.UseHsts();

        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.Headers.CacheControl = "no-store";
            await next();
        });
        return app;
    }
}
