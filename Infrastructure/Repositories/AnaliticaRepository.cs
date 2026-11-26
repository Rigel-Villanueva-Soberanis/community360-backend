using Application.DTOs.Responses;
using Application.Interfaces;
using Dapper;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>Todas las consultas son fijas (sin texto del cliente); solo la tendencia recibe dos fechas como parámetros.</summary>
public class AnaliticaRepository(DbConnectionFactory db) : IAnaliticaRepository
{
    private sealed class FilaTendencia { public DateTime Fecha { get; set; } public int Total { get; set; } public decimal Sentimiento { get; set; } }

    private async Task<IReadOnlyList<FilaAgrupada>> Agrupar(string sql, CancellationToken ct, object? p = null)
    {
        await using var cn = await db.CrearAsync(ct);
        var filas = await cn.QueryAsync<(string Etiqueta, decimal Valor, int Total)>(new CommandDefinition(sql, p, cancellationToken: ct));
        return filas.Select(f => new FilaAgrupada(f.Etiqueta, f.Valor, f.Total)).ToList();
    }

    public async Task<ResumenResponse> ResumenAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(*) AS TotalInteracciones,
                   COALESCE(ROUND(AVG(sentimiento), 3), 0) AS SentimientoPromedio,
                   COALESCE(ROUND(AVG(reacciones_recibidas), 2), 0) AS ReaccionesPromedio,
                   COALESCE(ROUND(100 * SUM(sentimiento < 0) / NULLIF(COUNT(*), 0), 1), 0) AS PorcentajeNegativo
            FROM interaccion
            """;
        await using var cn = await db.CrearAsync(ct);
        return await cn.QuerySingleAsync<ResumenResponse>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public Task<IReadOnlyList<FilaAgrupada>> InteraccionesPorPlataformaAsync(CancellationToken ct) => Agrupar("""
        SELECT p.nombre AS Etiqueta, COUNT(*) AS Valor, COUNT(*) AS Total
        FROM interaccion i JOIN plataforma p ON p.id_plataforma = i.id_plataforma
        GROUP BY p.nombre ORDER BY Total DESC, p.nombre
        """, ct);

    public Task<IReadOnlyList<FilaAgrupada>> InteraccionesPorTipoAsync(CancellationToken ct) => Agrupar("""
        SELECT t.nombre AS Etiqueta, COUNT(*) AS Valor, COUNT(*) AS Total
        FROM interaccion i JOIN tipo_interaccion t ON t.id_tipo = i.id_tipo
        GROUP BY t.nombre ORDER BY Total DESC, t.nombre
        """, ct);

    public async Task<IReadOnlyList<CeldaHeatmapResponse>> ActividadPorHoraAsync(CancellationToken ct)
    {
        const string sql = "SELECT DAYOFWEEK(fecha_hora) AS Dia, HOUR(fecha_hora) AS Hora, COUNT(*) AS Total FROM interaccion GROUP BY 1, 2 ORDER BY 1, 2";
        await using var cn = await db.CrearAsync(ct);
        return (await cn.QueryAsync<CeldaHeatmapResponse>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public Task<IReadOnlyList<FilaAgrupada>> SentimientoPorTemaAsync(CancellationToken ct) => Agrupar("""
        SELECT t.nombre AS Etiqueta, AVG(i.sentimiento) AS Valor, COUNT(*) AS Total
        FROM interaccion i JOIN tema t ON t.id_tema = i.id_tema
        GROUP BY t.nombre ORDER BY Valor, t.nombre
        """, ct);

    public Task<IReadOnlyList<FilaAgrupada>> EtiquetasTopAsync(int limite, CancellationToken ct) => Agrupar("""
        SELECT e.nombre AS Etiqueta, COUNT(*) AS Valor, COUNT(*) AS Total
        FROM interaccion_etiqueta ie JOIN etiqueta e ON e.id_etiqueta = ie.id_etiqueta
        GROUP BY e.nombre ORDER BY Total DESC, e.nombre LIMIT @Limite
        """, ct, new { Limite = limite });

    public Task<IReadOnlyList<FilaAgrupada>> UsuariosPorEstatusAsync(CancellationToken ct) => Agrupar("""
        SELECT estatus AS Etiqueta, COUNT(*) AS Valor, COUNT(*) AS Total FROM usuario GROUP BY estatus ORDER BY Total DESC, estatus
        """, ct);

    public Task<IReadOnlyList<FilaAgrupada>> UsuariosPorPlataformaAsync(CancellationToken ct) => Agrupar("""
        SELECT p.nombre AS Etiqueta, COUNT(*) AS Valor, COUNT(*) AS Total
        FROM usuario u JOIN plataforma p ON p.id_plataforma = u.id_plataforma
        GROUP BY p.nombre ORDER BY Total DESC, p.nombre
        """, ct);

    public Task<IReadOnlyList<FilaAgrupada>> UsuariosPorRangoEdadAsync(CancellationToken ct) => Agrupar("""
        SELECT rango_edad AS Etiqueta, COUNT(*) AS Valor, COUNT(*) AS Total FROM usuario GROUP BY rango_edad ORDER BY rango_edad
        """, ct);

    // Privacidad: solo usuarios con consentimiento "si" (los demás no tienen ciudad y no deben contarse por país).
    public Task<IReadOnlyList<FilaAgrupada>> UsuariosPorPaisAsync(CancellationToken ct) => Agrupar("""
        SELECT pa.nombre AS Etiqueta, COUNT(*) AS Valor, COUNT(*) AS Total
        FROM usuario u
        JOIN ciudad c ON c.id_ciudad = u.id_ciudad
        JOIN estado es ON es.id_estado = c.id_estado
        JOIN pais pa ON pa.id_pais = es.id_pais
        WHERE u.consentimiento_datos = 'si'
        GROUP BY pa.nombre ORDER BY Total DESC, pa.nombre
        """, ct);

    public async Task<IReadOnlyList<TendenciaDiariaResponse>> TendenciaDiariaAsync(DateTime desde, DateTime hastaExclusivo, CancellationToken ct)
    {
        const string sql = """
            SELECT DATE(fecha_hora) AS Fecha, COUNT(*) AS Total, AVG(sentimiento) AS Sentimiento
            FROM interaccion WHERE fecha_hora >= @Desde AND fecha_hora < @Hasta
            GROUP BY DATE(fecha_hora) ORDER BY Fecha
            """;
        await using var cn = await db.CrearAsync(ct);
        var filas = await cn.QueryAsync<FilaTendencia>(new CommandDefinition(sql, new { Desde = desde, Hasta = hastaExclusivo }, cancellationToken: ct));
        return filas.Select(f => new TendenciaDiariaResponse { Fecha = DateOnly.FromDateTime(f.Fecha), Total = f.Total, Sentimiento = f.Sentimiento }).ToList();
    }
}
