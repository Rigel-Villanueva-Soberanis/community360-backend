namespace Application.DTOs.Requests;

/// <summary>Filtros de auditoría. Solo valores de listas fijas (estadoRevision, operacion) y paginación.</summary>
public class FiltroAuditoriaRequest
{
    /// <summary>pendiente, descartada o recuperada (solo cuarentena).</summary>
    public string? EstadoRevision { get; set; }
    /// <summary>alta, modificacion, baja o rechazo (solo bitacora-api).</summary>
    public string? Operacion { get; set; }
    public int? Pagina { get; set; }
    public int? Tamano { get; set; }
}
