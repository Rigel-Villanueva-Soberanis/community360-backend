namespace Api.Swagger;

/// <summary>Documentación de un endpoint. Vive en Api/Swagger/Docs, nunca en el controller.</summary>
public sealed class EndpointDoc
{
    public required string Resumen { get; init; }
    public string? Descripcion { get; init; }

    /// <summary>Código HTTP → explicación de cuándo ocurre.</summary>
    public IReadOnlyDictionary<int, string> Respuestas { get; init; } = new Dictionary<int, string>();

    /// <summary>Nombre del parámetro (ruta o query) → explicación.</summary>
    public IReadOnlyDictionary<string, string> Parametros { get; init; } = new Dictionary<string, string>();

    /// <summary>Objeto que se muestra como ejemplo del cuerpo de la petición.</summary>
    public object? EjemploPeticion { get; init; }

    /// <summary>Objeto que se muestra como ejemplo de la respuesta exitosa (2xx).</summary>
    public object? EjemploExito { get; init; }
}
