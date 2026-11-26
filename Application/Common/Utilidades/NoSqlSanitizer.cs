using System.Text.RegularExpressions;
using Application.Common.ErrorCatalog;

namespace Application.Common.Utilidades;

/// <summary>
/// Sanitización de lo que llega del cliente.
/// Principio: el texto nunca se rechaza por "parecer" una inyección; se normaliza y se acota, y la defensa real
/// es que viaja SIEMPRE como parámetro (@param en SQL, $param en N1QL), así que se guarda y se devuelve como texto literal.
/// Lo único que se valida es lo que tiene un formato fijo por diseño: claves de documento, ids y catálogos.
/// </summary>
public static partial class NoSqlSanitizer
{
    [GeneratedRegex(@"^[A-Za-z0-9_:\-]{1,80}\z")] private static partial Regex ClaveRx();

    /// <summary>Clave de documento Couchbase: letras, números, '_', ':' y '-', máximo 80.</summary>
    public static bool ClaveValida(string? clave) => !string.IsNullOrEmpty(clave) && ClaveRx().IsMatch(clave);

    /// <summary>Igual que <see cref="ClaveValida"/> pero lanza 422 si no cumple.</summary>
    public static string ValidarClave(string? clave, string campo)
        => ClaveValida(clave) ? clave! : throw ApiErrors.Validation([new ErrorDetalle(campo, "Formato de identificador no válido.")]);

    /// <summary>
    /// Texto libre: se normaliza (<see cref="TextoNormalizer"/>) y se comprueba el largo máximo en caracteres.
    /// NO se rechaza por su contenido. Devuelve "" si viene vacío.
    /// </summary>
    public static string Limpiar(string? valor, string campo, int maxLargo)
    {
        var t = TextoNormalizer.Normalizar(valor);
        if (t.EnumerateRunes().Count() > maxLargo)
            throw ApiErrors.Validation([new ErrorDetalle(campo, $"Máximo {maxLargo} caracteres.")]);
        return t;
    }

    /// <summary>Como <see cref="Limpiar"/>, pero devuelve null si el texto queda vacío.</summary>
    public static string? LimpiarOpcional(string? valor, string campo, int maxLargo)
    {
        var t = Limpiar(valor, campo, maxLargo);
        return t.Length == 0 ? null : t;
    }
}
