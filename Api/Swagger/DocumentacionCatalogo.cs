using Api.Swagger.Docs;

namespace Api.Swagger;

/// <summary>Reúne la documentación de todos los módulos. Clave: "Controller.Accion".</summary>
public static class DocumentacionCatalogo
{
    private static readonly Dictionary<string, EndpointDoc> Todo = Unir(
        (AuthDocs.Controller, AuthDocs.Entradas),
        (CatalogosDocs.Controller, CatalogosDocs.Entradas),
        (UsuariosDocs.Controller, UsuariosDocs.Entradas),
        (InteraccionesDocs.Controller, InteraccionesDocs.Entradas),
        (AnaliticaDocs.Controller, AnaliticaDocs.Entradas),
        (AuditoriaDocs.Controller, AuditoriaDocs.Entradas),
        (TiempoRealDocs.Controller, TiempoRealDocs.Entradas));

    public static EndpointDoc? Buscar(string controller, string accion)
        => Todo.GetValueOrDefault($"{controller}.{accion}");

    public static IEnumerable<string> Claves => Todo.Keys;

    private static Dictionary<string, EndpointDoc> Unir(
        params (string Controller, IReadOnlyDictionary<string, EndpointDoc> Entradas)[] modulos)
    {
        var r = new Dictionary<string, EndpointDoc>();
        foreach (var (controller, entradas) in modulos)
            foreach (var (accion, doc) in entradas)
                r.Add($"{controller}.{accion}", doc);
        return r;
    }
}
