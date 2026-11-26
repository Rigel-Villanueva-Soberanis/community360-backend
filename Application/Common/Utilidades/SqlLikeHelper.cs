namespace Application.Common.Utilidades;

/// <summary>
/// Para búsquedas con LIKE: el texto del usuario se busca como texto literal.
/// Sin esto, un "%" o "_" escrito por el usuario actuaría como comodín.
/// Usar siempre con ESCAPE '\\' y el resultado como parámetro.
/// </summary>
public static class SqlLikeHelper
{
    public static string Escapar(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>Patrón "contiene" ya normalizado y escapado: %texto%.</summary>
    public static string Contiene(string? s) => "%" + Escapar(TextoNormalizer.Normalizar(s)) + "%";
}
