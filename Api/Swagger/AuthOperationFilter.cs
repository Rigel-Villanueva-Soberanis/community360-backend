using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Swagger;

/// <summary>Muestra el candado solo en endpoints que exigen sesión y agrega 401/403 si no están documentados.</summary>
public sealed class AuthOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var meta = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var requiere = meta.OfType<AuthorizeAttribute>().Any() && !meta.OfType<AllowAnonymousAttribute>().Any();
        if (!requiere) return;

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Sin token o token inválido." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "El rol no tiene permiso." });
        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            }
        ];
    }
}
