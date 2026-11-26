namespace Api.Swagger.Docs;

/// <summary>Documentación del módulo Auditoría (AuditoriaController). Solo administrador.</summary>
public static class AuditoriaDocs
{
    public const string Controller = "Auditoria";

    private static EndpointDoc Lista(string resumen, string descripcion) => new()
    {
        Resumen = resumen,
        Descripcion = descripcion,
        Parametros = new Dictionary<string, string>
        {
            ["pagina"] = "Número de página, desde 1.",
            ["tamano"] = "Registros por página."
        },
        Respuestas = new Dictionary<int, string>
        {
            [200] = "Página de documentos.",
            [403] = "El rol no es administrador.",
            [422] = "Algún parámetro tiene un valor no permitido."
        }
    };

    public static readonly IReadOnlyDictionary<string, EndpointDoc> Entradas = new Dictionary<string, EndpointDoc>
    {
        ["Cuarentena"] = Lista("Registros en cuarentena", "Interacciones huérfanas apartadas durante la carga (colección `cuarentena`). Filtro `estadoRevision`."),
        ["CuarentenaPorId"] = new()
        {
            Resumen = "Un registro de cuarentena",
            Parametros = new Dictionary<string, string> { ["idInteraccion"] = "Identificador con formato `I0000000`." },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Documento de cuarentena.",
                [403] = "El rol no es administrador.",
                [404] = "No existe.",
                [422] = "El id no tiene el formato `I0000000`."
            }
        },
        ["DiarioCambios"] = Lista("Diario de cambios de la limpieza", "Correcciones aplicadas por el ETL (colección `diario_cambios`)."),
        ["BitacoraCarga"] = new()
        {
            Resumen = "Bitácora de la carga",
            Descripcion = "Resumen de la ejecución del ETL (colección `bitacora_carga`).",
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Documentos de la bitácora.",
                [403] = "El rol no es administrador."
            }
        },
        ["BitacoraApi"] = Lista("Bitácora de operaciones del API", "Altas, modificaciones, bajas y rechazos hechos desde el API (colección `bitacora_api`). Filtro `operacion`."),
        ["Resumen"] = new()
        {
            Resumen = "Conteo de las colecciones de auditoría",
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Número de documentos de cada colección.",
                [403] = "El rol no es administrador."
            }
        }
    };
}
