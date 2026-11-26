using System.Text.Json.Serialization;

namespace Application.DTOs.Responses;

/// <summary>Ciudad, latitud, longitud y zona horaria solo aparecen si el usuario dio consentimiento.</summary>
public class UsuarioResponse
{
    public string IdUsuario { get; set; } = "";
    public string Alias { get; set; } = "";
    public DateOnly FechaIngreso { get; set; }
    public string RangoEdad { get; set; } = "";
    public string Especie { get; set; } = "";
    public string Plataforma { get; set; } = "";
    public bool EsArtista { get; set; }
    public string Estatus { get; set; } = "";
    public decimal SentimientoPromedio { get; set; }
    public DateOnly UltimaActividad { get; set; }
    public int Mensajes30d { get; set; }
    public int Reacciones30d { get; set; }
    public int EventosAsistidos { get; set; }
    public int ComisionesCompradas { get; set; }
    public string ConsentimientoDatos { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Ciudad { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Latitud { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public decimal? Longitud { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ZonaHoraria { get; set; }

    /// <summary>Solo en el detalle de un usuario.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public IReadOnlyList<InteresUsuarioResponse>? Intereses { get; set; }
}

public record InteresUsuarioResponse(string Nombre, bool EsPrincipal);
