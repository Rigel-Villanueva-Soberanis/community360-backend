using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Solo se valida que existan y no sean enormes. Lo demás (comillas, operadores, etc.) se trata como texto:
/// si no coincide con una cuenta, la respuesta es 401, igual que cualquier credencial incorrecta.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio."), StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
    public string NombreUsuario { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria."), StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Contrasena { get; set; } = "";
}
