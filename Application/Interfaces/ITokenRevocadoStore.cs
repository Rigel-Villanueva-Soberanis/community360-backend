namespace Application.Interfaces;

public interface ITokenRevocadoStore
{
    void Revocar(string jti, DateTime expiraUtc);
    bool EstaRevocado(string jti);
}
