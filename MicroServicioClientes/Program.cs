using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Aplicacion.Servicios;
using MicroServicioClientes.Dominio.Puertos;
using MicroServicioClientes.Infraestructura.Persistencia;
using MicroServicioClientes.Infraestructura.Seguridad;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Seguridad: valida el JWT de Keycloak y arma los roles.
builder.Services.AgregarAutenticacionKeycloak(builder.Configuration);

// Adaptadores de salida (infraestructura) enlazados a los puertos del núcleo.
builder.Services.AddSingleton<IFabricaConexion, FabricaConexionPostgres>();
builder.Services.AddScoped<IClienteRepositorio, PostgresClienteRepositorio>();
builder.Services.AddScoped<IContextoUsuario, ContextoUsuarioHttp>();

// Casos de uso (puertos de entrada).
builder.Services.AddScoped<IServicioClientes, ServicioClientes>();

// Crea las tablas al arrancar si no existen.
builder.Services.AddHostedService<InicializadorEsquema>();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    servicio = "MicroServicioClientes",
    estado = "saludable",
    fecha = DateTime.UtcNow
}));

app.MapGet("/index", () => Results.Content(PaginaEstado(), "text/html"));
app.MapGet("/", () => Results.Redirect("/index"));

app.Run();

static string PaginaEstado()
{
    return """
    <!DOCTYPE html>
    <html lang="es">
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <title>MicroServicioClientes</title>
      <style>
        body { font-family: system-ui, Segoe UI, Arial, sans-serif; background:#0f172a; color:#e2e8f0; margin:0; padding:48px; }
        .tarjeta { max-width:720px; margin:0 auto; background:#1e293b; border:1px solid #334155; border-radius:14px; padding:32px; }
        h1 { margin-top:0; color:#38bdf8; }
        .estado { display:inline-block; background:#064e3b; color:#6ee7b7; padding:4px 12px; border-radius:999px; font-weight:600; }
        ul { line-height:1.9; }
        code { background:#0f172a; padding:2px 6px; border-radius:6px; color:#fbbf24; }
      </style>
    </head>
    <body>
      <div class="tarjeta">
        <h1>MicroServicioClientes</h1>
        <p><span class="estado">ACTIVO</span></p>
        <p>Microservicio de gestión de clientes. Su base es PostgreSQL (PostgressClientes).</p>
        <ul>
          <li>API: <code>/api/clientes</code></li>
          <li>Salud: <code>/health</code></li>
        </ul>
      </div>
    </body>
    </html>
    """;
}
