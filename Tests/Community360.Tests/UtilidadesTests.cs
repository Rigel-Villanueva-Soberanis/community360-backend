using Application.Common.ErrorCatalog;
using Application.Common.Utilidades;
using Xunit;

namespace Community360.Tests;

public class TextoNormalizerTests
{
    [Theory]
    [InlineData("'; DROP TABLE interaccion;--")]
    [InlineData("{\"$ne\": null}")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("a' OR 1=1 --")]
    [InlineData("db.users.find({$where: 'sleep(1000)'})")]
    public void NoPierdeLoQueEscribioElUsuario(string texto)
        => Assert.Equal(texto, TextoNormalizer.Normalizar(texto));

    [Fact]
    public void QuitaControlYNul()
        => Assert.Equal("holamundo", TextoNormalizer.Normalizar("hola\0mun\u0007do"));

    [Fact]
    public void ConservaSaltosDeLineaYTabulaciones()
        => Assert.Equal("a\nb\tc", TextoNormalizer.Normalizar("a\r\nb\tc"));

    [Fact]
    public void QuitaCaracteresBidiYCeroAncho()
        => Assert.Equal("admin", TextoNormalizer.Normalizar("ad‮min​﻿"));

    [Fact]
    public void ConservaEmojisCompuestos()
    {
        const string familia = "👨‍👩‍👧";
        Assert.Equal(familia, TextoNormalizer.Normalizar(familia));
    }

    [Fact]
    public void QuitaSustitutosSueltos()
        => Assert.Equal("ab", TextoNormalizer.Normalizar("a\uD800b"));

    [Fact]
    public void NormalizaAFormC()
        => Assert.Equal("é", TextoNormalizer.Normalizar("é"));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("   \n\t ")]
    public void VacioDevuelveVacio(string? s) => Assert.Equal("", TextoNormalizer.Normalizar(s));
}

public class NoSqlSanitizerTests
{
    [Fact]
    public void LimpiarNoRechazaPatronesDeInyeccion()
    {
        const string t = "'; DROP TABLE x;-- /* */ $where {\"$ne\":null}";
        Assert.Equal(t, NoSqlSanitizer.Limpiar(t, "texto", 500));
    }

    [Fact]
    public void LimpiarRechazaSoloPorLargo()
    {
        var ex = Assert.Throws<DomainException>(() => NoSqlSanitizer.Limpiar(new string('a', 501), "texto", 500));
        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("texto", ex.Errores![0].Campo);
    }

    [Fact]
    public void ElLargoSeCuentaEnCaracteresNoEnUnidadesUtf16()
    {
        var quinientos = string.Concat(Enumerable.Repeat("😀", 500));   // 500 caracteres = 1000 unidades UTF-16
        Assert.Equal(quinientos, NoSqlSanitizer.Limpiar(quinientos, "texto", 500));
        Assert.Throws<DomainException>(() => NoSqlSanitizer.Limpiar(quinientos + "😀", "texto", 500));
    }

    [Fact]
    public void LimpiarOpcionalDevuelveNullSiQuedaVacio()
        => Assert.Null(NoSqlSanitizer.LimpiarOpcional("  ​ ", "texto", 10));

    [Theory]
    [InlineData("I0000001", true)] [InlineData("cuarentena:I0000001", true)] [InlineData("a-b_c", true)]
    [InlineData("", false)] [InlineData(null, false)] [InlineData("a b", false)] [InlineData("x'--", false)]
    [InlineData("I0000001\n", false)] [InlineData("`b`.`_default`", false)] [InlineData("{\"$ne\":1}", false)]
    public void ClaveValida(string? clave, bool esperado) => Assert.Equal(esperado, NoSqlSanitizer.ClaveValida(clave));

    [Fact]
    public void ClaveDemasiadoLargaNoEsValida() => Assert.False(NoSqlSanitizer.ClaveValida(new string('a', 81)));

    [Fact]
    public void ValidarClaveLanza422()
        => Assert.Equal(422, Assert.Throws<DomainException>(() => NoSqlSanitizer.ValidarClave("x y", "id")).StatusCode);
}

public class SqlLikeHelperTests
{
    [Fact]
    public void EscapaComodinesYBarra()
        => Assert.Equal(@"100\% \_ \\", SqlLikeHelper.Escapar(@"100% _ \"));

    [Fact]
    public void ContieneAgregaComodinesSoloAlrededor()
        => Assert.Equal(@"%a\%b%", SqlLikeHelper.Contiene(" a%b "));
}

public class IdentificadoresTests
{
    [Theory]
    [InlineData("U000001", true)] [InlineData("U00001", false)] [InlineData("u000001", false)]
    [InlineData("U000001' OR '1'='1", false)] [InlineData("U000001\n", false)]
    public void Usuario(string id, bool ok) => Assert.Equal(ok, Identificadores.UsuarioValido(id));

    [Theory]
    [InlineData("I0000001", true)] [InlineData("I000001", false)] [InlineData("I0000001;", false)]
    public void Interaccion(string id, bool ok) => Assert.Equal(ok, Identificadores.InteraccionValida(id));

    [Fact]
    public void InvalidoLanza422()
        => Assert.Equal(422, Assert.Throws<DomainException>(() => Identificadores.Usuario("x")).StatusCode);
}

public class PaginacionTests
{
    [Fact]
    public void Defaults() => Assert.Equal(new Paginacion(1, 25), Paginacion.Crear(null, null));

    [Fact]
    public void OffsetCorrecto() => Assert.Equal(50, Paginacion.Crear(3, 25).Offset);

    [Theory]
    [InlineData(0, 10)] [InlineData(-1, 10)] [InlineData(1, 0)] [InlineData(1, 101)] [InlineData(int.MaxValue, 100)]
    public void FueraDeRangoLanza422(int pagina, int tamano)
        => Assert.Equal(422, Assert.Throws<DomainException>(() => Paginacion.Crear(pagina, tamano)).StatusCode);
}

public class ListaBlancaTests
{
    private static readonly Dictionary<string, string> Orden = new() { ["fecha"] = "i.fecha_hora", ["sentimiento"] = "i.sentimiento" };

    [Fact] public void VacioUsaElPorDefecto() => Assert.Equal("i.fecha_hora", ListaBlanca.Resolver(Orden, null, "fecha", "orden"));
    [Fact] public void IgnoraMayusculas() => Assert.Equal("i.sentimiento", ListaBlanca.Resolver(Orden, " SENTIMIENTO ", "fecha", "orden"));

    [Fact]
    public void NuncaDevuelveTextoDelCliente()
        => Assert.Equal(422, Assert.Throws<DomainException>(() => ListaBlanca.Resolver(Orden, "fecha; DROP TABLE x", "fecha", "orden")).StatusCode);
}
