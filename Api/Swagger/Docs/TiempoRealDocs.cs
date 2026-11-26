namespace Api.Swagger.Docs;

/// <summary>Documentación del módulo Tiempo real (TiempoRealController y canal /ws).</summary>
public static class TiempoRealDocs
{
    public const string Controller = "TiempoReal";

    public static readonly IReadOnlyDictionary<string, EndpointDoc> Entradas = new Dictionary<string, EndpointDoc>
    {
        ["Estado"] = new()
        {
            Resumen = "Estado del canal WebSocket",
            Descripcion = "El canal en vivo es `wss://<host>/ws`; el token viaja en la cookie `c360_token` o en `?access_token=`. " +
                          "Cada vez que se crea, modifica o elimina una interacción, el servidor envía un mensaje JSON a todos los clientes conectados " +
                          "para que el dashboard se actualice sin recargar. Mensajes: `conexion.establecida`, `interaccion.creada`, `interaccion.modificada`, `interaccion.eliminada`; " +
                          "cada uno trae `tipo`, `fechaUtc`, `datos` (id, plataforma, tipo y sentimiento; nunca texto ni ubicación) y `metricas` recalculadas después del cambio. " +
                          "Sin token o con token revocado el canal responde 401.",
            Respuestas = new Dictionary<int, string> { [200] = "Conexiones activas y tipos de mensaje.", [401] = "Sin sesión.", [403] = "Rol no permitido." }
        }
    };
}
