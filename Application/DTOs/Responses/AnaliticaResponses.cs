namespace Application.DTOs.Responses;

/// <summary>I1 a I4. Con cero interacciones todos los valores son 0.</summary>
public class ResumenResponse
{
    public int TotalInteracciones { get; set; }
    public decimal SentimientoPromedio { get; set; }
    public decimal ReaccionesPromedio { get; set; }
    public decimal PorcentajeNegativo { get; set; }
}

/// <summary>Forma común para dona, barras y rankings: etiqueta + valor (+ total de filas que lo componen).</summary>
public class PuntoSerieResponse
{
    public string Etiqueta { get; set; } = "";
    public decimal Valor { get; set; }
    public int Total { get; set; }
}

public class CeldaHeatmapResponse
{
    /// <summary>1 = domingo ... 7 = sábado (UTC).</summary>
    public int Dia { get; set; }
    public int Hora { get; set; }
    public int Total { get; set; }
}

public class TendenciaDiariaResponse
{
    public DateOnly Fecha { get; set; }
    public int Total { get; set; }
    public decimal Sentimiento { get; set; }
}

/// <summary>I1 a I9 juntos. Lo que el WebSocket manda como metricas.actualizadas (paso 10).</summary>
public class SnapshotMetricasResponse
{
    public ResumenResponse Resumen { get; set; } = new();
    public IReadOnlyList<PuntoSerieResponse> PorPlataforma { get; set; } = [];
    public IReadOnlyList<PuntoSerieResponse> PorTipo { get; set; } = [];
    public IReadOnlyList<CeldaHeatmapResponse> ActividadPorHora { get; set; } = [];
    public IReadOnlyList<PuntoSerieResponse> SentimientoPorTema { get; set; } = [];
    public IReadOnlyList<PuntoSerieResponse> EtiquetasTop { get; set; } = [];
    public DateTime CalculadoUtc { get; set; }
}
