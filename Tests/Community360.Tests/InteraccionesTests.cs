using System.Net;
using Xunit;

namespace Community360.Tests;

public class InteraccionesTests : ApiTestHost
{
    private static string Cuerpo(string usuario = "U000001", string tipo = "comentario", string? texto = "hola", string extra = "", string plataforma = "Instagram")
        => $$"""
        {"idUsuario":"{{usuario}}","plataforma":"{{plataforma}}","tipo":"{{tipo}}","texto":{{(texto is null ? "null" : System.Text.Json.JsonSerializer.Serialize(texto))}},
         "tema":"arte_comisiones","sentimiento":0.456,"dispositivo":"Android","etiquetas":["arte","aviso"]{{extra}}}
        """;

    [Fact]
    public async Task Crear_valida_devuelve_201_con_Location_y_calcula_campos()
    {
        var r = await Post("/api/interacciones", Cuerpo(), TokenAdmin());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.NotNull(r.Headers.Location);
        var d = (await Json(r)).GetProperty("datos");
        Assert.Equal(4, d.GetProperty("longitudTexto").GetInt32());
        Assert.Equal(0.46m, d.GetProperty("sentimiento").GetDecimal());
        Assert.Equal(2, d.GetProperty("etiquetas").GetArrayLength());
        Assert.Equal("alta", Assert.Single(BitacoraFalsa.Registros).Operacion);
    }

    [Fact]
    public async Task Reaccion_con_texto_es_422_y_se_registra_el_rechazo()
    {
        var r = await Post("/api/interacciones", Cuerpo(tipo: "reaccion", texto: "algo"), TokenAdmin());
        Assert.Equal((HttpStatusCode)422, r.StatusCode);
        Assert.Equal("rechazo", Assert.Single(BitacoraFalsa.Registros).Operacion);
    }

    [Fact]
    public async Task Reaccion_sin_texto_es_valida() =>
        Assert.Equal(HttpStatusCode.Created, (await Post("/api/interacciones", Cuerpo(tipo: "reaccion", texto: null), TokenAdmin())).StatusCode);

    [Fact]
    public async Task Comentario_sin_texto_es_422() =>
        Assert.Equal((HttpStatusCode)422, (await Post("/api/interacciones", Cuerpo(texto: null), TokenAdmin())).StatusCode);

    [Fact]
    public async Task Usuario_inexistente_es_404() =>
        Assert.Equal(HttpStatusCode.NotFound, (await Post("/api/interacciones", Cuerpo(usuario: "U999999"), TokenAdmin())).StatusCode);

    [Fact]
    public async Task Usuario_sin_consentimiento_guarda_ciudad_nula()
    {
        var r = await Post("/api/interacciones", Cuerpo(usuario: "U000002"), TokenAdmin());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Null(Assert.Single(InteraccionesFalsas.Ultimas.Values).IdCiudad);
    }

    [Fact]
    public async Task Usuario_con_consentimiento_guarda_su_ciudad()
    {
        await Post("/api/interacciones", Cuerpo(usuario: "U000001"), TokenAdmin());
        Assert.NotNull(Assert.Single(InteraccionesFalsas.Ultimas.Values).IdCiudad);
    }

    [Fact]
    public async Task Sentimiento_fuera_de_rango_es_422() =>
        Assert.Equal((HttpStatusCode)422, (await Post("/api/interacciones", Cuerpo().Replace("0.456", "5"), TokenAdmin())).StatusCode);

    [Fact]
    public async Task Fecha_futura_es_422() =>
        Assert.Equal((HttpStatusCode)422, (await Post("/api/interacciones", Cuerpo(extra: ",\"fechaHora\":\"2999-01-01T00:00:00Z\""), TokenAdmin())).StatusCode);

    [Fact]
    public async Task Etiquetas_repetidas_es_422() =>
        Assert.Equal((HttpStatusCode)422, (await Post("/api/interacciones", Cuerpo().Replace("[\"arte\",\"aviso\"]", "[\"arte\",\"ARTE\"]"), TokenAdmin())).StatusCode);

    [Fact]
    public async Task Plataforma_con_inyeccion_es_422() =>
        Assert.Equal((HttpStatusCode)422, (await Post("/api/interacciones", Cuerpo(plataforma: "Instagram' OR '1'='1"), TokenAdmin())).StatusCode);

    [Fact]
    public async Task Texto_con_inyeccion_se_guarda_literal()
    {
        const string t = "'; DROP TABLE interaccion; -- {\"$ne\":null} <script>alert(1)</script>";
        var r = await Post("/api/interacciones", Cuerpo(texto: t), TokenAdmin());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(t, Assert.Single(InteraccionesFalsas.Datos.Values).Texto);
    }

    [Fact]
    public async Task Texto_de_mas_de_500_es_422() =>
        Assert.Equal((HttpStatusCode)422, (await Post("/api/interacciones", Cuerpo(texto: new string('a', 501)), TokenAdmin())).StatusCode);

