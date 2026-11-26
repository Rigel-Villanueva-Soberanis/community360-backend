using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;

namespace Application.Services;

/// <summary>Calcula todos los indicadores. Lo usan REST y WebSocket, así dan exactamente los mismos números.</summary>
public class MetricasService(IAnaliticaRepository repo)
{
    public const int TopEtiquetas = 10;

    public Task<ResumenResponse> ResumenAsync(CancellationToken ct) => repo.ResumenAsync(ct);

    /// <summary>I5: porcentaje por plataforma; con el método del mayor residuo para que sume exactamente 100.0.</summary>
    public async Task<IReadOnlyList<PuntoSerieResponse>> PorPlataformaAsync(CancellationToken ct)
        => Porcentajes(await repo.InteraccionesPorPlataformaAsync(ct));

    public async Task<IReadOnlyList<PuntoSerieResponse>> PorTipoAsync(CancellationToken ct) => Mapear(await repo.InteraccionesPorTipoAsync(ct));
    public Task<IReadOnlyList<CeldaHeatmapResponse>> ActividadPorHoraAsync(CancellationToken ct) => repo.ActividadPorHoraAsync(ct);
    public async Task<IReadOnlyList<PuntoSerieResponse>> SentimientoPorTemaAsync(CancellationToken ct)
        => (await repo.SentimientoPorTemaAsync(ct)).Select(f => new PuntoSerieResponse { Etiqueta = f.Etiqueta, Valor = Math.Round(f.Valor, 3), Total = f.Total }).ToList();
    public async Task<IReadOnlyList<PuntoSerieResponse>> EtiquetasTopAsync(CancellationToken ct) => Mapear(await repo.EtiquetasTopAsync(TopEtiquetas, ct));

    public async Task<IReadOnlyList<PuntoSerieResponse>> UsuariosPorEstatusAsync(CancellationToken ct) => Porcentajes(await repo.UsuariosPorEstatusAsync(ct));
    public async Task<IReadOnlyList<PuntoSerieResponse>> UsuariosPorPlataformaAsync(CancellationToken ct) => Mapear(await repo.UsuariosPorPlataformaAsync(ct));
    public async Task<IReadOnlyList<PuntoSerieResponse>> UsuariosPorRangoEdadAsync(CancellationToken ct) => Mapear(await repo.UsuariosPorRangoEdadAsync(ct));
    public async Task<IReadOnlyList<PuntoSerieResponse>> UsuariosPorPaisAsync(CancellationToken ct) => Mapear(await repo.UsuariosPorPaisAsync(ct));

    public async Task<IReadOnlyList<TendenciaDiariaResponse>> TendenciaDiariaAsync(RangoFechasRequest r, CancellationToken ct)
    {
        var desde = r.Desde!.Value.ToDateTime(TimeOnly.MinValue);
        var hasta = r.Hasta!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var filas = await repo.TendenciaDiariaAsync(desde, hasta, ct);
        return filas.Select(f => new TendenciaDiariaResponse { Fecha = f.Fecha, Total = f.Total, Sentimiento = Math.Round(f.Sentimiento, 3) }).ToList();
    }

    public async Task<SnapshotMetricasResponse> SnapshotAsync(CancellationToken ct) => new()
    {
        Resumen = await ResumenAsync(ct),
        PorPlataforma = await PorPlataformaAsync(ct),
        PorTipo = await PorTipoAsync(ct),
        ActividadPorHora = await ActividadPorHoraAsync(ct),
        SentimientoPorTema = await SentimientoPorTemaAsync(ct),
        EtiquetasTop = await EtiquetasTopAsync(ct),
        CalculadoUtc = DateTime.UtcNow
    };

    private static IReadOnlyList<PuntoSerieResponse> Mapear(IReadOnlyList<FilaAgrupada> f)
        => f.Select(x => new PuntoSerieResponse { Etiqueta = x.Etiqueta, Valor = x.Valor, Total = x.Total }).ToList();

    /// <summary>Convierte conteos en porcentajes con 1 decimal que suman exactamente 100.0 (mayor residuo). Lista vacía si no hay datos.</summary>
    public static IReadOnlyList<PuntoSerieResponse> Porcentajes(IReadOnlyList<FilaAgrupada> filas)
    {
        var suma = filas.Sum(f => f.Total);
        if (suma == 0) return [];
        var brutos = filas.Select(f => f.Total * 1000m / suma).ToList();      // décimas de punto porcentual
        var bases = brutos.Select(b => Math.Floor(b)).ToList();
        var faltan = 1000 - (int)bases.Sum();
        foreach (var i in Enumerable.Range(0, filas.Count).OrderByDescending(i => brutos[i] - bases[i]).Take(faltan)) bases[i] += 1;
        return filas.Select((f, i) => new PuntoSerieResponse { Etiqueta = f.Etiqueta, Valor = bases[i] / 10m, Total = f.Total }).ToList();
    }
}
