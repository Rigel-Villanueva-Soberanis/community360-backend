namespace Application.Common.Seguridad;

/// <summary>Parámetros del token. La clave sale de JWT_KEY (backend/.env); nunca va en el código.</summary>
public sealed record JwtSettings(string Issuer, string Audience, string Key, int MinutosVida)
{
    public const int LargoMinimoClave = 32;

    public static JwtSettings Desde(Func<string, string?> leer)
    {
        var key = leer("Jwt:Key");
        if (string.IsNullOrWhiteSpace(key) || key.Length < LargoMinimoClave)
            throw new InvalidOperationException(
                $"Falta la clave del token: define JWT_KEY en backend/.env con al menos {LargoMinimoClave} caracteres.");

        var minutos = int.TryParse(leer("Jwt:MinutosVida"), out var m) && m is >= 1 and <= 1440 ? m : 60;
        return new JwtSettings(leer("Jwt:Issuer") ?? "community360", leer("Jwt:Audience") ?? "community360-clientes", key, minutos);
    }
}
