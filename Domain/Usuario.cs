namespace Domain;

/// <summary>Usuario de la comunidad tal como sale de la consulta (con nombres de catálogo ya resueltos).</summary>
public class Usuario
{
    public string IdUsuario { get; set; } = "";
    public string Alias { get; set; } = "";
    public DateTime FechaIngreso { get; set; }
    public string ZonaHoraria { get; set; } = "";
    public string RangoEdad { get; set; } = "";
    public string Especie { get; set; } = "";
    public string Plataforma { get; set; } = "";
    public bool EsArtista { get; set; }
    public int Mensajes30d { get; set; }
    public int Reacciones30d { get; set; }
    public int EventosAsistidos { get; set; }
    public int ComisionesCompradas { get; set; }
    public decimal SentimientoPromedio { get; set; }
    public DateTime UltimaActividad { get; set; }
    public string Estatus { get; set; } = "";
    public string ConsentimientoDatos { get; set; } = "";
    public string? Ciudad { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
}

public class InteresUsuario
{
    public string Nombre { get; set; } = "";
    public bool EsPrincipal { get; set; }
}
