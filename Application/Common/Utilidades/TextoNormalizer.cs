using System.Text;

namespace Application.Common.Utilidades;

/// <summary>
/// Deja el texto del cliente en una forma estable y segura de guardar, SIN prohibirle nada de lo que escribió:
/// comillas, "--", "DROP", "$ne" o "&lt;script&gt;" se conservan tal cual (viajan siempre como parámetro).
/// Solo se quita lo que no es texto visible o que rompe el almacenamiento.
/// </summary>
public static class TextoNormalizer
{
    public static string Normalizar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];

            if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                sb.Append(c).Append(s[i + 1]);   // emoji y otros caracteres fuera del BMP: se conservan
                i++;
                continue;
            }
            if (char.IsSurrogate(c)) continue;   // sustituto suelto (UTF-16 inválido): rompe JSON y MySQL

            if (c == '\r') { if (i + 1 >= s.Length || s[i + 1] != '\n') sb.Append('\n'); continue; }
            if (c is '\n' or '\t') { sb.Append(c); continue; }
            if (char.IsControl(c) || EsInvisibleEnganoso(c)) continue;   // incluye NUL

            sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

    /// <summary>
    /// Caracteres sin forma visible que sirven para esconder texto o invertir su sentido (bidi).
    /// Se conservan U+200C y U+200D porque forman parte de emojis compuestos y de varios alfabetos.
    /// </summary>
    private static bool EsInvisibleEnganoso(char c) =>
        c is '​' or '‎' or '‏' or '⁠' or '﻿'
          or (>= '‪' and <= '‮')
          or (>= '⁦' and <= '⁩');
}
