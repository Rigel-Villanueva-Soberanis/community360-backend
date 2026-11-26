using System.Text.Json.Serialization;

namespace Application.DTOs.Responses;

public class InteraccionResponse
{
    public string IdInteraccion { get; set; } = "";
    public string IdUsuario { get; set; } = "";
    public DateTime FechaHoraUtc { get; set; }
    public string Plataforma { get; set; } = "";
    public string Tipo { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Texto { get; set; }
    public string Tema { get; set; } = "";
    public decimal Sentimiento { get; set; }
    public string Idioma { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Ciudad { get; set; }
    public string Dispositivo { get; set; } = "";
    public int LongitudTexto { get; set; }
    public int ReaccionesRecibidas { get; set; }
    public IReadOnlyList<string> Etiquetas { get; set; } = [];
}
