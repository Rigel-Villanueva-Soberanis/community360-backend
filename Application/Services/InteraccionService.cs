using Application.Common.ErrorCatalog;
using Application.Common.ErrorsInteracciones;
using Application.Common.ErrorsUsuarios;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Validators;
using AutoMapper;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class InteraccionService(
    IInteraccionRepository repo, IUsuarioRepository usuarios, CatalogoService catalogos,
    IBitacoraApiRepository bitacora, IRealtimeNotifier notifier, IMapper mapper, ILogger<InteraccionService> log)
{
    public const int MaxTexto = 500;

    public static readonly IReadOnlyDictionary<string, string> Ordenes = new Dictionary<string, string>
    { ["fecha"] = "fecha", ["sentimiento"] = "sentimiento", ["reacciones"] = "reacciones" };

    // ---------------------------------------------------------------- lectura

    public async Task<PaginaResponse<InteraccionResponse>> ListarAsync(FiltroInteraccionesRequest f, CancellationToken ct)
    {
        var pag = Paginacion.Crear(f.Pagina, f.Tamano);
        var orden = ListaBlanca.Resolver(Ordenes, f.Orden, "fecha", "orden");

        string? idUsuario = string.IsNullOrWhiteSpace(f.IdUsuario) ? null : Identificadores.Usuario(f.IdUsuario.Trim(), "idUsuario");
        var errores = new List<ErrorDetalle>();
        int? idPlataforma = null, idTipo = null, idTema = null, idEtiqueta = null;
        await Capturar(errores, async () => { if (Hay(f.Plataforma)) idPlataforma = (await catalogos.PlataformaAsync(f.Plataforma, "plataforma", ct)).Id; });
        await Capturar(errores, async () => { if (Hay(f.Tipo)) idTipo = (await catalogos.TipoAsync(f.Tipo, "tipo", ct)).Id; });
        await Capturar(errores, async () => { if (Hay(f.Tema)) idTema = (await catalogos.TemaAsync(f.Tema, "tema", ct)).Id; });
        await Capturar(errores, async () =>
        {
            if (Hay(f.Etiqueta)) idEtiqueta = (await catalogos.EtiquetasAsync([f.Etiqueta!], "etiqueta", ct)).Single().Id;
        });

        var desde = f.Desde is null ? (DateTime?)null : AUtc(f.Desde.Value);
        var hasta = f.Hasta is null ? (DateTime?)null : AUtc(f.Hasta.Value);
        if (desde > hasta) errores.Add(new ErrorDetalle("desde", "La fecha inicial no puede ser posterior a la final."));
        if (errores.Count > 0) throw ApiErrors.Validation(errores);

        var filtro = new FiltroInteracciones(idUsuario, idPlataforma, idTipo, idTema, idEtiqueta, desde, hasta, orden, f.Descendente ?? true);
        var (items, total) = await repo.ListarAsync(filtro, pag, ct);
        return new PaginaResponse<InteraccionResponse>(items.Select(mapper.Map<InteraccionResponse>).ToList(), pag.Pagina, pag.Tamano, total);
    }

    public async Task<InteraccionResponse> ObtenerAsync(string? id, CancellationToken ct)
    {
        var idValido = Identificadores.Interaccion(id);
        var i = await repo.ObtenerAsync(idValido, ct) ?? throw InteraccionErrors.NoExiste();
        return mapper.Map<InteraccionResponse>(i);
    }

    // ---------------------------------------------------------------- alta

    public async Task<InteraccionResponse> CrearAsync(CreateInteraccionRequest req, string porUsuario, CancellationToken ct)
    {
        try
        {
            var idSolicitado = Hay(req.IdInteraccion) ? Identificadores.Interaccion(req.IdInteraccion!.Trim(), "idInteraccion") : null;
            var idUsuario = Identificadores.Usuario(req.IdUsuario?.Trim(), "idUsuario");
            var usuario = await usuarios.ObtenerBasicoAsync(idUsuario, ct) ?? throw UsuarioErrors.NoExiste();      // regla 1 → 404

            // Se juntan todos los errores de datos para devolverlos juntos en un solo 422.
            var errores = new List<ErrorDetalle>();
            ItemCatalogo? plataforma = null, tipo = null, tema = null, dispositivo = null;
            IReadOnlyList<ItemCatalogo> etiquetas = [];
            await Capturar(errores, async () => plataforma = await catalogos.PlataformaAsync(req.Plataforma, "plataforma", ct));   // regla 2
            await Capturar(errores, async () => tipo = await catalogos.TipoAsync(req.Tipo, "tipo", ct));
            await Capturar(errores, async () => tema = await catalogos.TemaAsync(req.Tema, "tema", ct));
            await Capturar(errores, async () => dispositivo = await catalogos.DispositivoAsync(req.Dispositivo, "dispositivo", ct));
            errores.AddRange(InteraccionValidator.ValidarEtiquetasSinRepetir(req.Etiquetas.Select(e => TextoNormalizer.Normalizar(e))));
            await Capturar(errores, async () => etiquetas = await catalogos.EtiquetasAsync(req.Etiquetas, "etiquetas", ct));

            var texto = "";
            await Capturar(errores, () => { texto = NoSqlSanitizer.Limpiar(req.Texto, "texto", MaxTexto); return Task.CompletedTask; });
            errores.AddRange(InteraccionValidator.ValidarTexto(tipo?.AdmiteTexto, texto));
            var fecha = req.FechaHora is null ? DateTime.UtcNow : AUtc(req.FechaHora.Value);
            errores.AddRange(InteraccionValidator.ValidarFecha(fecha, DateTime.UtcNow));
            var sentimiento = req.Sentimiento ?? 0m;
            errores.AddRange(InteraccionValidator.ValidarSentimiento(sentimiento));
            if (errores.Count > 0) throw ApiErrors.Validation(errores);

            var entidad = new Interaccion
            {
                IdUsuario = usuario.IdUsuario,
                FechaHora = SinKind(fecha),
                IdPlataforma = plataforma!.Id, IdTipo = tipo!.Id, IdTema = tema!.Id, IdDispositivo = dispositivo!.Id,
                Texto = texto.Length > 0 ? texto : null,
                Sentimiento = Math.Round(sentimiento, 2),
                Idioma = req.Idioma,
                LongitudTexto = texto.EnumerateRunes().Count(),                                          // regla 8: la calcula la API
                ReaccionesRecibidas = req.ReaccionesRecibidas,
                IdCiudad = usuario.ConsentimientoDatos == "si" ? usuario.IdCiudad : null               // regla 9: privacidad
            };

            var id = await repo.InsertarAsync(entidad, idSolicitado, etiquetas.Select(e => e.Id).ToList(), ct);
            var creada = mapper.Map<InteraccionResponse>(await repo.ObtenerAsync(id, ct));

            await RegistrarAsync("alta", id, porUsuario, null, Snapshot(creada), null, ct);
            await NotificarAsync(() => notifier.InteraccionCreadaAsync(creada, ct));
            return creada;
        }
        catch (DomainException ex) when (ex.StatusCode is 404 or 409 or 422)
        {
            await RegistrarRechazoAsync(req.IdUsuario, porUsuario, ex, ct);
            throw;
        }
    }

    // ---------------------------------------------------------------- modificación

    public async Task<InteraccionResponse> ActualizarAsync(string? id, UpdateInteraccionRequest req, string porUsuario, CancellationToken ct)
    {
        try
        {
            var idValido = Identificadores.Interaccion(id);
            var actual = await repo.ObtenerAsync(idValido, ct) ?? throw InteraccionErrors.NoExiste();
            var antes = mapper.Map<InteraccionResponse>(actual);

            var errores = new List<ErrorDetalle>();
            ItemCatalogo? tipo = null, tema = null;
            IReadOnlyList<ItemCatalogo> etiquetas = [];
            await Capturar(errores, async () => tipo = await catalogos.TipoAsync(actual.Tipo, "tipo", ct));
            await Capturar(errores, async () => tema = await catalogos.TemaAsync(req.Tema, "tema", ct));
            errores.AddRange(InteraccionValidator.ValidarEtiquetasSinRepetir(req.Etiquetas.Select(e => TextoNormalizer.Normalizar(e))));
            await Capturar(errores, async () => etiquetas = await catalogos.EtiquetasAsync(req.Etiquetas, "etiquetas", ct));
            var texto = "";
            await Capturar(errores, () => { texto = NoSqlSanitizer.Limpiar(req.Texto, "texto", MaxTexto); return Task.CompletedTask; });
            errores.AddRange(InteraccionValidator.ValidarTexto(tipo?.AdmiteTexto, texto));
            var sentimiento = req.Sentimiento ?? 0m;
            errores.AddRange(InteraccionValidator.ValidarSentimiento(sentimiento));
            if (errores.Count > 0) throw ApiErrors.Validation(errores);

            var cambios = new InteraccionCambios
            {
                Texto = texto.Length > 0 ? texto : null, IdTema = tema!.Id, Sentimiento = Math.Round(sentimiento, 2),
                LongitudTexto = texto.EnumerateRunes().Count(), ReaccionesRecibidas = req.ReaccionesRecibidas
            };
            if (!await repo.ActualizarAsync(idValido, cambios, etiquetas.Select(e => e.Id).ToList(), ct)) throw InteraccionErrors.NoExiste();

            var despues = mapper.Map<InteraccionResponse>(await repo.ObtenerAsync(idValido, ct));
            await RegistrarAsync("modificacion", idValido, porUsuario, Snapshot(antes), Snapshot(despues), null, ct);
            await NotificarAsync(() => notifier.InteraccionModificadaAsync(despues, ct));
            return despues;
        }
        catch (DomainException ex) when (ex.StatusCode is 404 or 422)
        {
            await RegistrarRechazoAsync(null, porUsuario, ex, ct, id);
            throw;
        }
    }

    // ---------------------------------------------------------------- baja

    public async Task<InteraccionResponse> EliminarAsync(string? id, string porUsuario, CancellationToken ct)
    {
        try
        {
            var idValido = Identificadores.Interaccion(id);
            var actual = await repo.ObtenerAsync(idValido, ct) ?? throw InteraccionErrors.NoExiste();
            var eliminada = mapper.Map<InteraccionResponse>(actual);
            if (!await repo.EliminarAsync(idValido, ct)) throw InteraccionErrors.NoExiste();

            // "antes" guarda el registro completo: la baja queda respaldada en la bitácora.
            await RegistrarAsync("baja", idValido, porUsuario, Snapshot(eliminada), null, null, ct);
            await NotificarAsync(() => notifier.InteraccionEliminadaAsync(eliminada, ct));
            return eliminada;
        }
        catch (DomainException ex) when (ex.StatusCode is 404 or 422)
        {
            await RegistrarRechazoAsync(null, porUsuario, ex, ct, id);
            throw;
        }
    }

    // ---------------------------------------------------------------- apoyo

    private static bool Hay(string? s) => !string.IsNullOrWhiteSpace(s);

    /// <summary>Fecha a UTC; sin zona se asume UTC.</summary>
    private static DateTime AUtc(DateTime d) => d.Kind == DateTimeKind.Local ? d.ToUniversalTime() : DateTime.SpecifyKind(d, DateTimeKind.Utc);
    private static DateTime SinKind(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Unspecified);

    /// <summary>Ejecuta un paso y, si falla con 422, guarda sus errores en vez de cortar (así se devuelven todos juntos).</summary>
    private static async Task Capturar(List<ErrorDetalle> errores, Func<Task> paso)
    {
        try { await paso(); }
        catch (DomainException ex) when (ex.StatusCode == 422 && ex.Errores is not null) { errores.AddRange(ex.Errores); }
    }

    private static Dictionary<string, object?> Snapshot(InteraccionResponse i) => new()
    {
        ["id_interaccion"] = i.IdInteraccion, ["id_usuario"] = i.IdUsuario, ["fecha_hora_utc"] = i.FechaHoraUtc.ToString("O"),
        ["plataforma"] = i.Plataforma, ["tipo"] = i.Tipo, ["texto"] = i.Texto, ["tema"] = i.Tema, ["sentimiento"] = i.Sentimiento,
        ["idioma"] = i.Idioma, ["ciudad"] = i.Ciudad, ["dispositivo"] = i.Dispositivo, ["longitud_texto"] = i.LongitudTexto,
        ["reacciones_recibidas"] = i.ReaccionesRecibidas, ["etiquetas"] = i.Etiquetas.ToList()
    };

    // La base de datos es la fuente de verdad: si la bitácora o el aviso en vivo fallan, la operación NO se deshace;
    // queda una advertencia en el log (sin datos del cliente).
    private async Task RegistrarAsync(string operacion, string? id, string por,
        IReadOnlyDictionary<string, object?>? antes, IReadOnlyDictionary<string, object?>? despues,
        IReadOnlyDictionary<string, object?>? detalle, CancellationToken ct)
    {
        try { await bitacora.RegistrarAsync(operacion, id, por, antes, despues, detalle, ct); }
        catch (Exception ex) { log.LogWarning("No se pudo escribir la bitácora ({Operacion}): {Tipo}", operacion, ex.GetType().Name); }
    }

    private async Task NotificarAsync(Func<Task> aviso)
    {
        try { await aviso(); }
        catch (Exception ex) { log.LogWarning("No se pudo avisar por tiempo real: {Tipo}", ex.GetType().Name); }
    }

    /// <summary>Deja constancia del rechazo: código, motivo y campos, sin guardar el texto que mandó el cliente.</summary>
    private Task RegistrarRechazoAsync(string? idUsuario, string por, DomainException ex, CancellationToken ct, string? idInteraccion = null)
    {
        var detalle = new Dictionary<string, object?>
        {
            ["http"] = ex.StatusCode, ["codigo"] = ex.Codigo, ["motivo"] = ex.Message,
            ["campos"] = ex.Errores?.Select(e => e.Campo).ToList(),
            ["id_usuario"] = idUsuario is not null && Identificadores.UsuarioValido(idUsuario.Trim()) ? idUsuario.Trim() : null
        };
        var id = idInteraccion is not null && Identificadores.InteraccionValida(idInteraccion) ? idInteraccion : null;
        return RegistrarAsync("rechazo", id, por, null, null, detalle, ct);
    }
}
