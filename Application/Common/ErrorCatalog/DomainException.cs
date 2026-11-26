namespace Application.Common.ErrorCatalog;

/// <summary>Excepción controlada del negocio. El middleware la convierte en una respuesta HTTP con su código.</summary>
public class DomainException : Exception
{
    public int StatusCode { get; }
    public string Codigo { get; }
    public IReadOnlyList<ErrorDetalle>? Errores { get; }

    public DomainException(int statusCode, string codigo, string mensaje, IReadOnlyList<ErrorDetalle>? errores = null)
        : base(mensaje)
    {
        StatusCode = statusCode;
        Codigo = codigo;
        Errores = errores;
    }
}
