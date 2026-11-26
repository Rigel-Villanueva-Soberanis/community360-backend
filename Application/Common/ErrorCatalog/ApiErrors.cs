namespace Application.Common.ErrorCatalog;

/// <summary>Códigos de error genéricos. Cada módulo agrega los suyos en su carpeta Errors{Modulo}.</summary>
public static class ApiErrors
{
    public static DomainException NotFound(string que) => new(404, "NOT_FOUND", $"{que} no existe.");
    public static DomainException Conflict(string msg) => new(409, "CONFLICT", msg);
    public static DomainException Forbidden() => new(403, "FORBIDDEN", "No tienes permiso para esta operación.");
    public static DomainException Unauthorized(string m) => new(401, "UNAUTHORIZED", m);
    public static DomainException BadRequest(string msg) => new(400, "BAD_REQUEST", msg);

    public static DomainException Validation(IReadOnlyList<ErrorDetalle> e)
        => new(422, "VALIDATION_ERROR", "Hay datos que no cumplen las reglas.", e);
}
