namespace Application.DTOs.Responses;

public record LoginResponse(string Token, DateTime ExpiraUtc, string NombreUsuario, string Rol);
