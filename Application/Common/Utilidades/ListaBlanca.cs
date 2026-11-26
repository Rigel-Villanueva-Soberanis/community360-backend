using Application.Common.ErrorCatalog;

namespace Application.Common.Utilidades;

/// <summary>
/// Para lo que NO se puede parametrizar (columna de ORDER BY): el cliente elige una clave de una lista fija
/// y a la sentencia solo llega el valor que tú definiste, nunca el texto del cliente.
/// </summary>
public static class ListaBlanca
{
    public static string Resolver(IReadOnlyDictionary<string, string> permitidos, string? clave, string porDefecto, string campo)
    {
        if (string.IsNullOrWhiteSpace(clave)) return permitidos[porDefecto];
        foreach (var (k, v) in permitidos)
            if (string.Equals(k, clave.Trim(), StringComparison.OrdinalIgnoreCase)) return v;
        throw ApiErrors.Validation([new ErrorDetalle(campo, $"Valor no permitido. Opciones: {string.Join(", ", permitidos.Keys)}.")]);
    }
}
