using Application.Interfaces;
using Domain;

namespace Community360.Tests;

/// <summary>Los mismos valores que 06_insertar_catalogos.sql.</summary>
public class CatalogoFalso : ICatalogoRepository
{
    private static ItemCatalogo I(int id, string n, bool? t = null) => new() { Id = id, Nombre = n, AdmiteTexto = t };

    private readonly Dictionary<string, IReadOnlyList<ItemCatalogo>> _d = new()
    {
        ["plataformas"] = [I(1, "Discord"), I(2, "Facebook"), I(3, "Instagram"), I(4, "Telegram"), I(5, "TikTok"), I(6, "Twitter/X")],
        ["especies"] = [I(1, "Ave"), I(2, "Conejo"), I(3, "Coyote"), I(4, "Dragón"), I(5, "Gato"), I(6, "Husky"), I(7, "Híbrido/Otro"), I(8, "Lobo"), I(9, "Perro"), I(10, "Reptil"), I(11, "Tigre"), I(12, "Zorro")],
        ["intereses"] = [I(1, "Arte digital"), I(2, "Arte tradicional"), I(3, "Convenciones"), I(4, "Cosplay"), I(5, "Cómics"), I(6, "Escritura/Historias"), I(7, "Fursuit"), I(8, "Música"), I(9, "Streaming/Contenido"), I(10, "Videojuegos")],
        ["temas"] = [I(1, "arte_comisiones"), I(2, "eventos_convenciones"), I(3, "fursuits"), I(4, "memes_humor"), I(5, "musica"), I(6, "reglas_moderacion"), I(7, "seguridad_privacidad"), I(8, "videojuegos")],
        ["tipos-interaccion"] = [I(1, "comentario", true), I(2, "mensaje", true), I(3, "reaccion", false), I(4, "respuesta_encuesta", true), I(5, "rsvp_evento", true)],
        ["dispositivos"] = [I(1, "Android"), I(2, "Escritorio"), I(3, "Web"), I(4, "iOS")],
        ["etiquetas"] = [I(1, "arte"), I(2, "aviso"), I(3, "evento"), I(4, "humor"), I(5, "juegos"), I(6, "musica"), I(7, "reglas")],
    };

    public IReadOnlyList<string> Claves => _d.Keys.ToList();
    public Task<IReadOnlyList<ItemCatalogo>?> ListarAsync(string clave, CancellationToken ct)
        => Task.FromResult(_d.TryGetValue(clave, out var l) ? l : null);
}

public class UsuarioRepoFalso : Application.Interfaces.IUsuarioRepository
{
    public Application.Interfaces.FiltroUsuarios? UltimoFiltro { get; private set; }
    public Application.Common.Utilidades.Paginacion? UltimaPagina { get; private set; }

    public static Usuario U(string id, string alias, string consent, string? ciudad = "Mérida", decimal? lat = 20.9m, decimal? lng = -89.6m) => new()
    {
        IdUsuario = id, Alias = alias, FechaIngreso = new DateTime(2026, 1, 15), ZonaHoraria = "America/Merida", RangoEdad = "18-24",
        Especie = "Lobo", Plataforma = "Discord", EsArtista = true, SentimientoPromedio = 0.5m, UltimaActividad = new DateTime(2026, 11, 1),
        Estatus = "activo", ConsentimientoDatos = consent, Ciudad = ciudad, Latitud = lat, Longitud = lng
    };

    // El usuario U000002 NO consintió pero la "base" trae ubicación (caso de fuga que la API debe tapar).
    public List<Usuario> Datos { get; } =
    [
        U("U000001", "lobo_azul", "si"),
        U("U000002", "zorro_fuga", "no"),
        U("U000003", "sin_ciudad", "si", null, null, null),
    ];

