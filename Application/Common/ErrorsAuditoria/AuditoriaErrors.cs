using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsAuditoria;

public static class AuditoriaErrors
{
    public static DomainException NoExiste() => ApiErrors.NotFound("El registro de cuarentena");
}
