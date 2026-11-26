using System.Text.Json;
using Api.Common;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Swagger;

/// <summary>
/// Aplica a cada operación lo que está escrito en Api/Swagger/Docs: resumen, descripción,
/// códigos de respuesta, descripción de parámetros y ejemplos.
/// </summary>
public sealed class DocumentacionOperationFilter : IOperationFilter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        SoloJson(operation);
        NombresEnCamelCase(operation);

        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor accion) return;

        var doc = DocumentacionCatalogo.Buscar(accion.ControllerName, accion.ActionName);
        if (doc is null) return;

        operation.Summary = doc.Resumen;
        operation.Description = doc.Descripcion;

        foreach (var p in operation.Parameters)
            if (doc.Parametros.TryGetValue(p.Name, out var texto)) p.Description = texto;

        if (doc.EjemploPeticion is not null && operation.RequestBody is not null)
            foreach (var media in operation.RequestBody.Content.Values)
                media.Example = Ejemplo(doc.EjemploPeticion);

        var esquemaError = context.SchemaGenerator.GenerateSchema(typeof(ApiResponse<object>), context.SchemaRepository);
        foreach (var (codigo, texto) in doc.Respuestas.OrderBy(r => r.Key))
        {
            var clave = codigo.ToString();
            if (!operation.Responses.TryGetValue(clave, out var resp))
            {
                resp = new OpenApiResponse();
                if (codigo >= 400)
                    resp.Content["application/json"] = new OpenApiMediaType { Schema = esquemaError };
                operation.Responses[clave] = resp;
            }
            resp.Description = texto;

            if (codigo is >= 200 and < 300 && doc.EjemploExito is not null)
                foreach (var media in resp.Content.Values)
                    media.Example = Ejemplo(doc.EjemploExito);
        }
    }

    /// <summary>Los parámetros de consulta se muestran como se escriben en la URL (camelCase); el binding no distingue mayúsculas.</summary>
    private static void NombresEnCamelCase(OpenApiOperation operation)
    {
        foreach (var p in operation.Parameters.Where(p => p.In == ParameterLocation.Query && p.Name.Length > 0))
            p.Name = char.ToLowerInvariant(p.Name[0]) + p.Name[1..];
    }

    /// <summary>El API solo habla JSON: quita text/plain y text/json que Swashbuckle agrega por defecto.</summary>
    private static void SoloJson(OpenApiOperation operation)
    {
        foreach (var resp in operation.Responses.Values)
        {
            if (!resp.Content.TryGetValue("application/json", out var json))
                json = resp.Content.Values.FirstOrDefault();
            resp.Content.Clear();
            if (json is not null) resp.Content["application/json"] = json;
        }
    }

    private static Microsoft.OpenApi.Any.IOpenApiAny Ejemplo(object valor)
        => OpenApiAnyFactory.CreateFromJson(JsonSerializer.Serialize(valor, Json));
}
