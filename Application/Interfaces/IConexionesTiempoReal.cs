namespace Application.Interfaces;

/// <summary>Estado del canal en vivo (lo consulta TiempoRealController sin conocer WebSockets).</summary>
public interface IConexionesTiempoReal
{
    int Conectados { get; }
}
