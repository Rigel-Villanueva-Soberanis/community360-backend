namespace Api.Swagger.Docs;

/// <summary>Documentación del módulo Usuarios de la comunidad (UsuariosController).</summary>
public static class UsuariosDocs
{
    public const string Controller = "Usuarios";

    public static readonly IReadOnlyDictionary<string, EndpointDoc> Entradas = new Dictionary<string, EndpointDoc>
    {
        ["Listar"] = new()
        {
            Resumen = "Listar usuarios (paginado)",
            Descripcion = "Padrón de la comunidad con filtros, orden y paginación. Para quien no dio consentimiento, ciudad, latitud, longitud y zona horaria no aparecen.",
            Parametros = new Dictionary<string, string>
            {
                ["estatus"] = "`activo`, `en_riesgo` o `inactivo`.",
                ["rangoEdad"] = "`18-24`, `25-31`, `32-38` o `39+`.",
                ["plataforma"] = "Nombre del catálogo de plataformas (por ejemplo `Discord`); no importan las mayúsculas.",
                ["especie"] = "Nombre del catálogo de especies (por ejemplo `Lobo`).",
                ["esArtista"] = "`true` o `false`.",
                ["buscar"] = "Texto a buscar dentro del alias. Se busca como texto literal (`%` y `_` no son comodines). Máximo 30 caracteres.",
                ["pagina"] = "Número de página, desde 1 (por defecto 1).",
                ["tamano"] = "Registros por página, de 1 a 100 (por defecto 25).",
                ["orden"] = "`alias` (por defecto), `ingreso`, `actividad` o `sentimiento`.",
                ["descendente"] = "`true` para orden descendente."
            },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Página de usuarios con el total de registros y de páginas.",
                [422] = "Algún filtro o la paginación tienen un valor no permitido."
            }
        },
        ["ObtenerPorId"] = new()
        {
            Resumen = "Detalle de un usuario",
            Parametros = new Dictionary<string, string> { ["id"] = "Identificador con formato `U000000`." },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Datos del usuario y sus intereses (el principal primero).",
                [404] = "No existe un usuario con ese id.",
                [422] = "El id no tiene el formato `U000000`."
            }
        },
        ["ListarInteracciones"] = new()
        {
            Resumen = "Historial de interacciones de un usuario",
            Descripcion = "Las más recientes primero, con paginación.",
            Parametros = new Dictionary<string, string>
            {
                ["id"] = "Identificador con formato `U000000`.",
                ["pagina"] = "Número de página, desde 1 (por defecto 1).",
                ["tamano"] = "Registros por página, de 1 a 100 (por defecto 25)."
            },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Interacciones del usuario.",
                [404] = "No existe un usuario con ese id.",
                [422] = "El id no tiene el formato `U000000`."
            }
        }
    };
}
