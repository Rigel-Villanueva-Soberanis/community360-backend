using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;

namespace Api.Swagger;

public static class SwaggerConfig
{
    public static IServiceCollection AddSwaggerPorModulos(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            foreach (var (clave, titulo, descripcion) in ApiGroups.Todos)
                o.SwaggerDoc(clave, new OpenApiInfo
                {
                    Title = $"COMMUNITY 360 · {titulo}",
                    Version = "v1",
                    Description = descripcion
                });

            // Cada documento incluye solo los endpoints de su módulo.
            o.DocInclusionPredicate((doc, api) => api.GroupName == doc);

            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Pega el token que devuelve POST /api/auth/login (sin la palabra Bearer). " +
                              "En el navegador la sesión también viaja en la cookie c360_token."
            });

            o.OperationFilter<DocumentacionOperationFilter>();   // primero: lo escrito en Docs
            o.OperationFilter<AuthOperationFilter>();            // después: candado y 401/403 faltantes
        });

        services.AddControllers(c =>
        {
            c.Conventions.Add(new ModuloConvention());                          // módulo por nombre de controller
        });
        return services;
    }

    public static WebApplication UseSwaggerPorModulos(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            foreach (var (clave, titulo, _) in ApiGroups.Todos)
                o.SwaggerEndpoint($"/swagger/{clave}/swagger.json", titulo);
            o.DocumentTitle = "COMMUNITY 360 · API";
            o.DefaultModelsExpandDepth(-1);
            o.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            o.EnablePersistAuthorization();
        });

        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
        VerificarCobertura(app);
        return app;
    }

    /// <summary>
    /// Al arrancar avisa (solo log) de: controllers sin módulo, endpoints sin documentación en Docs
    /// y entradas de Docs cuyo endpoint ya no existe.
    /// </summary>
    private static void VerificarCobertura(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Swagger");
            var acciones = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
                .ApiDescriptionGroups.Items.SelectMany(g => g.Items)
                .Select(a => a.ActionDescriptor).OfType<ControllerActionDescriptor>().DistinctBy(a => a.Id).ToList();

            foreach (var c in acciones.Select(a => a.ControllerName).Distinct()
                         .Where(c => !ApiGroups.PorControlador.ContainsKey(c)))
                log.LogWarning("El controller {Controller} no tiene módulo en ApiGroups.PorControlador.", c);

            var existentes = acciones.Select(a => $"{a.ControllerName}.{a.ActionName}").ToHashSet();
            foreach (var clave in existentes.Where(k => !DocumentacionCatalogo.Claves.Contains(k)))
                log.LogWarning("Endpoint sin documentación en Api/Swagger/Docs: {Clave}", clave);

            var controllers = acciones.Select(a => a.ControllerName).ToHashSet();
            foreach (var clave in DocumentacionCatalogo.Claves
                         .Where(k => controllers.Contains(k.Split('.')[0]) && !existentes.Contains(k)))
                log.LogWarning("Docs documenta un endpoint que no existe: {Clave}", clave);
        });
    }
}
