using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsInteracciones;

public static class InteraccionErrors
{
    public const string ReaccionSinTexto = "Este tipo de interacción no lleva texto; déjalo vacío.";
    public const string TextoObligatorio = "Este tipo de interacción requiere texto.";
    public const string FechaFutura = "La fecha y hora no puede ser del futuro.";
    public const string EtiquetaRepetida = "Hay etiquetas repetidas.";
    public const string SentimientoFueraDeRango = "El sentimiento debe estar entre -1 y 1.";

    public static DomainException NoExiste() => ApiErrors.NotFound("La interacción");
    public static DomainException IdRepetido() => ApiErrors.Conflict("Ya existe una interacción con ese identificador.");
}
