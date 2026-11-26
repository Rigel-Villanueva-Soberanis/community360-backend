namespace Application.DTOs.Responses;

public record PaginaResponse<T>(IReadOnlyList<T> Items, int Pagina, int Tamano, int Total)
{
    public int TotalPaginas => Tamano == 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamano);
}
