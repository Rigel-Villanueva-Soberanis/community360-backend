using System.Text;

namespace Api.Configuration;

/// <summary>
/// Lee el archivo .env (el mismo formato que usa el ETL) y lo traduce a la configuración de la API.
/// Busca .env desde la carpeta del proyecto hacia arriba (backend/.env). Nunca registra valores.
///
/// Claves del .env                       → Configuración
///   TIDB_HOST, TIDB_PORT, TIDB_USER,
///   TIDB_PASSWORD, TIDB_DATABASE, TIDB_CA → ConnectionStrings:TiDb
///   (TIDB_CA_PEM: el texto del certificado en vez de la ruta del archivo; tiene prioridad sobre TIDB_CA)
///   CB_CONNSTR, CB_USER, CB_PASSWORD,
///   CB_BUCKET, CB_SCOPE                   → Couchbase:*
///   JWT_KEY, JWT_MINUTOS                  → Jwt:Key, Jwt:MinutosVida
///   SEED_ADMIN_USUARIO, SEED_ADMIN_PASSWORD → Seed:AdminUsuario, Seed:AdminPassword
///   SEED_ANALISTA_USUARIO, SEED_ANALISTA_PASSWORD (opcionales) → Seed:AnalistaUsuario, Seed:AnalistaPassword
///   SEED_PROFESOR_USUARIO, SEED_PROFESOR_PASSWORD (opcionales) → Seed:ProfesorUsuario, Seed:ProfesorPassword
///   CORS_ORIGENES (opcional, varios separados por coma) → Cors:OrigenesPermitidos  (ej. https://community360.pages.dev)
///
/// Las mismas claves pueden venir como variables de entorno (así se configura el servicio en Render, sin archivo .env).
/// Precedencia: variables de entorno y secretos de usuario ganan; el .env solo rellena lo que está vacío.
/// </summary>
public static class EnvFileConfiguration
{
    private static readonly string[] ClavesConocidas =
    [
        "TIDB_HOST", "TIDB_PORT", "TIDB_USER", "TIDB_PASSWORD", "TIDB_DATABASE", "TIDB_CA", "TIDB_CA_PEM",
        "CB_CONNSTR", "CB_USER", "CB_PASSWORD", "CB_BUCKET", "CB_SCOPE",
        "JWT_KEY", "JWT_MINUTOS",
        "SEED_ADMIN_USUARIO", "SEED_ADMIN_PASSWORD", "SEED_ANALISTA_USUARIO", "SEED_ANALISTA_PASSWORD",
        "SEED_PROFESOR_USUARIO", "SEED_PROFESOR_PASSWORD",
        "CORS_ORIGENES",
    ];

