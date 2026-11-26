namespace Api.Swagger.Docs;

/// <summary>Documentación del módulo Autenticación (AuthController).</summary>
public static class AuthDocs
{
    public const string Controller = "Auth";

    public static readonly IReadOnlyDictionary<string, EndpointDoc> Entradas = new Dictionary<string, EndpointDoc>
    {
        ["Login"] = new()
        {
            Resumen = "Iniciar sesión",
            Descripcion = "Valida usuario y contraseña (BCrypt) y devuelve un token JWT. El token también se guarda en la cookie " +
                          "HttpOnly `c360_token`. Por seguridad, el mensaje es el mismo si el usuario no existe o la contraseña es incorrecta. " +
                          "Se permiten 5 intentos por minuto desde la misma IP; el sexto responde 429. Cualquier texto raro en el usuario o la contraseña se trata como texto y termina en 401.",
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Sesión iniciada; devuelve el token, su expiración, el nombre de usuario y el rol.",
                [400] = "El cuerpo no es un JSON válido o no corresponde a los tipos esperados.",
                [401] = "Usuario o contraseña incorrectos, o cuenta inactiva (mismo mensaje en todos los casos).",
                [422] = "Falta el usuario o la contraseña, o exceden el largo máximo.",
                [429] = "Demasiados intentos; espera antes de reintentar."
            },
            EjemploPeticion = new { nombreUsuario = "admin", contrasena = "********" },
            EjemploExito = new
            {
                exito = true, mensaje = (string?)null,
                datos = new { token = "eyJhbGciOiJIUzI1NiIs...", expiraUtc = "2026-11-23T22:00:00Z", nombreUsuario = "admin", rol = "administrador" },
                errores = (object?)null
            }
        },
        ["Me"] = new()
        {
            Resumen = "Usuario de la sesión actual",
            Descripcion = "Devuelve quién es el dueño del token enviado (cookie o encabezado Bearer).",
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Id, nombre de usuario y rol de la sesión.",
                [401] = "Sin token, token vencido o token cerrado con logout."
            }
        },
        ["Logout"] = new()
        {
            Resumen = "Cerrar sesión",
            Descripcion = "Revoca el token actual (queda inválido aunque no haya vencido) y borra la cookie.",
            Respuestas = new Dictionary<int, string>
            {
                [200] = "Sesión cerrada.",
                [401] = "No había una sesión válida."
            }
        }
    };
}
