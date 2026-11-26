using Application.Common.ErrorCatalog;

namespace Application.Common.Utilidades;

/// <summary>Paginación acotada. LIMIT y OFFSET salen de aquí y se pasan como parámetros.</summary>
public readonly record struct Paginacion(int Pagina, int Tamano)
{
    public const int PaginaMaxima = 100_000;
    public int Offset => (Pagina - 1) * Tamano;

    public static Paginacion Crear(int? pagina, int? tamano, int porDefecto = 25, int maximo = 100)
    {
        var p = pagina ?? 1;
        var t = tamano ?? porDefecto;
        var errores = new List<ErrorDetalle>();
        if (p < 1 || p > PaginaMaxima) errores.Add(new ErrorDetalle("pagina", $"Debe estar entre 1 y {PaginaMaxima}."));
        if (t < 1 || t > maximo) errores.Add(new ErrorDetalle("tamano", $"Debe estar entre 1 y {maximo}."));
        if (errores.Count > 0) throw ApiErrors.Validation(errores);
        return new Paginacion(p, t);
    }
}
