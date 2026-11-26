using Application.Common.ErrorCatalog;
using Application.Common.ErrorsInteracciones;

namespace Application.Validators;

/// <summary>
/// Reglas de negocio de una interacción (las mismas que aplicó el ETL). No toca la base de datos:
/// recibe lo que el servicio ya consultó y devuelve la lista de errores por campo.
/// </summary>
public static class InteraccionValidator
{
    public static readonly TimeSpan TolerianciaReloj = TimeSpan.FromMinutes(5);

    /// <param name="admiteTexto">Valor del tipo de interacción; null si el tipo no se pudo resolver (ya hay otro error).</param>
    /// <param name="texto">Texto ya normalizado ("" si no hay).</param>
    public static List<ErrorDetalle> ValidarTexto(bool? admiteTexto, string texto)
    {
        var e = new List<ErrorDetalle>();
        if (admiteTexto == false && texto.Length > 0) e.Add(new("texto", InteraccionErrors.ReaccionSinTexto));      // regla 3
        if (admiteTexto == true && texto.Length == 0) e.Add(new("texto", InteraccionErrors.TextoObligatorio));      // regla 4
        return e;
    }

    public static List<ErrorDetalle> ValidarFecha(DateTime fechaUtc, DateTime ahoraUtc)
        => fechaUtc > ahoraUtc + TolerianciaReloj ? [new("fechaHora", InteraccionErrors.FechaFutura)] : [];          // regla 6

    public static List<ErrorDetalle> ValidarSentimiento(decimal sentimiento)
        => sentimiento is < -1m or > 1m ? [new("sentimiento", InteraccionErrors.SentimientoFueraDeRango)] : [];      // regla 5

    public static List<ErrorDetalle> ValidarEtiquetasSinRepetir(IEnumerable<string> normalizadas)                    // regla 7
    {
        var lista = normalizadas.Where(x => x.Length > 0).ToList();
        return lista.Count != lista.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            ? [new("etiquetas", InteraccionErrors.EtiquetaRepetida)] : [];
    }
}
