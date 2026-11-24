using System.Security.Claims;
using System.Text.Json;
using MicroServicioProveedores.Aplicacion.CasosDeUso;
using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Comun;
using MicroServicioProveedores.Infraestructura.Factories;
using MicroServicioProveedores.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

// ---------------------------------------------------------------------------
// MongoDB
// ---------------------------------------------------------------------------
string cadenaMongo = builder.Configuration.GetConnectionString("MongoConnection")
    ?? builder.Configuration["ConnectionStrings__MongoConnection"]
    ?? "mongodb://localhost:27017";

string nombreBd = builder.Configuration["Mongo:BaseDatos"]
    ?? builder.Configuration["Mongo__BaseDatos"]
    ?? "bdproveedores";

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(cadenaMongo));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(nombreBd));
// Factory Method: Aplicación solo conoce IRepositoryCreator<T>; el creador
// concreto es el único que referencia al repositorio Mongo.
builder.Services.AddScoped<IRepositoryCreator<IRepositorioProveedores>, ProveedorRepositoryCreator>();
builder.Services.AddScoped<MongoInicializadorProveedores>();

// ---------------------------------------------------------------------------
// Casos de uso (Aplicación)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<ListarProveedoresCasoDeUso>();
builder.Services.AddScoped<ObtenerProveedorCasoDeUso>();
builder.Services.AddScoped<CrearProveedorCasoDeUso>();
builder.Services.AddScoped<ActualizarProveedorCasoDeUso>();
builder.Services.AddScoped<EliminarProveedorCasoDeUso>();

// ---------------------------------------------------------------------------
// Autenticación JWT (Keycloak)
// ---------------------------------------------------------------------------
string authority = builder.Configuration["Keycloak:Authority"]
    ?? "http://localhost:8081/realms/ferreteria";

string metadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
    ?? $"{authority.TrimEnd('/')}/.well-known/openid-configuration";

string[] emisoresValidos = (
    builder.Configuration["Keycloak:ValidIssuers"]
    ?? builder.Configuration["Keycloak:ValidIssuer"]
    ?? authority)
    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = metadataAddress;
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = emisoresValidos,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = contexto =>
            {
                if (contexto.Principal?.Identity is not ClaimsIdentity identidad)
                {
                    return Task.CompletedTask;
                }

                foreach (string rol in ExtraerRoles(contexto.Principal))
                {
                    if (!identidad.HasClaim(ClaimTypes.Role, rol))
                    {
                        identidad.AddClaim(new Claim(ClaimTypes.Role, rol));
                    }
                }

                string? nombre = contexto.Principal.FindFirst("name")?.Value
                    ?? contexto.Principal.FindFirst("preferred_username")?.Value;

                if (!string.IsNullOrWhiteSpace(nombre)
                    && !identidad.HasClaim(ClaimTypes.Name, nombre))
                {
                    identidad.AddClaim(new Claim(ClaimTypes.Name, nombre));
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Inicialización de MongoDB (índice único de NIT, sincronización de contador y datos semilla si aplica).
using (var scope = app.Services.CreateScope())
{
    var inicializador = scope.ServiceProvider.GetRequiredService<MongoInicializadorProveedores>();
    await inicializador.InicializarAsync();
}

// Manejo centralizado de fallos inesperados.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async contexto =>
    {
        var feature = contexto.Features.Get<IExceptionHandlerFeature>();
        var logger = contexto.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MicroServicioProveedores");

        if (feature?.Error is not null)
        {
            logger.LogError(feature.Error, "Error inesperado al procesar la solicitud.");
        }

        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        contexto.Response.ContentType = "application/json; charset=utf-8";

        var respuesta = RespuestaApi<object>.Error(
            "Ocurrió un error inesperado en el servidor.",
            new List<string> { "Ocurrió un error inesperado en el servidor." });

        await contexto.Response.WriteAsJsonAsync(respuesta);
    });
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    servicio = "MicroServicioProveedores",
    estado = "saludable",
    fecha = DateTime.UtcNow
}));

app.MapGet("/index", () => Results.Content(PaginaEstado(), "text/html"));
app.MapGet("/", () => Results.Redirect("/index"));

app.Run();

static IEnumerable<string> ExtraerRoles(ClaimsPrincipal principal)
{
    string? realmAccess = principal.FindFirst("realm_access")?.Value;

    if (string.IsNullOrWhiteSpace(realmAccess))
        return Array.Empty<string>();

    try
    {
        using JsonDocument documento = JsonDocument.Parse(realmAccess);

        if (!documento.RootElement.TryGetProperty("roles", out JsonElement roles)
            || roles.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return roles.EnumerateArray()
            .Where(rol => rol.ValueKind == JsonValueKind.String)
            .Select(rol => rol.GetString())
            .Where(rol => !string.IsNullOrWhiteSpace(rol))
            .Select(rol => rol!)
            .ToArray();
    }
    catch (JsonException)
    {
        return Array.Empty<string>();
    }
}

static string PaginaEstado()
{
    return """
    <!DOCTYPE html>
    <html lang="es">
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <title>MicroServicioProveedores</title>
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
        <h1>MicroServicioProveedores</h1>
        <p><span class="estado">ACTIVO</span></p>
        <p>Microservicio de gestión de proveedores con persistencia en MongoDB y arquitectura limpia.</p>
        <ul>
          <li>API: <code>/api/proveedores</code></li>
          <li>Salud: <code>/health</code></li>
        </ul>
      </div>
    </body>
    </html>
    """;
}
