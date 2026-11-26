namespace Domain;

/// <summary>Interacción lista para guardar (ids de catálogo ya resueltos).</summary>
public class Interaccion
{
    public string IdUsuario { get; set; } = "";
    public DateTime FechaHora { get; set; }
    public int IdPlataforma { get; set; }
    public int IdTipo { get; set; }
    public string? Texto { get; set; }
    public int IdTema { get; set; }
    public decimal Sentimiento { get; set; }
    public string Idioma { get; set; } = "es";
    public int? IdCiudad { get; set; }
    public int IdDispositivo { get; set; }
    public int LongitudTexto { get; set; }
    public int ReaccionesRecibidas { get; set; }
}

/// <summary>Campos que se pueden modificar de una interacción existente.</summary>
public class InteraccionCambios
{
    public string? Texto { get; set; }
    public int IdTema { get; set; }
    public decimal Sentimiento { get; set; }
    public int LongitudTexto { get; set; }
    public int ReaccionesRecibidas { get; set; }
}

/// <summary>Lo mínimo del usuario que necesita el alta de una interacción.</summary>
public class UsuarioBasico
{
    public string IdUsuario { get; set; } = "";
    public string ConsentimientoDatos { get; set; } = "";
    public int? IdCiudad { get; set; }
}
