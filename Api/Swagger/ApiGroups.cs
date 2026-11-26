namespace Api.Swagger;

/// <summary>
/// Módulos de la documentación. Cada uno es un documento Swagger independiente
/// (selector "Select a definition" en /swagger).
/// </summary>
public static class ApiGroups
{
    public const string Auth = "auth";
    public const string Catalogos = "catalogos";
    public const string Usuarios = "usuarios";
    public const string Interacciones = "interacciones";
    public const string Analitica = "analitica";
    public const string Auditoria = "auditoria";
    public const string TiempoReal = "tiempo-real";

    public static readonly (string Clave, string Titulo, string Descripcion)[] Todos =
    [
        (Auth, "Autenticación", "Inicio y cierre de sesión, token JWT y usuario actual."),
        (Catalogos, "Catálogos", "Listas de valores permitidos (plataformas, tipos, temas, etiquetas...)."),
        (Usuarios, "Usuarios de la comunidad", "Consulta del padrón. Nunca devuelve ubicación de quien no dio consentimiento."),
        (Interacciones, "Interacciones", "Alta, consulta, modificación y baja de interacciones."),
        (Analitica, "Analítica e indicadores", "Indicadores con definición matemática y consultas históricas."),
        (Auditoria, "Auditoría", "Cuarentena, diario de cambios y bitácoras. Solo administrador."),
        (TiempoReal, "Tiempo real (WebSocket)", "Estado del canal /ws y formato de los mensajes."),
    ];

    /// <summary>Nombre del controller (sin el sufijo "Controller") → módulo.</summary>
    public static readonly IReadOnlyDictionary<string, string> PorControlador = new Dictionary<string, string>
    {
        ["Auth"] = Auth,
        ["Catalogos"] = Catalogos,
        ["Usuarios"] = Usuarios,
        ["Interacciones"] = Interacciones,
        ["Analitica"] = Analitica,
        ["Auditoria"] = Auditoria,
        ["TiempoReal"] = TiempoReal,
    };
}
