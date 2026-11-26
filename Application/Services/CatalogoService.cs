using Application.Common.ErrorsCatalogos;
using Application.Common.Utilidades;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class CatalogoService(ICatalogoRepository repo)
{
    public const string Plataformas = "plataformas";
    public const string Especies = "especies";
    public const string Temas = "temas";
    public const string Dispositivos = "dispositivos";
    public const string TiposInteraccion = "tipos-interaccion";
    public const string Etiquetas = "etiquetas";

    public async Task<IReadOnlyList<ItemCatalogoResponse>> ListarAsync(string? clave, CancellationToken ct)
    {
        var k = TextoNormalizer.Normalizar(clave).ToLowerInvariant();
        var items = await repo.ListarAsync(k, ct) ?? throw CatalogoErrors.NoExiste(repo.Claves);
        return items.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<ItemCatalogoResponse>>> ListarTodosAsync(CancellationToken ct)
    {
        var r = new Dictionary<string, IReadOnlyList<ItemCatalogoResponse>>();
        foreach (var clave in repo.Claves)
            r[clave] = (await repo.ListarAsync(clave, ct) ?? []).Select(Mapear).ToList();
        return r;
    }

    // ---- Traducción nombre → valor del catálogo (la usan Interacciones y Usuarios).
    // El nombre nunca viaja a una sentencia SQL: se compara en memoria contra la lista del catálogo.

    public Task<ItemCatalogo> PlataformaAsync(string? nombre, string campo, CancellationToken ct) => ResolverAsync(Plataformas, nombre, campo, ct);
    public Task<ItemCatalogo> EspecieAsync(string? nombre, string campo, CancellationToken ct) => ResolverAsync(Especies, nombre, campo, ct);
    public Task<ItemCatalogo> TemaAsync(string? nombre, string campo, CancellationToken ct) => ResolverAsync(Temas, nombre, campo, ct);
    public Task<ItemCatalogo> DispositivoAsync(string? nombre, string campo, CancellationToken ct) => ResolverAsync(Dispositivos, nombre, campo, ct);
    public Task<ItemCatalogo> TipoAsync(string? nombre, string campo, CancellationToken ct) => ResolverAsync(TiposInteraccion, nombre, campo, ct);

    public async Task<IReadOnlyList<ItemCatalogo>> EtiquetasAsync(IEnumerable<string>? nombres, string campo, CancellationToken ct)
    {
        var resultado = new List<ItemCatalogo>();
        foreach (var n in (nombres ?? []).Select(x => TextoNormalizer.Normalizar(x)).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            resultado.Add(await ResolverAsync(Etiquetas, n, campo, ct));
        return resultado;
    }

    private async Task<ItemCatalogo> ResolverAsync(string clave, string? nombre, string campo, CancellationToken ct)
    {
        var items = await repo.ListarAsync(clave, ct) ?? [];
        var buscado = TextoNormalizer.Normalizar(nombre);
        return items.FirstOrDefault(i => string.Equals(i.Nombre, buscado, StringComparison.OrdinalIgnoreCase))
               ?? throw CatalogoErrors.ValorNoPermitido(campo, items.Select(i => i.Nombre));
    }

    private static ItemCatalogoResponse Mapear(ItemCatalogo i) => new(i.Id, i.Nombre, i.AdmiteTexto);
}
