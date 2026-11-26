using Domain;

namespace Application.Interfaces;

public interface IJwtGenerator
{
    (string Token, DateTime ExpiraUtc) Crear(UsuarioSistema usuario);
}
