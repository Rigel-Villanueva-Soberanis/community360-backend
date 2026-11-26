namespace Api.Swagger.Docs;

/// <summary>Documentación del módulo Interacciones (InteraccionesController).</summary>
public static class InteraccionesDocs
{
    public const string Controller = "Interacciones";

    public static readonly IReadOnlyDictionary<string, EndpointDoc> Entradas = new Dictionary<string, EndpointDoc>
    {
        ["Listar"] = new()
        {
            Resumen = "Listar interacciones (paginado)",
            Descripcion = "Admite filtros por usuario, plataforma, tipo, tema, etiqueta y rango de fechas (UTC), con orden y paginación.",
            Parametros = new Dictionary<string, string>
            {
                ["idUsuario"] = "Formato `U000000`.",
                ["plataforma"] = "Debe existir en el catálogo de plataformas.",
                ["tipo"] = "Debe existir en el catálogo de tipos de interacción.",
                ["tema"] = "Debe existir en el catálogo de temas.",
                ["etiqueta"] = "Debe existir en el catálogo de etiquetas.",
                ["desde"] = "Fecha y hora inicial (UTC).",
                ["hasta"] = "Fecha y hora final (UTC).",
                ["orden"] = "`fecha`, `sentimiento` o `reacciones`.",
                ["descendente"] = "`true` para orden descendente."
            },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Página de interacciones.",
                [422] = "Algún filtro tiene un valor no permitido."
            }
        },
        ["ObtenerPorId"] = new()
        {
            Resumen = "Detalle de una interacción",
            Parametros = new Dictionary<string, string> { ["id"] = "Identificador con formato `I0000000`." },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Datos de la interacción con sus etiquetas.",
                [404] = "No existe.",
                [422] = "El id no tiene el formato `I0000000`."
            }
        },
        ["Crear"] = new()
        {
            Resumen = "Registrar una interacción",
            Descripcion = "Solo administrador. Valida las reglas de negocio (tipo de interacción, texto, sentimiento entre -1 y 1, catálogos). " +
                          "Si el usuario no dio consentimiento, la interacción se guarda sin ubicación. " +
                          "Deja registro en la bitácora del API y avisa por WebSocket a los dashboards conectados.",
            Respuestas = new Dictionary<int, string>
            {
                [201] = "Interacción creada; el encabezado `Location` apunta a su detalle.",
                [400] = "JSON mal formado o con tipos que no corresponden.",
                [403] = "El rol no es administrador.",
                [404] = "El usuario indicado no existe.",
                [409] = "El identificador ya existe.",
                [422] = "Alguna regla de negocio o de formato no se cumple."
            },
            EjemploPeticion = new
            {
                idUsuario = "U000001",
                plataforma = "Instagram",
                tipo = "comentario",
                texto = "Me encantó la nueva función.",
                tema = "arte_comisiones",
                sentimiento = 0.8,
                idioma = "es",
                dispositivo = "Android",
                etiquetas = new[] { "arte", "aviso" }
            }
        },
        ["Actualizar"] = new()
        {
            Resumen = "Modificar una interacción",
            Descripcion = "Solo administrador. Cambia los campos editables (texto, tema, sentimiento, reacciones, etiquetas). " +
                          "Guarda en la bitácora el estado anterior y el nuevo.",
            Parametros = new Dictionary<string, string> { ["id"] = "Identificador con formato `I0000000`." },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Interacción actualizada.",
                [403] = "El rol no es administrador.",
                [404] = "No existe.",
                [422] = "Alguna regla de negocio o de formato no se cumple."
            }
        },
        ["Eliminar"] = new()
        {
            Resumen = "Eliminar una interacción",
            Descripcion = "Solo administrador. Guarda en la bitácora el registro eliminado y avisa por WebSocket.",
            Parametros = new Dictionary<string, string> { ["id"] = "Identificador con formato `I0000000`." },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Interacción eliminada.",
                [403] = "El rol no es administrador.",
                [404] = "No existe."
            }
        }
    };
}
