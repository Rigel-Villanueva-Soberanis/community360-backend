using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Api.Swagger;

/// <summary>
/// Asigna cada controller a su módulo de Swagger por convención (según su nombre),
/// de modo que los controllers no necesitan ningún atributo de documentación.
/// </summary>
public sealed class ModuloConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (ApiGroups.PorControlador.TryGetValue(controller.ControllerName, out var grupo))
            controller.ApiExplorer.GroupName = grupo;
    }
}
