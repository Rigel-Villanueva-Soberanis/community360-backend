using System.Data.Common;
using Application.Common.ErrorsInteracciones;
using Application.Common.Utilidades;
using Application.Interfaces;
using Dapper;
using Domain;
using Infrastructure.Data;
using MySqlConnector;

namespace Infrastructure.Repositories;

public class InteraccionRepository(DbConnectionFactory db) : IInteraccionRepository
{
    private static readonly Dictionary<string, string> Columnas = new()
    {
        ["fecha"] = "i.fecha_hora",
        ["sentimiento"] = "i.sentimiento",
        ["reacciones"] = "i.reacciones_recibidas",
    };

    public async Task<(IReadOnlyList<InteraccionLectura> Items, int Total)> ListarAsync(FiltroInteracciones f, Paginacion pag, CancellationToken ct)
    {
        var where = new List<string>();
        var p = new DynamicParameters();
        if (f.IdUsuario is not null) { where.Add("i.id_usuario = @IdUsuario"); p.Add("IdUsuario", f.IdUsuario); }
        if (f.IdPlataforma is not null) { where.Add("i.id_plataforma = @IdPlataforma"); p.Add("IdPlataforma", f.IdPlataforma); }
        if (f.IdTipo is not null) { where.Add("i.id_tipo = @IdTipo"); p.Add("IdTipo", f.IdTipo); }
        if (f.IdTema is not null) { where.Add("i.id_tema = @IdTema"); p.Add("IdTema", f.IdTema); }
        if (f.IdEtiqueta is not null)
        {
            where.Add("EXISTS (SELECT 1 FROM interaccion_etiqueta fe WHERE fe.id_interaccion = i.id_interaccion AND fe.id_etiqueta = @IdEtiqueta)");
            p.Add("IdEtiqueta", f.IdEtiqueta);
        }
        if (f.Desde is not null) { where.Add("i.fecha_hora >= @Desde"); p.Add("Desde", DateTime.SpecifyKind(f.Desde.Value, DateTimeKind.Unspecified)); }
        if (f.Hasta is not null) { where.Add("i.fecha_hora <= @Hasta"); p.Add("Hasta", DateTime.SpecifyKind(f.Hasta.Value, DateTimeKind.Unspecified)); }
        var clausula = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        var col = Columnas.GetValueOrDefault(f.Orden, "i.fecha_hora");   // lista blanca
        var dir = f.Descendente ? "DESC" : "ASC";                        // decidido por un bool
        p.Add("Limite", pag.Tamano);
        p.Add("Offset", pag.Offset);

        var sql = $"{InteraccionSql.Select} {clausula} ORDER BY {col} {dir}, i.id_interaccion {dir} LIMIT @Limite OFFSET @Offset";
        var sqlTotal = $"SELECT COUNT(*) FROM interaccion i {clausula}";

        await using var cn = await db.CrearAsync(ct);
        var items = (await cn.QueryAsync<InteraccionLectura>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();
        var total = await cn.ExecuteScalarAsync<int>(new CommandDefinition(sqlTotal, p, cancellationToken: ct));
        return (items, total);
    }

    public async Task<InteraccionLectura?> ObtenerAsync(string idInteraccion, CancellationToken ct)
    {
        await using var cn = await db.CrearAsync(ct);
        return await cn.QuerySingleOrDefaultAsync<InteraccionLectura>(
            new CommandDefinition($"{InteraccionSql.Select} WHERE i.id_interaccion = @Id", new { Id = idInteraccion }, cancellationToken: ct));
    }

    public async Task<string> InsertarAsync(Interaccion i, string? idSolicitado, IReadOnlyCollection<int> idsEtiquetas, CancellationToken ct)
    {
        // Si el id lo genera la API y dos altas coinciden, se reintenta con el siguiente número.
        var intentos = idSolicitado is null ? 3 : 1;
        for (var intento = 1; ; intento++)
        {
            await using var cn = await db.CrearAsync(ct);
            await using var tx = await cn.BeginTransactionAsync(ct);
            try
            {
                var id = idSolicitado ?? await SiguienteIdAsync(cn, tx, ct);
                const string sql = """
                    INSERT INTO interaccion (id_interaccion, id_usuario, fecha_hora, id_plataforma, id_tipo, texto, id_tema,
                                             sentimiento, idioma, id_ciudad, id_dispositivo, longitud_texto, reacciones_recibidas)
                    VALUES (@Id, @IdUsuario, @FechaHora, @IdPlataforma, @IdTipo, @Texto, @IdTema,
                            @Sentimiento, @Idioma, @IdCiudad, @IdDispositivo, @LongitudTexto, @ReaccionesRecibidas)
                    """;
                await cn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    Id = id, i.IdUsuario, i.FechaHora, i.IdPlataforma, i.IdTipo, i.Texto, i.IdTema, i.Sentimiento, i.Idioma,
                    i.IdCiudad, i.IdDispositivo, i.LongitudTexto, i.ReaccionesRecibidas
                }, tx, cancellationToken: ct));
                if (idsEtiquetas.Count > 0)
                    await cn.ExecuteAsync(new CommandDefinition(
                        "INSERT INTO interaccion_etiqueta (id_interaccion, id_etiqueta) VALUES (@Id, @IdEtiqueta)",
                        idsEtiquetas.Select(e => new { Id = id, IdEtiqueta = e }), tx, cancellationToken: ct));
                await tx.CommitAsync(ct);
                return id;
            }
            catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            {
                await tx.RollbackAsync(CancellationToken.None);
                if (intento >= intentos) throw InteraccionErrors.IdRepetido();
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
    }

