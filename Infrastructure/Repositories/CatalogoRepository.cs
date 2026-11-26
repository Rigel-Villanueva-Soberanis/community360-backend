using System.Collections.Concurrent;
using Application.Interfaces;
using Dapper;
using Domain;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Catálogos de solo lectura. El nombre de tabla no se puede parametrizar, por eso cada catálogo sale de un diccionario fijo:
/// el texto del cliente nunca llega a la sentencia. Los catálogos casi no cambian, así que se guardan 5 minutos en memoria.
/// </summary>
public class CatalogoRepository(DbConnectionFactory db) : ICatalogoRepository
{
    private static readonly TimeSpan Vida = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<string, string> Catalogos = new()
    {
        ["plataformas"] = "SELECT id_plataforma AS Id, nombre AS Nombre FROM plataforma ORDER BY nombre",
        ["especies"] = "SELECT id_especie AS Id, nombre AS Nombre FROM especie ORDER BY nombre",
        ["intereses"] = "SELECT id_interes AS Id, nombre AS Nombre FROM interes ORDER BY nombre",
        ["temas"] = "SELECT id_tema AS Id, nombre AS Nombre FROM tema ORDER BY nombre",
        ["tipos-interaccion"] = "SELECT id_tipo AS Id, nombre AS Nombre, admite_texto AS AdmiteTexto FROM tipo_interaccion ORDER BY nombre",
        ["dispositivos"] = "SELECT id_dispositivo AS Id, nombre AS Nombre FROM dispositivo ORDER BY nombre",
        ["etiquetas"] = "SELECT id_etiqueta AS Id, nombre AS Nombre FROM etiqueta ORDER BY nombre",
    };

    private readonly ConcurrentDictionary<string, (DateTime Hasta, IReadOnlyList<ItemCatalogo> Items)> _cache = new();

    public IReadOnlyList<string> Claves { get; } = Catalogos.Keys.ToList();

    public async Task<IReadOnlyList<ItemCatalogo>?> ListarAsync(string clave, CancellationToken ct)
    {
        if (!Catalogos.TryGetValue(clave, out var sql)) return null;   // 404 en el service

        if (_cache.TryGetValue(clave, out var c) && c.Hasta > DateTime.UtcNow) return c.Items;

        await using var cn = await db.CrearAsync(ct);
        var items = (await cn.QueryAsync<ItemCatalogo>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
        _cache[clave] = (DateTime.UtcNow + Vida, items);
        return items;
    }
}
