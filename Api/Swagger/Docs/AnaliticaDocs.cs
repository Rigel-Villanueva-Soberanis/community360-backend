namespace Api.Swagger.Docs;

/// <summary>Documentación del módulo Analítica e indicadores (AnaliticaController).</summary>
public static class AnaliticaDocs
{
    public const string Controller = "Analitica";

    private static EndpointDoc Simple(string resumen, string descripcion) => new()
    {
        Resumen = resumen,
        Descripcion = descripcion,
        Respuestas = new Dictionary<int, string> { [200] = "Datos listos para graficar." }
    };

    public static readonly IReadOnlyDictionary<string, EndpointDoc> Entradas = new Dictionary<string, EndpointDoc>
    {
        ["Resumen"] = Simple("Indicadores globales (I1 a I4)", "Totales y promedios de toda la plataforma."),
        ["PorPlataforma"] = Simple("Interacciones por plataforma (I5)", "Distribución para la gráfica de dona."),
        ["PorTipo"] = Simple("Interacciones por tipo (I6)", "Conteo por tipo de interacción para barras."),
        ["ActividadPorHora"] = Simple("Actividad por día y hora (I7)", "Matriz día de la semana × hora para el mapa de calor."),
        ["SentimientoPorTema"] = Simple("Sentimiento promedio por tema (I8)", "Promedio de sentimiento de cada tema."),
        ["EtiquetasTop"] = Simple("Etiquetas más usadas (I9)", "Ranking de etiquetas por número de interacciones."),
        ["UsuariosPorEstatus"] = Simple("Usuarios por estatus (H1)", "Histórico: activo, en riesgo e inactivo."),
        ["UsuariosPorPlataforma"] = Simple("Usuarios por plataforma (H2)", "Histórico de usuarios según su plataforma principal."),
        ["UsuariosPorRangoEdad"] = Simple("Usuarios por rango de edad (H3)", "Histórico por rangos de edad."),
        ["UsuariosPorPais"] = Simple("Usuarios por país (H4)", "Histórico por país. Solo cuenta usuarios con consentimiento \"si\"."),
        ["TendenciaDiaria"] = new()
        {
            Resumen = "Tendencia diaria (H5)",
            Descripcion = "Interacciones y sentimiento promedio por día dentro del rango pedido.",
            Parametros = new Dictionary<string, string>
            {
                ["desde"] = "Fecha inicial (AAAA-MM-DD).",
                ["hasta"] = "Fecha final (AAAA-MM-DD)."
            },
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Serie diaria.",
                [422] = "Fechas inválidas o rango invertido."
            }
        }
    };
}
