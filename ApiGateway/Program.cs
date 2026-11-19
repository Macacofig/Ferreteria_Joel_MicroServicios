using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// YARP: enrutamiento inverso hacia los cuatro microservicios. Las rutas y los
// clústeres se declaran en appsettings.json (sección ReverseProxy).
// ---------------------------------------------------------------------------
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ---------------------------------------------------------------------------
// JWT Bearer: el gateway valida los tokens emitidos por Keycloak usando el
// JWKS del realm. Los roles del realm (realm_access.roles) se normalizan a
// ClaimTypes.Role para que las políticas de autorización sean homogéneas.
// ---------------------------------------------------------------------------
string authority = builder.Configuration["Keycloak:Authority"]
    ?? "http://localhost:8081/realms/ferreteria";

string metadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
    ?? $"{authority.TrimEnd('/')}/.well-known/openid-configuration";

// Se aceptan uno o varios emisores (separados por coma o punto y coma) para
// tolerar tanto el issuer interno de Docker (http://keycloak:8080/realms/...)
// como el público (http://localhost:8081/realms/...).
string[] emisoresValidos = (
    builder.Configuration["Keycloak:ValidIssuers"]
    ?? builder.Configuration["Keycloak:ValidIssuer"]
    ?? authority)
    .Split(new[] { ',', ';' },
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
                ClaimsPrincipal? principal = contexto.Principal;

                if (principal?.Identity is not ClaimsIdentity identidad)
                {
                    contexto.Fail("El token no contiene una identidad válida.");
                    return Task.CompletedTask;
                }

                foreach (string rol in ExtraerRoles(principal))
                {
                    if (!identidad.HasClaim(ClaimTypes.Role, rol))
                    {
                        identidad.AddClaim(new Claim(ClaimTypes.Role, rol));
                    }
                }

                string? nombre = principal.FindFirst("name")?.Value
                    ?? principal.FindFirst("preferred_username")?.Value;

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

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// Endpoints propios del gateway: salud, estado HTML y descubrimiento.
// ---------------------------------------------------------------------------
app.MapGet("/health", () => Results.Ok(new
{
    servicio = "ApiGateway",
    estado = "saludable",
    fecha = DateTime.UtcNow
}));

app.MapGet("/index", () => Results.Content(PaginaEstado(), "text/html"));

app.MapGet("/", () => Results.Redirect("/index"));

// El enrutamiento inverso debe mapearse al final para no interceptar los
// endpoints propios del gateway.
app.MapReverseProxy();

app.Run();

static IEnumerable<string> ExtraerRoles(ClaimsPrincipal principal)
{
    string? realmAccess = principal.FindFirst("realm_access")?.Value;

    if (string.IsNullOrWhiteSpace(realmAccess))
        return Array.Empty<string>();

    try
    {
        using JsonDocument documento = JsonDocument.Parse(realmAccess);

        if (!documento.RootElement.TryGetProperty(
                "roles", out JsonElement roles)
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
      <title>ApiGateway (YARP)</title>
      <style>
        body { font-family: system-ui, Segoe UI, Arial, sans-serif; background:#0f172a; color:#e2e8f0; margin:0; padding:48px; }
        .tarjeta { max-width:820px; margin:0 auto; background:#1e293b; border:1px solid #334155; border-radius:14px; padding:32px; }
        h1 { margin-top:0; color:#38bdf8; }
        .estado { display:inline-block; background:#064e3b; color:#6ee7b7; padding:4px 12px; border-radius:999px; font-weight:600; }
        table { width:100%; border-collapse:collapse; margin-top:16px; }
        th, td { text-align:left; padding:8px 10px; border-bottom:1px solid #334155; }
        code { background:#0f172a; padding:2px 6px; border-radius:6px; color:#fbbf24; }
      </style>
    </head>
    <body>
      <div class="tarjeta">
        <h1>ApiGateway</h1>
        <p><span class="estado">ACTIVO</span></p>
        <p>Puerta de entrada (YARP) que enruta hacia los microservicios y valida los JWT de Keycloak.</p>
        <table>
          <tr><th>Ruta</th><th>Destino</th></tr>
          <tr><td><code>/api/usuarios/**</code></td><td>MicroServicioUsuarios</td></tr>
          <tr><td><code>/api/productos/**</code></td><td>MicroServicioProductos</td></tr>
          <tr><td><code>/api/clientes/**</code></td><td>MicroServicioClientes</td></tr>
          <tr><td><code>/api/proveedores/**</code></td><td>MicroServicioProveedores</td></tr>
        </table>
      </div>
    </body>
    </html>
    """;
}
