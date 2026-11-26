namespace Infrastructure.Repositories;

/// <summary>Fragmentos de SQL fijos (escritos por nosotros) que comparten los repositorios que leen interacciones.</summary>
internal static class InteraccionSql
{
    public const string Select = """
        SELECT i.id_interaccion AS IdInteraccion, i.id_usuario AS IdUsuario, i.fecha_hora AS FechaHora,
               pl.nombre AS Plataforma, t.nombre AS Tipo, i.texto AS Texto, tm.nombre AS Tema,
               i.sentimiento AS Sentimiento, i.idioma AS Idioma, c.nombre AS Ciudad, d.nombre AS Dispositivo,
               i.longitud_texto AS LongitudTexto, i.reacciones_recibidas AS ReaccionesRecibidas,
               (SELECT GROUP_CONCAT(e.nombre ORDER BY e.nombre SEPARATOR ',')
                  FROM interaccion_etiqueta ie JOIN etiqueta e ON e.id_etiqueta = ie.id_etiqueta
                 WHERE ie.id_interaccion = i.id_interaccion) AS EtiquetasCsv
        FROM interaccion i
        JOIN plataforma pl ON pl.id_plataforma = i.id_plataforma
        JOIN tipo_interaccion t ON t.id_tipo = i.id_tipo
        JOIN tema tm ON tm.id_tema = i.id_tema
        JOIN dispositivo d ON d.id_dispositivo = i.id_dispositivo
        LEFT JOIN ciudad c ON c.id_ciudad = i.id_ciudad
        """;
}
