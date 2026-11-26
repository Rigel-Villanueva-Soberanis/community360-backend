using Couchbase;
using Couchbase.KeyValue;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data;

/// <summary>Conexión única (singleton) a Couchbase Capella.</summary>
public class CouchbaseContext(IConfiguration cfg)
{
    private ICluster? _cluster;
    private IScope? _scope;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public string Bucket { get; } = cfg["Couchbase:Bucket"] ?? "community360";
    public string ScopeName { get; } = cfg["Couchbase:Scope"] ?? "_default";

    public async Task<ICluster> ClusterAsync()
    {
        if (_cluster is not null) return _cluster;
        await _lock.WaitAsync();
        try
        {
            _cluster ??= await Cluster.ConnectAsync(
                Requerido("Couchbase:ConnectionString"),
                Requerido("Couchbase:Usuario"),
                Requerido("Couchbase:Password"));
            return _cluster;
        }
        finally { _lock.Release(); }
    }

    public async Task<ICouchbaseCollection> ColeccionAsync(string nombre)
    {
        var cluster = await ClusterAsync();
        _scope ??= await (await cluster.BucketAsync(Bucket)).ScopeAsync(ScopeName);
        return await _scope.CollectionAsync(nombre);
    }

    /// <summary>
    /// Prefijo seguro para N1QL: `bucket`.`scope`.`coleccion`.
    /// Solo debe llamarse con constantes del código ("cuarentena", "diario_cambios"...), nunca con texto del cliente.
    /// </summary>
    public string Ruta(string coleccion) => $"`{Bucket}`.`{ScopeName}`.`{coleccion}`";

    private string Requerido(string clave) => cfg[clave] is { Length: > 0 } v
        ? v
        : throw new InvalidOperationException($"Falta {clave} (secretos de usuario o variable de entorno).");
}
