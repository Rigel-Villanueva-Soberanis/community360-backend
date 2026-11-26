namespace Application.Common.ErrorCatalog;

/// <summary>Error de un campo concreto: { campo, mensaje }.</summary>
public record ErrorDetalle(string Campo, string Mensaje);
