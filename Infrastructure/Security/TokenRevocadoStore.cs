using System.Collections.Concurrent;
using Application.Interfaces;

namespace Infrastructure.Security;

/// <summary>
/// Lista de tokens cerrados (por jti) hasta que venzan. Vive en memoria: si el servidor se reinicia, se pierde
/// (limitación a mencionar; en producción iría en Couchbase con expiración automática).
/// </summary>
public class TokenRevocadoStore : ITokenRevocadoStore
{
    private readonly ConcurrentDictionary<string, DateTime> _revocados = new();

    public void Revocar(string jti, DateTime expiraUtc)
    {
        Limpiar();
        _revocados[jti] = expiraUtc;
    }

    public bool EstaRevocado(string jti) => _revocados.ContainsKey(jti);

    private void Limpiar()
    {
        foreach (var (k, v) in _revocados)
            if (v < DateTime.UtcNow) _revocados.TryRemove(k, out _);
    }
}