    public Task<(IReadOnlyList<Usuario> Items, int Total)> ListarAsync(Application.Interfaces.FiltroUsuarios filtro, Application.Common.Utilidades.Paginacion pag, CancellationToken ct)
    {
        UltimoFiltro = filtro; UltimaPagina = pag;
        return Task.FromResult<(IReadOnlyList<Usuario>, int)>((Datos, Datos.Count));
    }

    public Task<Usuario?> ObtenerAsync(string idUsuario, CancellationToken ct) => Task.FromResult(Datos.FirstOrDefault(d => d.IdUsuario == idUsuario));

    public Task<UsuarioBasico?> ObtenerBasicoAsync(string idUsuario, CancellationToken ct)
    {
        var u = Datos.FirstOrDefault(d => d.IdUsuario == idUsuario);
        return Task.FromResult(u is null ? null : new UsuarioBasico { IdUsuario = u.IdUsuario, ConsentimientoDatos = u.ConsentimientoDatos, IdCiudad = u.Ciudad is null ? null : 7 });
    }

    public Task<IReadOnlyList<InteresUsuario>> InteresesAsync(string idUsuario, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<InteresUsuario>>([new InteresUsuario { Nombre = "Cosplay", EsPrincipal = true }, new InteresUsuario { Nombre = "Música" }]);

    public Task<(IReadOnlyList<InteraccionLectura> Items, int Total)> ListarInteraccionesAsync(string idUsuario, Application.Common.Utilidades.Paginacion pag, CancellationToken ct)
    {
        UltimaPagina = pag;
        IReadOnlyList<InteraccionLectura> l = [new InteraccionLectura
        {
            IdInteraccion = "I0000001", IdUsuario = idUsuario, FechaHora = new DateTime(2026, 11, 2, 10, 0, 0), Plataforma = "Discord", Tipo = "comentario",
            Texto = "'; DROP TABLE x;--", Tema = "musica", Sentimiento = 0.4m, Idioma = "es", Dispositivo = "Web", LongitudTexto = 17, EtiquetasCsv = "arte,humor"
        }];
        return Task.FromResult((l, 1));
    }
}


/// <summary>Interacciones en memoria; resuelve ids de catálogo con los mismos valores de CatalogoFalso.</summary>
public class InteraccionRepoFalso : IInteraccionRepository
{
    private static readonly string[] Plat = ["", "Discord", "Facebook", "Instagram", "Telegram", "TikTok", "Twitter/X"];
    private static readonly string[] Tipos = ["", "comentario", "mensaje", "reaccion", "respuesta_encuesta", "rsvp_evento"];
    private static readonly string[] Temas = ["", "arte_comisiones", "eventos_convenciones", "fursuits", "memes_humor", "musica", "reglas_moderacion", "seguridad_privacidad", "videojuegos"];
    private static readonly string[] Disp = ["", "Android", "Escritorio", "Web", "iOS"];
    private static readonly string[] Etq = ["", "arte", "aviso", "evento", "humor", "juegos", "musica", "reglas"];

    public Dictionary<string, InteraccionLectura> Datos { get; } = new();
    public Dictionary<string, Interaccion> Ultimas { get; } = new();
    public FiltroInteracciones? UltimoFiltro { get; private set; }
    private int _n = 100;

    public Task<(IReadOnlyList<InteraccionLectura> Items, int Total)> ListarAsync(FiltroInteracciones f, Application.Common.Utilidades.Paginacion pag, CancellationToken ct)
    { UltimoFiltro = f; return Task.FromResult<(IReadOnlyList<InteraccionLectura>, int)>((Datos.Values.ToList(), Datos.Count)); }

    public Task<InteraccionLectura?> ObtenerAsync(string id, CancellationToken ct) => Task.FromResult(Datos.GetValueOrDefault(id));

