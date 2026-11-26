using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Alta. El cliente NO manda longitud del texto ni ciudad: la API los calcula (la ciudad solo si el usuario dio consentimiento).
/// Plataforma, tipo, tema, dispositivo y etiquetas se escriben por nombre y se validan contra su catálogo.
/// El texto libre nunca se rechaza por su contenido, solo por su largo (500 caracteres).
/// </summary>
public class CreateInteraccionRequest
{
    [RegularExpression(@"^I\d{7}\z", ErrorMessage = "Formato esperado: I0000000.")]
    public string? IdInteraccion { get; set; }

    [Required(ErrorMessage = "El usuario es obligatorio.")] public string IdUsuario { get; set; } = "";
    public DateTime? FechaHora { get; set; }
    [Required(ErrorMessage = "La plataforma es obligatoria.")] public string Plataforma { get; set; } = "";
    [Required(ErrorMessage = "El tipo es obligatorio.")] public string Tipo { get; set; } = "";
    public string? Texto { get; set; }
    [Required(ErrorMessage = "El tema es obligatorio.")] public string Tema { get; set; } = "";

    [Required(ErrorMessage = "El sentimiento es obligatorio."), Range(-1.0, 1.0, ErrorMessage = "El sentimiento debe estar entre -1 y 1.")]
    public decimal? Sentimiento { get; set; }

    [RegularExpression(@"^[a-z]{2}\z", ErrorMessage = "Código de idioma de 2 letras minúsculas (por ejemplo es).")]
    public string Idioma { get; set; } = "es";

    [Required(ErrorMessage = "El dispositivo es obligatorio.")] public string Dispositivo { get; set; } = "";
    [Range(0, 1_000_000, ErrorMessage = "Debe estar entre 0 y 1,000,000.")] public int ReaccionesRecibidas { get; set; }
    [MaxLength(7, ErrorMessage = "Máximo 7 etiquetas.")] public List<string> Etiquetas { get; set; } = [];
}

/// <summary>Modificación: solo los campos editables. El id va en la URL.</summary>
public class UpdateInteraccionRequest
{
    public string? Texto { get; set; }
    [Required(ErrorMessage = "El tema es obligatorio.")] public string Tema { get; set; } = "";

    [Required(ErrorMessage = "El sentimiento es obligatorio."), Range(-1.0, 1.0, ErrorMessage = "El sentimiento debe estar entre -1 y 1.")]
    public decimal? Sentimiento { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Debe estar entre 0 y 1,000,000.")] public int ReaccionesRecibidas { get; set; }
    [MaxLength(7, ErrorMessage = "Máximo 7 etiquetas.")] public List<string> Etiquetas { get; set; } = [];
}

public class FiltroInteraccionesRequest
{
    public string? IdUsuario { get; set; }
    public string? Plataforma { get; set; }
    public string? Tipo { get; set; }
    public string? Tema { get; set; }
    public string? Etiqueta { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? Pagina { get; set; }
    public int? Tamano { get; set; }
    public string? Orden { get; set; }
    public bool? Descendente { get; set; }
}