    private static async Task<string> SiguienteIdAsync(DbConnection cn, DbTransaction tx, CancellationToken ct)
    {
        const string sql = "SELECT COALESCE(MAX(CAST(SUBSTRING(id_interaccion, 2) AS UNSIGNED)), 0) + 1 FROM interaccion";
        var n = await cn.ExecuteScalarAsync<long>(new CommandDefinition(sql, transaction: tx, cancellationToken: ct));
        return $"I{n:D7}";
    }

    public async Task<bool> ActualizarAsync(string id, InteraccionCambios c, IReadOnlyCollection<int> idsEtiquetas, CancellationToken ct)
    {
        await using var cn = await db.CrearAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            const string sql = """
                UPDATE interaccion
                SET texto = @Texto, id_tema = @IdTema, sentimiento = @Sentimiento,
                    longitud_texto = @LongitudTexto, reacciones_recibidas = @ReaccionesRecibidas
                WHERE id_interaccion = @Id
                """;
            var filas = await cn.ExecuteAsync(new CommandDefinition(sql,
                new { Id = id, c.Texto, c.IdTema, c.Sentimiento, c.LongitudTexto, c.ReaccionesRecibidas }, tx, cancellationToken: ct));
            if (filas == 0) { await tx.RollbackAsync(CancellationToken.None); return false; }

            await cn.ExecuteAsync(new CommandDefinition("DELETE FROM interaccion_etiqueta WHERE id_interaccion = @Id", new { Id = id }, tx, cancellationToken: ct));
            if (idsEtiquetas.Count > 0)
                await cn.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO interaccion_etiqueta (id_interaccion, id_etiqueta) VALUES (@Id, @IdEtiqueta)",
                    idsEtiquetas.Select(e => new { Id = id, IdEtiqueta = e }), tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> EliminarAsync(string id, CancellationToken ct)
    {
        await using var cn = await db.CrearAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            await cn.ExecuteAsync(new CommandDefinition("DELETE FROM interaccion_etiqueta WHERE id_interaccion = @Id", new { Id = id }, tx, cancellationToken: ct));
            var filas = await cn.ExecuteAsync(new CommandDefinition("DELETE FROM interaccion WHERE id_interaccion = @Id", new { Id = id }, tx, cancellationToken: ct));
            if (filas == 0) { await tx.RollbackAsync(CancellationToken.None); return false; }
            await tx.CommitAsync(ct);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
