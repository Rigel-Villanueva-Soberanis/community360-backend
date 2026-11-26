using Application.Interfaces;
using Dapper;
using Domain;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class UsuarioSistemaRepository(DbConnectionFactory db) : IUsuarioSistemaRepository
{
    public async Task<UsuarioSistema?> BuscarPorNombreAsync(string nombre, CancellationToken ct)
    {
        const string sql = """
            SELECT us.id_usuario_sistema AS Id, us.nombre_usuario AS NombreUsuario,
                   us.contrasena_hash AS ContrasenaHash, r.nombre AS Rol, us.activo AS Activo
            FROM usuario_sistema us JOIN rol r ON r.id_rol = us.id_rol
            WHERE us.nombre_usuario = @Nombre
            """;
        await using var cn = await db.CrearAsync(ct);
        return await cn.QuerySingleOrDefaultAsync<UsuarioSistema>(
            new CommandDefinition(sql, new { Nombre = nombre }, cancellationToken: ct));
    }

    public async Task<int> ContarAsync(CancellationToken ct)
    {
        await using var cn = await db.CrearAsync(ct);
        return await cn.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM usuario_sistema", cancellationToken: ct));
    }

    public async Task<bool> CrearAsync(string nombre, string hash, string rol, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO usuario_sistema (nombre_usuario, contrasena_hash, id_rol)
            SELECT @Nombre, @Hash, id_rol FROM rol WHERE nombre = @Rol
            """;
        await using var cn = await db.CrearAsync(ct);
        var filas = await cn.ExecuteAsync(new CommandDefinition(sql, new { Nombre = nombre, Hash = hash, Rol = rol }, cancellationToken: ct));
        return filas > 0;
    }
}