    [Fact]
    public async Task Operador_nosql_en_idUsuario_es_rechazado()
    {
        var r = await Post("/api/interacciones", Cuerpo().Replace("\"U000001\"", "{\"$ne\":null}"), TokenAdmin());
        Assert.True(r.StatusCode is HttpStatusCode.BadRequest or (HttpStatusCode)422);
    }

    [Fact]
    public async Task Analista_no_puede_crear_modificar_ni_borrar()
    {
        var t = TokenAnalista();
        Assert.Equal(HttpStatusCode.Forbidden, (await Post("/api/interacciones", Cuerpo(), t)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Put("/api/interacciones/I0000001", "{}", t)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Delete("/api/interacciones/I0000001", t)).StatusCode);
    }

    [Fact]
    public async Task Sin_token_es_401() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/interacciones")).StatusCode);

    [Fact]
    public async Task Analista_puede_listar_y_consultar()
    {
        await Post("/api/interacciones", Cuerpo(), TokenAdmin());
        var id = InteraccionesFalsas.Datos.Keys.Single();
        Assert.Equal(HttpStatusCode.OK, (await Get("/api/interacciones", TokenAnalista())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get($"/api/interacciones/{id}", TokenAnalista())).StatusCode);
    }

    [Fact]
    public async Task Obtener_inexistente_404_y_id_invalido_422()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Get("/api/interacciones/I0999999", TokenAdmin())).StatusCode);
        Assert.Equal((HttpStatusCode)422, (await Get("/api/interacciones/abc", TokenAdmin())).StatusCode);
    }

    [Fact]
    public async Task Listar_con_orden_desconocido_o_tamano_enorme_es_422()
    {
        Assert.Equal((HttpStatusCode)422, (await Get("/api/interacciones?orden=fecha;DROP", TokenAdmin())).StatusCode);
        Assert.Equal((HttpStatusCode)422, (await Get("/api/interacciones?tamano=100000", TokenAdmin())).StatusCode);
    }

    [Fact]
    public async Task Listar_traduce_filtros_a_ids()
    {
        var r = await Get("/api/interacciones?plataforma=instagram&tipo=comentario&etiqueta=arte&orden=reacciones&descendente=false", TokenAdmin());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var f = InteraccionesFalsas.UltimoFiltro!;
        Assert.Equal(3, f.IdPlataforma); Assert.Equal(1, f.IdTipo); Assert.Equal(1, f.IdEtiqueta);
        Assert.Equal("reacciones", f.Orden); Assert.False(f.Descendente);
    }

    [Fact]
    public async Task Actualizar_reemplaza_etiquetas_y_registra_modificacion()
    {
        await Post("/api/interacciones", Cuerpo(), TokenAdmin());
        var id = InteraccionesFalsas.Datos.Keys.Single();
        var r = await Put($"/api/interacciones/{id}", """{"texto":"nuevo","tema":"musica","sentimiento":-0.5,"reaccionesRecibidas":3,"etiquetas":["musica"]}""", TokenAdmin());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = (await Json(r)).GetProperty("datos");
        Assert.Equal("musica", d.GetProperty("tema").GetString());
        Assert.Equal(5, d.GetProperty("longitudTexto").GetInt32());
        Assert.Single(d.GetProperty("etiquetas").EnumerateArray());
        Assert.Contains(BitacoraFalsa.Registros, x => x.Operacion == "modificacion");
    }

    [Fact]
    public async Task Actualizar_inexistente_404() =>
        Assert.Equal(HttpStatusCode.NotFound, (await Put("/api/interacciones/I0999999", """{"texto":"x","tema":"musica","sentimiento":0,"etiquetas":[]}""", TokenAdmin())).StatusCode);

    [Fact]
    public async Task Eliminar_funciona_y_segunda_vez_404()
    {
        await Post("/api/interacciones", Cuerpo(), TokenAdmin());
        var id = InteraccionesFalsas.Datos.Keys.Single();
        Assert.Equal(HttpStatusCode.OK, (await Delete($"/api/interacciones/{id}", TokenAdmin())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Delete($"/api/interacciones/{id}", TokenAdmin())).StatusCode);
        Assert.Contains(BitacoraFalsa.Registros, x => x.Operacion == "baja");
    }

    [Fact]
    public async Task Si_la_bitacora_falla_la_operacion_no_se_deshace()
    {
        BitacoraFalsa.Fallar = true;
        Assert.Equal(HttpStatusCode.Created, (await Post("/api/interacciones", Cuerpo(), TokenAdmin())).StatusCode);
        Assert.Single(InteraccionesFalsas.Datos);
    }

    [Fact]
    public async Task Id_repetido_es_409()
    {
        var c = Cuerpo(extra: ",\"idInteraccion\":\"I0000500\"");
        Assert.Equal(HttpStatusCode.Created, (await Post("/api/interacciones", c, TokenAdmin())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post("/api/interacciones", c, TokenAdmin())).StatusCode);
    }
}

