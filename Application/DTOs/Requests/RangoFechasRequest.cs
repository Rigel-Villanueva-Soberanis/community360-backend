using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Único dato del cliente en analítica: dos fechas (AAAA-MM-DD). Cualquier otra cosa no es fecha y se rechaza.</summary>
public class RangoFechasRequest : IValidatableObject
{
    public const int MaxDias = 366;

    [Required(ErrorMessage = "La fecha inicial es obligatoria.")] public DateOnly? Desde { get; set; }
    [Required(ErrorMessage = "La fecha final es obligatoria.")] public DateOnly? Hasta { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext c)
    {
        if (Desde is null || Hasta is null) yield break;
        if (Hasta < Desde) yield return new("'hasta' debe ser mayor o igual que 'desde'.", [nameof(Hasta)]);
        else if (Hasta.Value.DayNumber - Desde.Value.DayNumber > MaxDias) yield return new($"El rango máximo es {MaxDias} días.", [nameof(Hasta)]);
    }
}