    public static WebApplicationBuilder AddArchivoEnv(this WebApplicationBuilder builder)
    {
        var ruta = BuscarEnv(builder.Environment.ContentRootPath);
        var v = ruta is null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : Leer(ruta);
        foreach (var clave in ClavesConocidas)   // una variable de entorno con la misma clave reemplaza a la del archivo
            if (Environment.GetEnvironmentVariable(clave) is { Length: > 0 } valorEntorno) v[clave] = valorEntorno;
        if (v.Count == 0) return builder;
        var carpeta = ruta is null ? builder.Environment.ContentRootPath : Path.GetDirectoryName(ruta)!;
        var cfg = builder.Configuration;
        var nuevos = new Dictionary<string, string?>();

        void Poner(string clave, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;
            if (!string.IsNullOrWhiteSpace(cfg[clave])) return;      // ya viene de secretos o variables de entorno
            nuevos[clave] = valor;
        }

        // --- TiDB
        if (v.TryGetValue("TIDB_HOST", out var host) && v.TryGetValue("TIDB_USER", out var user)
            && v.TryGetValue("TIDB_PASSWORD", out var pass) && v.TryGetValue("TIDB_DATABASE", out var db))
        {
            var puerto = v.GetValueOrDefault("TIDB_PORT") is { Length: > 0 } p ? p : "4000";
            var ca = CertificadoDesdeTexto(v.GetValueOrDefault("TIDB_CA_PEM")) ?? BuscarCertificado(v.GetValueOrDefault("TIDB_CA"), carpeta);
            var cs = new StringBuilder($"Server={host};Port={puerto};User ID={user};Password=\"{pass.Replace("\"", "\"\"")}\";Database={db}");
            if (ca is not null) cs.Append($";SslMode=VerifyFull;SslCa={ca}");
            else cs.Append(";SslMode=VerifyFull");
            Poner("ConnectionStrings:TiDb", cs.ToString());
        }

        // --- Couchbase
        Poner("Couchbase:ConnectionString", v.GetValueOrDefault("CB_CONNSTR"));
        Poner("Couchbase:Usuario", v.GetValueOrDefault("CB_USER"));
        Poner("Couchbase:Password", v.GetValueOrDefault("CB_PASSWORD"));
        Poner("Couchbase:Bucket", v.GetValueOrDefault("CB_BUCKET"));
        Poner("Couchbase:Scope", v.GetValueOrDefault("CB_SCOPE"));

        // --- Jwt y primer administrador (los usa el paso 4)
        Poner("Jwt:Key", v.GetValueOrDefault("JWT_KEY"));
        Poner("Jwt:MinutosVida", v.GetValueOrDefault("JWT_MINUTOS"));
        Poner("Seed:AdminUsuario", v.GetValueOrDefault("SEED_ADMIN_USUARIO"));
        Poner("Seed:AdminPassword", v.GetValueOrDefault("SEED_ADMIN_PASSWORD"));
        Poner("Seed:AnalistaUsuario", v.GetValueOrDefault("SEED_ANALISTA_USUARIO"));
        Poner("Seed:AnalistaPassword", v.GetValueOrDefault("SEED_ANALISTA_PASSWORD"));
        Poner("Seed:ProfesorUsuario", v.GetValueOrDefault("SEED_PROFESOR_USUARIO"));
        Poner("Seed:ProfesorPassword", v.GetValueOrDefault("SEED_PROFESOR_PASSWORD"));

        // --- Orígenes del frontend (ocupan las primeras posiciones de la lista y reemplazan las de desarrollo de appsettings.json)
        if (v.GetValueOrDefault("CORS_ORIGENES") is { Length: > 0 } cors)
        {
            var origenes = cors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < origenes.Length; i++) nuevos[$"Cors:OrigenesPermitidos:{i}"] = origenes[i];   // reemplaza las posiciones de desarrollo
        }

        if (nuevos.Count > 0) cfg.AddInMemoryCollection(nuevos);
        return builder;
    }

    private static string? BuscarEnv(string desde)
    {
        for (var dir = new DirectoryInfo(desde); dir is not null; dir = dir.Parent)
        {
            var candidato = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidato)) return candidato;
        }
        return null;
    }

    /// <summary>
    /// TIDB_CA_PEM: el contenido del certificado pegado en una variable (útil en Render, donde no se suben archivos .pem).
    /// Acepta saltos de línea reales o escritos como \n. Se guarda en un archivo temporal que usa el conector MySQL.
    /// </summary>
    private static string? CertificadoDesdeTexto(string? pem)
    {
        if (string.IsNullOrWhiteSpace(pem)) return null;
        var texto = pem.Replace("\\n", "\n").Trim();
        if (!texto.Contains("BEGIN CERTIFICATE")) return null;
        var ruta = Path.Combine(Path.GetTempPath(), "c360-tidb-ca.pem");
        File.WriteAllText(ruta, texto + "\n");
        return ruta;
    }

    private static string? BuscarCertificado(string? indicado, string carpetaEnv)
    {
        var candidatos = new List<string>();
        if (!string.IsNullOrWhiteSpace(indicado))
            candidatos.Add(Path.IsPathRooted(indicado) ? indicado : Path.Combine(carpetaEnv, indicado));
        candidatos.Add(Path.Combine(carpetaEnv, "ca.pem"));                      // backend/ca.pem
        candidatos.Add(Path.Combine(carpetaEnv, "..", "etl", "ca.pem"));         // el del ETL
        return candidatos.Where(File.Exists).Select(Path.GetFullPath).FirstOrDefault();
    }

    /// <summary>
    /// Lee el .env. Un valor entre comillas puede ocupar varias líneas (así se pega un certificado .pem completo):
    ///   TIDB_CA_PEM="-----BEGIN CERTIFICATE-----
    ///   ...
    ///   -----END CERTIFICATE-----"
    /// </summary>
    public static Dictionary<string, string> Leer(string ruta)
        => Interpretar(File.ReadAllLines(ruta, Encoding.UTF8));

    public static Dictionary<string, string> Interpretar(IReadOnlyList<string> lineas)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var n = 0; n < lineas.Count; n++)
        {
            var t = lineas[n].Trim();
            if (t.Length == 0 || t[0] == '#' || !t.Contains('=')) continue;
            if (t.StartsWith("export ", StringComparison.OrdinalIgnoreCase)) t = t[7..].TrimStart();
            var i = t.IndexOf('=');
            var clave = t[..i].Trim();
            var valor = t[(i + 1)..].Trim();
            if (valor.Length > 0 && (valor[0] == '"' || valor[0] == '\''))
            {
                var comilla = valor[0];
                if (valor.Length >= 2 && valor[^1] == comilla) valor = valor[1..^1];
                else
                {
                    // valor de varias líneas: sigue hasta la línea que cierra la comilla (si nunca cierra, se toma lo leído)
                    var partes = new List<string> { valor[1..] };
                    while (n + 1 < lineas.Count)
                    {
                        var siguiente = lineas[++n].TrimEnd();
                        if (siguiente.EndsWith(comilla)) { partes.Add(siguiente[..^1]); break; }
                        partes.Add(siguiente);
                    }
                    valor = string.Join("\n", partes.Select(x => x.Trim()));
                }
            }
            if (clave.Length > 0) d[clave] = valor;
        }
        return d;
    }
}
