namespace Api.Configuration;

/// <summary>Orígenes del frontend (Vue) que pueden llamar a la API y abrir el WebSocket.</summary>
public class OrigenesPermitidos(IEnumerable<string> origenes)
{
    public static readonly string[] PorDefecto = ["https://localhost:5173", "http://localhost:5173"];
    public IReadOnlySet<string> Lista { get; } = origenes.Select(Normalizar).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool Contiene(string? origen) => !string.IsNullOrEmpty(origen) && Lista.Contains(Normalizar(origen));

    private static string Normalizar(string o) => o.Trim().TrimEnd('/');

    public static OrigenesPermitidos Leer(IConfiguration cfg)
    {
        var lista = cfg.GetSection("Cors:OrigenesPermitidos").Get<string[]>() is { Length: > 0 } l ? l : PorDefecto;
        foreach (var o in lista)
            if (!Uri.TryCreate(o, UriKind.Absolute, out var u) || (u.Scheme != "http" && u.Scheme != "https") || u.PathAndQuery.Length > 1 || o.Contains('*'))
                throw new InvalidOperationException($"Cors:OrigenesPermitidos contiene un origen no válido: '{o}'. Usa esquema://host[:puerto], sin comodines.");
        return new OrigenesPermitidos(lista);
    }
}

public static class CorsConfig
{
    public const string Politica = "frontend";

    /// <summary>
    /// CORS cerrado: solo los orígenes listados (nunca "*", porque la sesión viaja en una cookie con credenciales),
    /// solo los métodos y cabeceras que el frontend usa.
    /// </summary>
    public static IServiceCollection AddCorsFrontend(this IServiceCollection s, IConfiguration cfg)
    {
        var origenes = OrigenesPermitidos.Leer(cfg);
        s.AddSingleton(origenes);
        s.AddCors(o => o.AddPolicy(Politica, p => p
            .WithOrigins(origenes.Lista.ToArray())
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization")
            .AllowCredentials()));
        return s;
    }
}
