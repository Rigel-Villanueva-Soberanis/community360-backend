namespace Domain;

/// <summary>Interacción tal como sale de la consulta, con catálogos resueltos y etiquetas en una sola cadena.</summary>
public class InteraccionLectura
{
    public string IdInteraccion { get; set; } = "";
    public string IdUsuario { get; set; } = "";
    public DateTime FechaHora { get; set; }
    public string Plataforma { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? Texto { get; set; }
    public string Tema { get; set; } = "";
    public decimal Sentimiento { get; set; }
    public string Idioma { get; set; } = "";
    public string? Ciudad { get; set; }
    public string Dispositivo { get; set; } = "";
    public int LongitudTexto { get; set; }
    public int ReaccionesRecibidas { get; set; }
    public string? EtiquetasCsv { get; set; }
}
