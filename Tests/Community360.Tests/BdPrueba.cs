using Dapper;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Community360.Tests;

/// <summary>
/// Pruebas de integración contra una base MySQL/MariaDB/TiDB con el esquema del proyecto (scripts 01 a 08).
/// Son opcionales: solo corren si existe la variable de entorno C360_TEST_MYSQL con una cadena de conexión
/// a una base DE PRUEBAS (nunca la de producción). Sin ella se omiten.
/// Ejemplo: Server=127.0.0.1;Port=3307;User ID=c360;Password=c360pass;Database=community360
/// </summary>
public static class BdPrueba
{
    public const string Variable = "C360_TEST_MYSQL";
    public static string? Cadena => Environment.GetEnvironmentVariable(Variable);
    public static bool Disponible => !string.IsNullOrWhiteSpace(Cadena);

    public static DbConnectionFactory Factory()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:TiDb"] = Cadena }).Build();
        return new DbConnectionFactory(cfg);
    }

    public static async Task EjecutarAsync(string sql, object? p = null)
    {
        await using var cn = await Factory().CrearAsync();
        await cn.ExecuteAsync(sql, p);
    }

    public static async Task<T> EscalarAsync<T>(string sql, object? p = null)
    {
        await using var cn = await Factory().CrearAsync();
        return await cn.ExecuteScalarAsync<T>(sql, p) ?? default!;
    }

    /// <summary>Borra los datos de prueba cuyo id de usuario empieza con el prefijo dado (p. ej. "U90").</summary>
    public static async Task LimpiarAsync(string prefijoUsuario)
    {
        var like = prefijoUsuario + "%";
        await EjecutarAsync("DELETE FROM interaccion_etiqueta WHERE id_interaccion IN (SELECT id_interaccion FROM interaccion WHERE id_usuario LIKE @L)", new { L = like });
        await EjecutarAsync("DELETE FROM interaccion WHERE id_usuario LIKE @L", new { L = like });
        await EjecutarAsync("DELETE FROM usuario_interes WHERE id_usuario LIKE @L", new { L = like });
        await EjecutarAsync("DELETE FROM usuario WHERE id_usuario LIKE @L", new { L = like });
    }

    public static Task InsertarUsuarioAsync(string id, string alias, string consentimiento = "si", int plataforma = 1, int especie = 1,
        string estatus = "activo", string rango = "18-24", bool artista = false, decimal sentimiento = 0.5m, string ultima = "2026-11-01")
    {
        var si = consentimiento == "si";
        return EjecutarAsync("""
            INSERT INTO usuario (id_usuario, alias, fecha_ingreso, id_ciudad, latitud, longitud, zona_horaria, rango_edad, id_especie,
                                 id_plataforma, es_artista, mensajes_30d, reacciones_30d, eventos_asistidos, comisiones_compradas,
                                 sentimiento_promedio, ultima_actividad, estatus, consentimiento_datos)
            VALUES (@Id, @Alias, '2026-01-15', @Ciudad, @Lat, @Lng, 'America/Merida', @Rango, @Especie,
                    @Plataforma, @Artista, 3, 2, 1, 0, @Sent, @Ultima, @Estatus, @Consent)
            """, new
        {
            Id = id, Alias = alias, Ciudad = si ? 1 : (int?)null, Lat = si ? 20.967m : (decimal?)null, Lng = si ? -89.623m : (decimal?)null,
            Rango = rango, Especie = especie, Plataforma = plataforma, Artista = artista, Sent = sentimiento,
            Ultima = ultima, Estatus = estatus, Consent = consentimiento
        });
    }

    public static Task InsertarInteresAsync(string idUsuario, int idInteres, bool principal)
        => EjecutarAsync("INSERT INTO usuario_interes (id_usuario, id_interes, es_principal) VALUES (@U, @I, @P)",
            new { U = idUsuario, I = idInteres, P = principal });

    public static async Task InsertarInteraccionAsync(string id, string idUsuario, string fechaHora, int plataforma = 1, int tipo = 1,
        string? texto = "hola", int tema = 1, decimal sentimiento = 0.5m, int dispositivo = 1, int? ciudad = 1, int reacciones = 0, params int[] etiquetas)
    {
        await EjecutarAsync("""
            INSERT INTO interaccion (id_interaccion, id_usuario, fecha_hora, id_plataforma, id_tipo, texto, id_tema, sentimiento, idioma,
                                     id_ciudad, id_dispositivo, longitud_texto, reacciones_recibidas)
            VALUES (@Id, @U, @F, @Pl, @Ti, @Tx, @Te, @S, 'es', @C, @D, @L, @R)
            """, new { Id = id, U = idUsuario, F = fechaHora, Pl = plataforma, Ti = tipo, Tx = texto, Te = tema, S = sentimiento, C = ciudad, D = dispositivo, L = texto?.Length ?? 0, R = reacciones });
        foreach (var e in etiquetas)
            await EjecutarAsync("INSERT INTO interaccion_etiqueta (id_interaccion, id_etiqueta) VALUES (@I, @E)", new { I = id, E = e });
    }
}

/// <summary>[Fact] que se omite si no hay base de pruebas configurada.</summary>
public sealed class FactBdAttribute : FactAttribute
{
    public FactBdAttribute()
    {
        if (!BdPrueba.Disponible) Skip = $"Define {BdPrueba.Variable} para correr las pruebas contra una base de datos.";
    }
}

[CollectionDefinition("bd", DisableParallelization = true)]
public class ColeccionBd { }
