using System.Data.Common;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Infrastructure.Data;

/// <summary>Crea conexiones a TiDB. No contiene SQL; los repositorios lo ejecutan.</summary>
public class DbConnectionFactory(IConfiguration cfg)
{
    public async Task<DbConnection> CrearAsync(CancellationToken ct = default)
    {
        var cs = cfg.GetConnectionString("TiDb");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Falta ConnectionStrings:TiDb (secretos de usuario o variable de entorno).");

        var cn = new MySqlConnection(cs);
        await cn.OpenAsync(ct);
        return cn;
    }
}
