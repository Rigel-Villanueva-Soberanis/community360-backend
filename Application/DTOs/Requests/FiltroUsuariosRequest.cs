using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Filtros del padrón (todos opcionales). Los valores de formato fijo (estatus, rango de edad) se validan aquí;
/// plataforma y especie se resuelven contra su catálogo; el texto de búsqueda nunca se rechaza, se busca como texto.
/// </summary>
public class FiltroUsuariosRequest
{
    [RegularExpression(@"^(activo|en_riesgo|inactivo)\z", ErrorMessage = "Valores: activo, en_riesgo o inactivo.")]
    public string? Estatus { get; set; }

    [RegularExpression(@"^(18-24|25-31|32-38|39\+)\z", ErrorMessage = "Valores: 18-24, 25-31, 32-38 o 39+.")]
    public string? RangoEdad { get; set; }

    [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")] public string? Plataforma { get; set; }
    [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")] public string? Especie { get; set; }
    public bool? EsArtista { get; set; }
    [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")] public string? Buscar { get; set; }

    public int? Pagina { get; set; }
    public int? Tamano { get; set; }
    public string? Orden { get; set; }
    public bool Descendente { get; set; }
}

public class PaginacionRequest
{
    public int? Pagina { get; set; }
    public int? Tamano { get; set; }
}
