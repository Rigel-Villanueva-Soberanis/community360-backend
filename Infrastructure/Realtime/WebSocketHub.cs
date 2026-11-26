using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Realtime;

/// <summary>
/// Mantiene los sockets abiertos y difunde los cambios. Singleton: como MetricasService es scoped, se crea un
/// ámbito por difusión. Los mensajes llevan solo id, plataforma, tipo y sentimiento (nunca texto ni ubicación)
/// más el snapshot de métricas calculado DESPUÉS del cambio.
/// </summary>
public class WebSocketHub(IServiceScopeFactory scopes, ILogger<WebSocketHub> log) : IRealtimeNotifier, IConexionesTiempoReal
{
    public const int MaxConexiones = 100;
    private static readonly TimeSpan TiempoEnvio = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed class Cliente(WebSocket socket) { public WebSocket Socket { get; } = socket; public SemaphoreSlim Envio { get; } = new(1, 1); }
    private readonly ConcurrentDictionary<Guid, Cliente> _clientes = new();

    public int Conectados => _clientes.Count;
    public bool HayCupo => _clientes.Count < MaxConexiones;

    public async Task AtenderAsync(WebSocket socket, string usuario, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var cliente = new Cliente(socket);
        _clientes[id] = cliente;
        try
        {
            await EnviarAsync(cliente, new { tipo = "conexion.establecida", usuario, fechaUtc = DateTime.UtcNow, metricas = await SnapshotAsync(ct) }, ct);
            var buffer = new byte[256];                     // el cliente no manda datos útiles: solo se detecta el cierre
            while (socket.State == WebSocketState.Open)
            {
                var r = await socket.ReceiveAsync(buffer, ct);
                if (r.MessageType == WebSocketMessageType.Close) break;
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException) { /* cierre abrupto o servidor deteniéndose */ }
        finally
        {
            _clientes.TryRemove(id, out _);
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); } catch { /* ya cerrado */ }
            }
        }
    }

    public Task InteraccionCreadaAsync(InteraccionResponse i, CancellationToken ct) => DifundirAsync("interaccion.creada", Resumen(i), ct);
    public Task InteraccionModificadaAsync(InteraccionResponse i, CancellationToken ct) => DifundirAsync("interaccion.modificada", Resumen(i), ct);
    public Task InteraccionEliminadaAsync(InteraccionResponse e, CancellationToken ct) => DifundirAsync("interaccion.eliminada", Resumen(e), ct);

    private static object Resumen(InteraccionResponse i) => new { i.IdInteraccion, i.Plataforma, i.Tipo, i.Sentimiento };

    private async Task<SnapshotMetricasResponse> SnapshotAsync(CancellationToken ct)
    {
        await using var ambito = scopes.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<MetricasService>().SnapshotAsync(ct);
    }

    private async Task DifundirAsync(string tipo, object datos, CancellationToken ct)
    {
        if (_clientes.IsEmpty) return;
        var mensaje = new { tipo, fechaUtc = DateTime.UtcNow, datos, metricas = await SnapshotAsync(ct) };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(mensaje, Json);
        await Task.WhenAll(_clientes.Select(async par =>
        {
            try { await EnviarBytesAsync(par.Value, bytes, ct); }
            catch (Exception ex)
            {
                log.LogWarning("Cliente de tiempo real descartado: {Tipo}", ex.GetType().Name);
                _clientes.TryRemove(par.Key, out _);
            }
        }));
    }

    private static Task EnviarAsync(Cliente c, object obj, CancellationToken ct) => EnviarBytesAsync(c, JsonSerializer.SerializeToUtf8Bytes(obj, Json), ct);

    // Un socket no admite dos envíos a la vez; y un cliente lento no debe frenar a los demás ni a la petición HTTP.
    private static async Task EnviarBytesAsync(Cliente c, byte[] bytes, CancellationToken ct)
    {
        if (c.Socket.State != WebSocketState.Open) return;
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TiempoEnvio);
        await c.Envio.WaitAsync(limite.Token);
        try { await c.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, limite.Token); }
        finally { c.Envio.Release(); }
    }
}
