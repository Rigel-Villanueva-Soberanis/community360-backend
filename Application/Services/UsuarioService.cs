using Application.Common.ErrorsUsuarios;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;

namespace Application.Services;

public class UsuarioService(IUsuarioRepository repo, CatalogoService catalogos, IMapper mapper)
{
    public static readonly IReadOnlyDictionary<string, string> Ordenes = new Dictionary<string, string>
    { ["alias"] = "alias", ["ingreso"] = "ingreso", ["actividad"] = "actividad", ["sentimiento"] = "sentimiento" };

    public async Task<PaginaResponse<UsuarioResponse>> ListarAsync(FiltroUsuariosRequest f, CancellationToken ct)
    {
        var pag = Paginacion.Crear(f.Pagina, f.Tamano);
        var orden = ListaBlanca.Resolver(Ordenes, f.Orden, "alias", "orden");

        int? idPlataforma = null, idEspecie = null;
        if (!string.IsNullOrWhiteSpace(f.Plataforma)) idPlataforma = (await catalogos.PlataformaAsync(f.Plataforma, "plataforma", ct)).Id;
        if (!string.IsNullOrWhiteSpace(f.Especie)) idEspecie = (await catalogos.EspecieAsync(f.Especie, "especie", ct)).Id;

        // El texto de búsqueda no se rechaza nunca: se normaliza, se escapan los comodines y viaja como parámetro.
        var buscar = TextoNormalizer.Normalizar(f.Buscar);
        var filtro = new FiltroUsuarios(idPlataforma, idEspecie, f.Estatus, f.RangoEdad, f.EsArtista,
            buscar.Length == 0 ? null : SqlLikeHelper.Contiene(buscar), orden, f.Descendente);

        var (items, total) = await repo.ListarAsync(filtro, pag, ct);
        return new PaginaResponse<UsuarioResponse>(items.Select(mapper.Map<UsuarioResponse>).ToList(), pag.Pagina, pag.Tamano, total);
    }

    public async Task<UsuarioResponse> ObtenerAsync(string? id, CancellationToken ct)
    {
        var idValido = Identificadores.Usuario(id);
        var u = await repo.ObtenerAsync(idValido, ct) ?? throw UsuarioErrors.NoExiste();
        var r = mapper.Map<UsuarioResponse>(u);
        r.Intereses = (await repo.InteresesAsync(idValido, ct)).Select(mapper.Map<InteresUsuarioResponse>).ToList();
        return r;
    }

    public async Task<PaginaResponse<InteraccionResponse>> ListarInteraccionesAsync(string? id, PaginacionRequest p, CancellationToken ct)
    {
        var idValido = Identificadores.Usuario(id);
        var pag = Paginacion.Crear(p.Pagina, p.Tamano);
        if (await repo.ObtenerAsync(idValido, ct) is null) throw UsuarioErrors.NoExiste();
        var (items, total) = await repo.ListarInteraccionesAsync(idValido, pag, ct);
        return new PaginaResponse<InteraccionResponse>(items.Select(mapper.Map<InteraccionResponse>).ToList(), pag.Pagina, pag.Tamano, total);
    }
}