    public Task<string> InsertarAsync(Interaccion i, string? idSolicitado, IReadOnlyCollection<int> etiquetas, CancellationToken ct)
    {
        var id = idSolicitado ?? $"I{++_n:D7}";
        if (Datos.ContainsKey(id)) throw Application.Common.ErrorsInteracciones.InteraccionErrors.IdRepetido();
        Ultimas[id] = i;
        Datos[id] = new InteraccionLectura
        {
            IdInteraccion = id, IdUsuario = i.IdUsuario, FechaHora = i.FechaHora, Plataforma = Plat[i.IdPlataforma], Tipo = Tipos[i.IdTipo],
            Texto = i.Texto, Tema = Temas[i.IdTema], Sentimiento = i.Sentimiento, Idioma = i.Idioma, Ciudad = i.IdCiudad is null ? null : "Mérida",
            Dispositivo = Disp[i.IdDispositivo], LongitudTexto = i.LongitudTexto, ReaccionesRecibidas = i.ReaccionesRecibidas,
            EtiquetasCsv = string.Join(',', etiquetas.Select(e => Etq[e]))
        };
        return Task.FromResult(id);
    }

    public Task<bool> ActualizarAsync(string id, InteraccionCambios c, IReadOnlyCollection<int> etiquetas, CancellationToken ct)
    {
        if (!Datos.TryGetValue(id, out var d)) return Task.FromResult(false);
        d.Texto = c.Texto; d.Tema = Temas[c.IdTema]; d.Sentimiento = c.Sentimiento; d.LongitudTexto = c.LongitudTexto;
        d.ReaccionesRecibidas = c.ReaccionesRecibidas; d.EtiquetasCsv = string.Join(',', etiquetas.Select(e => Etq[e]));
        return Task.FromResult(true);
    }

    public Task<bool> EliminarAsync(string id, CancellationToken ct) => Task.FromResult(Datos.Remove(id));
}

public class BitacoraFalsa : IBitacoraApiRepository
{
    public List<(string Operacion, string? Id, string Por, IReadOnlyDictionary<string, object?>? Detalle)> Registros { get; } = [];
    public bool Fallar { get; set; }

    public Task RegistrarAsync(string operacion, string? id, string por, IReadOnlyDictionary<string, object?>? antes,
        IReadOnlyDictionary<string, object?>? despues, IReadOnlyDictionary<string, object?>? detalle, CancellationToken ct)
    {
        if (Fallar) throw new InvalidOperationException("bitácora caída");
        Registros.Add((operacion, id, por, detalle));
        return Task.CompletedTask;
    }
}

public class AnaliticaRepoFalso : IAnaliticaRepository
{
    public DateTime? Desde { get; private set; }
    public DateTime? HastaExclusivo { get; private set; }
    public bool Vacio { get; set; }

    private IReadOnlyList<FilaAgrupada> Filas(params (string, int)[] f) => Vacio ? [] : f.Select(x => new FilaAgrupada(x.Item1, x.Item2, x.Item2)).ToList();