/// <summary>Contra la base real de pruebas (C360_TEST_MYSQL). Datos con prefijo U91/I91; se limpian antes y después.</summary>
[Collection("bd")]
public class InteraccionesBdTests : IAsyncLifetime
{
    private readonly Infrastructure.Repositories.InteraccionRepository _repo = new(BdPrueba.Factory());

    public async Task InitializeAsync()
    {
        if (!BdPrueba.Disponible) return;
        await Limpiar();
        await BdPrueba.InsertarUsuarioAsync("U910001", "bd_lobo");
    }
    public async Task DisposeAsync() { if (BdPrueba.Disponible) await Limpiar(); }
    private static async Task Limpiar()
    {
        await BdPrueba.EjecutarAsync("DELETE FROM interaccion_etiqueta WHERE id_interaccion LIKE 'I91%'");
        await BdPrueba.LimpiarAsync("U91");
    }

    private static Domain.Interaccion Nueva(string texto = "hola 😀 '; DROP TABLE x; --") => new()
    {
        IdUsuario = "U910001", FechaHora = new DateTime(2026, 11, 1, 12, 0, 0), IdPlataforma = 3, IdTipo = 1, Texto = texto, IdTema = 1,
        Sentimiento = 0.46m, Idioma = "es", IdCiudad = 1, IdDispositivo = 1, LongitudTexto = texto.EnumerateRunes().Count()
    };

    [FactBd]
    public async Task Insertar_con_id_solicitado_guarda_texto_literal_y_etiquetas()
    {
        var id = await _repo.InsertarAsync(Nueva(), "I910001", [1, 2], default);
        Assert.Equal("I910001", id);
        var l = await _repo.ObtenerAsync(id, default);
        Assert.Equal("hola 😀 '; DROP TABLE x; --", l!.Texto);
        Assert.Equal("Instagram", l.Plataforma);
        Assert.Equal(2, l.EtiquetasCsv!.Split(',').Length);
    }

    [FactBd]
    public async Task Id_repetido_lanza_409()
    {
        await _repo.InsertarAsync(Nueva(), "I910002", [], default);
        var ex = await Assert.ThrowsAsync<Application.Common.ErrorCatalog.DomainException>(() => _repo.InsertarAsync(Nueva(), "I910002", [], default));
        Assert.Equal(409, ex.StatusCode);
    }

    [FactBd]
    public async Task Insertar_con_id_generado_sigue_la_secuencia()
    {
        var a = await _repo.InsertarAsync(Nueva(), null, [], default);
        var b = await _repo.InsertarAsync(Nueva(), null, [], default);
        Assert.Matches(@"^I\d{7}$", a);
        Assert.Equal(long.Parse(a[1..]) + 1, long.Parse(b[1..]));
        await BdPrueba.EjecutarAsync("DELETE FROM interaccion WHERE id_interaccion IN (@A, @B)", new { A = a, B = b });
    }

    [FactBd]
    public async Task Actualizar_reemplaza_etiquetas_y_Eliminar_borra_todo()
    {
        await _repo.InsertarAsync(Nueva(), "I910003", [1, 2], default);
        Assert.True(await _repo.ActualizarAsync("I910003", new Domain.InteraccionCambios { Texto = "nuevo", IdTema = 5, Sentimiento = -0.5m, LongitudTexto = 5, ReaccionesRecibidas = 9 }, [3], default));
        var l = await _repo.ObtenerAsync("I910003", default);
        Assert.Equal("musica", l!.Tema); Assert.Equal("evento", l.EtiquetasCsv); Assert.Equal(9, l.ReaccionesRecibidas);

        Assert.True(await _repo.EliminarAsync("I910003", default));
        Assert.Null(await _repo.ObtenerAsync("I910003", default));
        Assert.Equal(0, await BdPrueba.EscalarAsync<int>("SELECT COUNT(*) FROM interaccion_etiqueta WHERE id_interaccion = 'I910003'"));
        Assert.False(await _repo.EliminarAsync("I910003", default));
        Assert.False(await _repo.ActualizarAsync("I910003", new Domain.InteraccionCambios { IdTema = 1 }, [], default));
    }

    [FactBd]
    public async Task Listar_filtra_pagina_y_ordena_con_lista_blanca()
    {
        await _repo.InsertarAsync(Nueva("uno"), "I910010", [1], default);
        var otra = Nueva("dos"); otra.Sentimiento = 0.9m; otra.IdPlataforma = 1;
        await _repo.InsertarAsync(otra, "I910011", [], default);
        var f = new Application.Interfaces.FiltroInteracciones("U910001", 3, null, null, 1, null, null, "sentimiento", true);
        var (items, total) = await _repo.ListarAsync(f, Application.Common.Utilidades.Paginacion.Crear(1, 10), default);
        Assert.Equal(1, total); Assert.Equal("I910010", Assert.Single(items).IdInteraccion);
        var (todos, t2) = await _repo.ListarAsync(f with { IdPlataforma = null, IdEtiqueta = null }, Application.Common.Utilidades.Paginacion.Crear(1, 10), default);
        Assert.Equal(2, t2); Assert.Equal("I910011", todos[0].IdInteraccion);
    }
}
