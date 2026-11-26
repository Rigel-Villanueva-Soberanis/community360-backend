using Application.DTOs.Responses;

namespace Application.Interfaces;

/// <summary>Fila cruda de una agrupación (etiqueta, valor y cuántas filas la componen).</summary>
public record FilaAgrupada(string Etiqueta, decimal Valor, int Total);

public interface IAnaliticaRepository
{
    Task<ResumenResponse> ResumenAsync(CancellationToken ct);
    Task<IReadOnlyList<FilaAgrupada>> InteraccionesPorPlataformaAsync(CancellationToken ct);
    Task<IReadOnlyList<FilaAgrupada>> InteraccionesPorTipoAsync(CancellationToken ct);
    Task<IReadOnlyList<CeldaHeatmapResponse>> ActividadPorHoraAsync(CancellationToken ct);
    Task<IReadOnlyList<FilaAgrupada>> SentimientoPorTemaAsync(CancellationToken ct);
    Task<IReadOnlyList<FilaAgrupada>> EtiquetasTopAsync(int limite, CancellationToken ct);
    Task<IReadOnlyList<FilaAgrupada>> UsuariosPorEstatusAsync(CancellationToken ct);
    Task<IReadOnlyList<FilaAgrupada>> UsuariosPorPlataformaAsync(CancellationToken ct);
    Task<IReadOnlyList<FilaAgrupada>> UsuariosPorRangoEdadAsync(CancellationToken ct);
    /// <summary>Solo usuarios con consentimiento "si".</summary>
    Task<IReadOnlyList<FilaAgrupada>> UsuariosPorPaisAsync(CancellationToken ct);
    /// <summary>Hasta es exclusivo (el servicio manda el día siguiente al final del rango).</summary>
    Task<IReadOnlyList<TendenciaDiariaResponse>> TendenciaDiariaAsync(DateTime desde, DateTime hastaExclusivo, CancellationToken ct);
}
