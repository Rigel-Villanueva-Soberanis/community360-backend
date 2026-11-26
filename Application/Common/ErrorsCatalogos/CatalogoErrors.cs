using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsCatalogos;

public static class CatalogoErrors
{
    /// <summary>404. No repite el texto del cliente; lista los catálogos que sí existen.</summary>
    public static DomainException NoExiste(IEnumerable<string> disponibles)
        => new(404, "CATALOGO_NOT_FOUND", $"El catálogo solicitado no existe. Disponibles: {string.Join(", ", disponibles)}.");

    /// <summary>422 cuando un valor enviado no está en su catálogo.</summary>
    public static DomainException ValorNoPermitido(string campo, IEnumerable<string> validos)
        => ApiErrors.Validation([new ErrorDetalle(campo, $"Valor no permitido. Opciones: {string.Join(", ", validos)}.")]);
}