    public Task<Application.DTOs.Responses.ResumenResponse> ResumenAsync(CancellationToken ct)
        => Task.FromResult(Vacio ? new Application.DTOs.Responses.ResumenResponse() : new() { TotalInteracciones = 207, SentimientoPromedio = 0.123m, ReaccionesPromedio = 4.5m, PorcentajeNegativo = 30.4m });
    public Task<IReadOnlyList<FilaAgrupada>> InteraccionesPorPlataformaAsync(CancellationToken ct) => Task.FromResult(Filas(("Discord", 1), ("Instagram", 1), ("TikTok", 1)));
    public Task<IReadOnlyList<FilaAgrupada>> InteraccionesPorTipoAsync(CancellationToken ct) => Task.FromResult(Filas(("comentario", 5), ("reaccion", 2)));
    public Task<IReadOnlyList<Application.DTOs.Responses.CeldaHeatmapResponse>> ActividadPorHoraAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Application.DTOs.Responses.CeldaHeatmapResponse>>(Vacio ? [] : [new() { Dia = 1, Hora = 5, Total = 2 }]);
    public Task<IReadOnlyList<FilaAgrupada>> SentimientoPorTemaAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<FilaAgrupada>>(Vacio ? [] : [new("musica", 0.12345m, 3)]);
    public Task<IReadOnlyList<FilaAgrupada>> EtiquetasTopAsync(int limite, CancellationToken ct) => Task.FromResult(Filas(("arte", 9)));
    public Task<IReadOnlyList<FilaAgrupada>> UsuariosPorEstatusAsync(CancellationToken ct) => Task.FromResult(Filas(("activo", 2), ("en_riesgo", 1)));
    public Task<IReadOnlyList<FilaAgrupada>> UsuariosPorPlataformaAsync(CancellationToken ct) => Task.FromResult(Filas(("Discord", 4)));
    public Task<IReadOnlyList<FilaAgrupada>> UsuariosPorRangoEdadAsync(CancellationToken ct) => Task.FromResult(Filas(("18-24", 4)));
    public Task<IReadOnlyList<FilaAgrupada>> UsuariosPorPaisAsync(CancellationToken ct) => Task.FromResult(Filas(("México", 4)));
    public Task<IReadOnlyList<Application.DTOs.Responses.TendenciaDiariaResponse>> TendenciaDiariaAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    { Desde = desde; HastaExclusivo = hasta; return Task.FromResult<IReadOnlyList<Application.DTOs.Responses.TendenciaDiariaResponse>>([new() { Fecha = DateOnly.FromDateTime(desde), Total = 1, Sentimiento = 0.5m }]); }
}

public class AuditoriaRepoFalso : IAuditoriaRepository
{
    public string? UltimoEstado { get; private set; }
    public string? UltimaOperacion { get; private set; }
    public int UltimoLimite { get; private set; }
    public int UltimoOffset { get; private set; }

    private static System.Text.Json.JsonElement J(string json) => System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();

    public Task<(IReadOnlyList<System.Text.Json.JsonElement> Items, long Total)> ListarCuarentenaAsync(string? estado, int limite, int offset, CancellationToken ct)
    {
        UltimoEstado = estado; UltimoLimite = limite; UltimoOffset = offset;
        // Documento con ubicación "colada" a propósito: la API debe quitarla.
        return Task.FromResult<(IReadOnlyList<System.Text.Json.JsonElement>, long)>(([J("""{"id_interaccion":"I0000001","motivo":"x","estado_revision":"pendiente","pais":"México","ciudad":"Mérida","estado":"Yucatán","latitud":20.9,"texto":"<b>hola</b>"}""")], 793));
    }

    public Task<System.Text.Json.JsonElement?> ObtenerCuarentenaAsync(string id, CancellationToken ct)
        => Task.FromResult<System.Text.Json.JsonElement?>(id == "I0000001" ? J("""{"id_interaccion":"I0000001","motivo":"x","estado_revision":"pendiente","ciudad":"Mérida"}""") : null);

    public Task<(IReadOnlyList<System.Text.Json.JsonElement> Items, long Total)> ListarDiarioAsync(int limite, int offset, CancellationToken ct)
        => Task.FromResult<(IReadOnlyList<System.Text.Json.JsonElement>, long)>(([J("""{"tabla":"usuario","id":"U000002"}""")], 171));

    public Task<IReadOnlyList<System.Text.Json.JsonElement>> ListarBitacoraCargaAsync(int limite, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<System.Text.Json.JsonElement>>([J("""{"carga_id":"c1"}""")]);

    public Task<(IReadOnlyList<System.Text.Json.JsonElement> Items, long Total)> ListarBitacoraApiAsync(string? operacion, int limite, int offset, CancellationToken ct)
    { UltimaOperacion = operacion; return Task.FromResult<(IReadOnlyList<System.Text.Json.JsonElement>, long)>(([], 0)); }

    public Task<IReadOnlyDictionary<string, long>> ConteosAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyDictionary<string, long>>(new Dictionary<string, long> { ["cuarentena"] = 793, ["diario_cambios"] = 171, ["bitacora_carga"] = 1, ["bitacora_api"] = 0 });
}
