using Api.Configuration;
using Api.Middlewares;
using Api.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.AddArchivoEnv();                          // lee backend/.env (credenciales de TiDB y Couchbase)

builder.AddSeguridadBasica();                     // paso 3: límite de cuerpo, texto literal, 400/422 uniformes
builder.Services.AddCorsFrontend(builder.Configuration);   // CORS para el frontend en Vue
builder.Services.AddControllers();
builder.Services.AddCommunity360(builder.Configuration);
builder.Services.AddSwaggerPorModulos();
builder.Services.AddAutenticacion(builder.Configuration);   // paso 4: JWT, roles, límite de login

var app = builder.Build();

app.UseSeguridadBasica();                        // cabeceras de seguridad y HSTS
app.UseMiddleware<ExceptionMiddleware>();        // atrapa todo lo que ocurra después
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Habilitado"))
    app.UseSwaggerPorModulos();                  // documentación por módulos (para la demostración: Swagger__Habilitado=true)
app.UseCors(CorsConfig.Politica);                // antes de autenticar: responde los preflight OPTIONS
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.UseMiddleware<Api.Middlewares.WebSocketMiddleware>();   // /ws (token por cabecera, cookie o ?access_token=)
app.MapControllers();


// Comprobación de que el servidor arranca (paso 0).
app.MapGet("/salud", () => Results.Ok(new { estado = "ok", proyecto = "COMMUNITY 360", fechaUtc = DateTime.UtcNow }));

app.Run();

public partial class Program;   // lo necesitan las pruebas
