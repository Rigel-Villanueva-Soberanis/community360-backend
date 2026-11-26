using Application.Common.Utilidades;
using Application.Interfaces;
using Dapper;
using Domain;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// SQL dinámico seguro: el WHERE se arma con fragmentos fijos y DynamicParameters; el texto del cliente solo entra como parámetro.
/// La columna de ORDER BY sale de una lista blanca, nunca del texto del cliente.
/// </summary>
public class UsuarioRepository(DbConnectionFactory db) : IUsuarioRepository
{
    private static readonly Dictionary<string, string> Columnas = new()
    {
        ["alias"] = "u.alias",
        ["ingreso"] = "u.fecha_ingreso",
        ["actividad"] = "u.ultima_actividad",
        ["sentimiento"] = "u.sentimiento_promedio",
    };

    private const string SelectUsuario = """
        SELECT u.id_usuario AS IdUsuario, u.alias AS Alias, u.fecha_ingreso AS FechaIngreso, u.zona_horaria AS ZonaHoraria,
               u.rango_edad AS RangoEdad, e.nombre AS Especie, pl.nombre AS Plataforma, u.es_artista AS EsArtista,
               u.mensajes_30d AS Mensajes30d, u.reacciones_30d AS Reacciones30d, u.eventos_asistidos AS EventosAsistidos,
               u.comisiones_compradas AS ComisionesCompradas, u.sentimiento_promedio AS SentimientoPromedio,
               u.ultima_actividad AS UltimaActividad, u.estatus AS Estatus, u.consentimiento_datos AS ConsentimientoDatos,
               c.nombre AS Ciudad, u.latitud AS Latitud, u.longitud AS Longitud
        FROM usuario u
        JOIN especie e ON e.id_especie = u.id_especie
        JOIN plataforma pl ON pl.id_plataforma = u.id_plataforma
        LEFT JOIN ciudad c ON c.id_ciudad = u.id_ciudad
        """;

    public async Task<(IReadOnlyList<Usuario> Items, int Total)> ListarAsync(FiltroUsuarios f, Paginacion pag, CancellationToken ct)
    {
        var where = new List<string>();
        var p = new DynamicParameters();
        if (f.IdPlataforma is not null) { where.Add("u.id_plataforma = @IdPlataforma"); p.Add("IdPlataforma", f.IdPlataforma); }
        if (f.IdEspecie is not null) { where.Add("u.id_especie = @IdEspecie"); p.Add("IdEspecie", f.IdEspecie); }
        if (f.Estatus is not null) { where.Add("u.estatus = @Estatus"); p.Add("Estatus", f.Estatus); }
        if (f.RangoEdad is not null) { where.Add("u.rango_edad = @RangoEdad"); p.Add("RangoEdad", f.RangoEdad); }
        if (f.EsArtista is not null) { where.Add("u.es_artista = @EsArtista"); p.Add("EsArtista", f.EsArtista); }
        if (f.PatronBuscar is not null) { where.Add("u.alias LIKE @Buscar ESCAPE '\\\\'"); p.Add("Buscar", f.PatronBuscar); }
        var clausula = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        var col = Columnas.GetValueOrDefault(f.Orden, "u.alias");   // lista blanca
        var dir = f.Descendente ? "DESC" : "ASC";                   // decidido por un bool
        p.Add("Limite", pag.Tamano);
        p.Add("Offset", pag.Offset);

        var sql = $"{SelectUsuario} {clausula} ORDER BY {col} {dir}, u.id_usuario ASC LIMIT @Limite OFFSET @Offset";
        var sqlTotal = $"SELECT COUNT(*) FROM usuario u {clausula}";

        await using var cn = await db.CrearAsync(ct);
        var items = (await cn.QueryAsync<Usuario>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();
        var total = await cn.ExecuteScalarAsync<int>(new CommandDefinition(sqlTotal, p, cancellationToken: ct));
        return (items, total);
    }

    public async Task<Usuario?> ObtenerAsync(string idUsuario, CancellationToken ct)
    {
        await using var cn = await db.CrearAsync(ct);
        return await cn.QuerySingleOrDefaultAsync<Usuario>(
            new CommandDefinition($"{SelectUsuario} WHERE u.id_usuario = @Id", new { Id = idUsuario }, cancellationToken: ct));
    }

    public async Task<UsuarioBasico?> ObtenerBasicoAsync(string idUsuario, CancellationToken ct)
    {
        const string sql = "SELECT id_usuario AS IdUsuario, consentimiento_datos AS ConsentimientoDatos, id_ciudad AS IdCiudad FROM usuario WHERE id_usuario = @Id";
        await using var cn = await db.CrearAsync(ct);
        return await cn.QuerySingleOrDefaultAsync<UsuarioBasico>(new CommandDefinition(sql, new { Id = idUsuario }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<InteresUsuario>> InteresesAsync(string idUsuario, CancellationToken ct)
    {
        const string sql = """
            SELECT i.nombre AS Nombre, ui.es_principal AS EsPrincipal
            FROM usuario_interes ui JOIN interes i ON i.id_interes = ui.id_interes
            WHERE ui.id_usuario = @Id
            ORDER BY ui.es_principal DESC, i.nombre
            """;
        await using var cn = await db.CrearAsync(ct);
        return (await cn.QueryAsync<InteresUsuario>(new CommandDefinition(sql, new { Id = idUsuario }, cancellationToken: ct))).ToList();
    }

    public async Task<(IReadOnlyList<InteraccionLectura> Items, int Total)> ListarInteraccionesAsync(string idUsuario, Paginacion pag, CancellationToken ct)
    {
        var p = new { Id = idUsuario, Limite = pag.Tamano, pag.Offset };
        var sql = $"{InteraccionSql.Select} WHERE i.id_usuario = @Id ORDER BY i.fecha_hora DESC, i.id_interaccion DESC LIMIT @Limite OFFSET @Offset";
        await using var cn = await db.CrearAsync(ct);
        var items = (await cn.QueryAsync<InteraccionLectura>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();
        var total = await cn.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM interaccion WHERE id_usuario = @Id", new { Id = idUsuario }, cancellationToken: ct));
        return (items, total);
    }
}
