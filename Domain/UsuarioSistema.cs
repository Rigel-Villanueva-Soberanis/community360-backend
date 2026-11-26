namespace Domain;

/// <summary>Cuenta que usa la API (administrador o analista). No es un usuario de la comunidad.</summary>
public class UsuarioSistema
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string ContrasenaHash { get; set; } = "";
    public string Rol { get; set; } = "";      // "administrador" | "analista"
    public bool Activo { get; set; }
}
