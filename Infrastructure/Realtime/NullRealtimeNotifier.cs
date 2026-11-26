using Application.DTOs.Responses;
using Application.Interfaces;

namespace Infrastructure.Realtime;

/// <summary>No avisa a nadie. Se reemplaza por la implementación de WebSockets en el paso 10.</summary>
public class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task InteraccionCreadaAsync(InteraccionResponse interaccion, CancellationToken ct) => Task.CompletedTask;
    public Task InteraccionModificadaAsync(InteraccionResponse interaccion, CancellationToken ct) => Task.CompletedTask;
    public Task InteraccionEliminadaAsync(InteraccionResponse eliminada, CancellationToken ct) => Task.CompletedTask;
}
