namespace Api.Swagger.Docs;

/// <summary>Documentación del módulo Catálogos (CatalogosController).</summary>
public static class CatalogosDocs
{
    public const string Controller = "Catalogos";

    public static readonly IReadOnlyDictionary<string, EndpointDoc> Entradas = new Dictionary<string, EndpointDoc>
    {
        ["ObtenerTodos"] = new()
        {
            Resumen = "Todos los catálogos",
            Descripcion = "Devuelve juntas las listas de valores permitidos (plataformas, tipos de interacción, temas, etiquetas, dispositivos, etc.). " +
                          "Sirve para llenar los selectores del dashboard y para saber qué valores acepta el alta de interacciones.",
            Respuestas = new Dictionary<int, string> { [200] = "Todos los catálogos." }
        },
        ["ObtenerPorNombre"] = new()
        {
            Resumen = "Un catálogo por nombre",
            Parametros = new Dictionary<string, string>
            {
                ["nombre"] = "`plataformas`, `especies`, `intereses`, `temas`, `tipos-interaccion`, `dispositivos` o `etiquetas`."
            },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Los valores del catálogo (id y nombre; los tipos de interacción incluyen `admiteTexto`).",
                [404] = "El catálogo no existe; el mensaje lista los disponibles."
            }
        }
    };
}
