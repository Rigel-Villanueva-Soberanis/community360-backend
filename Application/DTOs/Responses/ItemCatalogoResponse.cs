using System.Text.Json.Serialization;

namespace Application.DTOs.Responses;

public record ItemCatalogoResponse(
    int Id,
    string Nombre,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? AdmiteTexto = null);
