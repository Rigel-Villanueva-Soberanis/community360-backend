using Application.DTOs.Responses;

namespace Application.Interfaces;

/// <summary>Avisa a los dashboards conectados (WebSocket, paso 10). Mientras tanto hay una implementación que no hace nada.</summary>
public interface IRealtimeNotifier
{
    Task InteraccionCreadaAsync(InteraccionResponse interaccion, CancellationToken ct);
    Task InteraccionModificadaAsync(InteraccionResponse interaccion, CancellationToken ct);
    Task InteraccionEliminadaAsync(InteraccionResponse eliminada, CancellationToken ct);
}
