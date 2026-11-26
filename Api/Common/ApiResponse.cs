using Application.Common.ErrorCatalog;

namespace Api.Common;

/// <summary>Modelo estándar de respuesta para todos los endpoints.</summary>
public class ApiResponse<T>
{
    public bool Exito { get; init; }
    public string? Mensaje { get; init; }
    public T? Datos { get; init; }
    public IReadOnlyList<ErrorDetalle>? Errores { get; init; }

    public static ApiResponse<T> Ok(T datos, string? mensaje = null)
        => new() { Exito = true, Datos = datos, Mensaje = mensaje };

    public static ApiResponse<T> Falla(string mensaje, IReadOnlyList<ErrorDetalle>? errores = null)
        => new() { Exito = false, Mensaje = mensaje, Errores = errores };
}
