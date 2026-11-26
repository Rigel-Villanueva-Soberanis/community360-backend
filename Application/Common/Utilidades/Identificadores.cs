using System.Text.RegularExpressions;
using Application.Common.ErrorCatalog;

namespace Application.Common.Utilidades;

/// <summary>Los ids tienen formato fijo; lo que no lo cumple se rechaza con 422 antes de tocar una base de datos.</summary>
public static partial class Identificadores
{
    [GeneratedRegex(@"^U\d{6}\z")] private static partial Regex UsuarioRx();
    [GeneratedRegex(@"^I\d{7}\z")] private static partial Regex InteraccionRx();

    public static bool UsuarioValido(string? id) => id is not null && UsuarioRx().IsMatch(id);
    public static bool InteraccionValida(string? id) => id is not null && InteraccionRx().IsMatch(id);

    public static string Usuario(string? id, string campo = "id")
        => UsuarioValido(id) ? id! : throw ApiErrors.Validation([new ErrorDetalle(campo, "Formato esperado: U000000.")]);

    public static string Interaccion(string? id, string campo = "id")
        => InteraccionValida(id) ? id! : throw ApiErrors.Validation([new ErrorDetalle(campo, "Formato esperado: I0000000.")]);
}
