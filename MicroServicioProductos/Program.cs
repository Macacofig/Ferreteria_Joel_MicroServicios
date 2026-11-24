using System.Security.Claims;
using System.Text.Json;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Aplicacion.Servicios;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MicroServicioProductos.Infraestructura.Factories;
using MicroServicioProductos.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// El .env de la raíz aporta MySQL y Keycloak. Nunca se versiona.
Dictionary<string, string?> configuracionLocal =
    CargarVariablesEntorno(builder.Environment.ContentRootPath);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddInMemoryCollection(configuracionLocal);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

// ---------------------------------------------------------------------------
// Persistencia MySQL (bdproductos) con patrón Factory Method. Cada entidad
// tiene su propio creador concreto y su propio caso de uso en Aplicación.
// La unidad de trabajo es compartida por todos los repositorios de la
// petición para que producto e historial se confirmen en el mismo commit.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddScoped<IMySqlUnitOfWork, MySqlUnitOfWork>();
builder.Services.AddScoped<IUnitOfWork>(proveedor =>
    proveedor.GetRequiredService<IMySqlUnitOfWork>());

builder.Services.AddScoped<RepositoryCreator<IRepositoryProducto>,
    ProductoRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IRepositoryCategoria>,
    CategoriaRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IRepositoryMarca>,
    MarcaRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IRepositoryHistoricoProducto>,
    HistoricoProductoRepositoryCreator>();

// Puertos de entrada: cada controlador depende solo de su propio servicio.
builder.Services.AddScoped<IServicioProductos, ServicioProductos>();
builder.Services.AddScoped<IServicioCategorias, ServicioCategorias>();
builder.Services.AddScoped<IServicioMarcas, ServicioMarcas>();
builder.Services.AddScoped<IServicioHistoricoProducto, ServicioHistoricoProducto>();

// Identidad resuelta desde el JWT validado.
builder.Services.AddScoped<IContextoUsuario, ContextoUsuarioJwt>();

// ---------------------------------------------------------------------------
// JWT Bearer: misma validación que MicroServicioUsuarios. Keycloak emite el
// token y se validan issuer, firma (JWKS) y vigencia; no se valida audience
// porque los access tokens traen aud "account".
// ---------------------------------------------------------------------------
string metadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
    ?? $"{PrimeraNoVacia(builder.Configuration["Keycloak:Authority"],
        "http://localhost:8081/realms/ferreteria").TrimEnd('/')}/.well-known/openid-configuration";

string issuerValido = PrimeraNoVacia(
    builder.Configuration["Keycloak:ValidIssuer"],
    builder.Configuration["Keycloak:Authority"],
    "http://localhost:8081/realms/ferreteria");

string[] emisoresValidos = (
    PrimeraNoVacia(
        builder.Configuration["Keycloak:ValidIssuers"],
        issuerValido))
    .Split(
        new[] { ',', ';' },
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
            OnMessageReceived = contexto =>
            {
                Console.WriteLine(
                    "[JWT] Authorization recibido: " +
                    (contexto.Request.Headers.ContainsKey("Authorization")
                        ? "SÍ"
                        : "NO"));

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = contexto =>
            {
                Console.WriteLine("[JWT] ERROR:");
                Console.WriteLine(contexto.Exception.ToString());

                return Task.CompletedTask;
            },

            OnTokenValidated = contexto =>
            {
                Console.WriteLine("[JWT] TOKEN VALIDADO CORRECTAMENTE");

                ClaimsIdentity? identidad =
                    contexto.Principal?.Identity as ClaimsIdentity;

                if (identidad is null)
                {
                    return Task.CompletedTask;
                }

                // Los roles de Keycloak llegan dentro del claim realm_access.
                string? realmAccess =
                    identidad.FindFirst("realm_access")?.Value;

                if (!string.IsNullOrWhiteSpace(realmAccess))
                {
                    try
                    {
                        using JsonDocument documento =
                            JsonDocument.Parse(realmAccess);

                        if (documento.RootElement.TryGetProperty(
                                "roles", out JsonElement roles)
                            && roles.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement elemento in roles.EnumerateArray())
                            {
                                if (elemento.ValueKind != JsonValueKind.String)
                                {
                                    continue;
                                }

                                string? rol = elemento.GetString();

                                if (string.IsNullOrWhiteSpace(rol))
                                {
                                    continue;
                                }

                                if (!identidad.HasClaim(ClaimTypes.Role, rol))
                                {
                                    identidad.AddClaim(
                                        new Claim(ClaimTypes.Role, rol));
                                }

                                Console.WriteLine($"[JWT] Rol reconocido: {rol}");
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine(
                            $"[JWT] Error leyendo realm_access: {ex.Message}");
                    }
                }

                string? nombreUsuario =
                    identidad.FindFirst("preferred_username")?.Value;

                Console.WriteLine(nombreUsuario is null
                    ? "[JWT] No se encontró preferred_username."
                    : $"[JWT] Usuario autenticado: {nombreUsuario}");

                return Task.CompletedTask;
            },

            OnChallenge = contexto =>
            {
                Console.WriteLine("[JWT] CHALLENGE");
                Console.WriteLine($"Error: {contexto.Error}");
                Console.WriteLine($"Descripción: {contexto.ErrorDescription}");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// En Docker se escucha solo por HTTP; redirigir a HTTPS dejaría el puerto
// inaccesible.
bool sirveHttps =
    app.Urls.Any(url =>
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    || (builder.Configuration["Urls"] ?? string.Empty)
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Any(url =>
            url.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (sirveHttps)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Sondas públicas para Docker, el ApiGateway y el navegador.
app.MapGet("/health", () => Results.Ok(new
{
    servicio = "MicroServicioProductos",
    estado = "saludable",
    fecha = DateTime.UtcNow
})).AllowAnonymous();

app.MapGet("/index", () =>
    Results.Content(PaginaEstado(), "text/html")).AllowAnonymous();

app.MapGet("/", () =>
    Results.Redirect("/index")).AllowAnonymous();

app.Run();

// Lee clave=valor del .env sin sobrescribir variables ya definidas.
static Dictionary<string, string?> CargarVariablesEntorno(string directorio)
{
    Dictionary<string, string?> variables = new(
        StringComparer.OrdinalIgnoreCase);

    string? ruta = BuscarArchivoEnv(directorio);

    if (ruta is null)
    {
        return variables;
    }

    foreach (string linea in File.ReadAllLines(ruta))
    {
        string texto = linea.Trim();

        if (texto.Length == 0 || texto.StartsWith('#'))
        {
            continue;
        }

        int separador = texto.IndexOf('=');

        if (separador <= 0)
        {
            continue;
        }

        string clave = texto[..separador].Trim();
        string valor = texto[(separador + 1)..].Trim().Trim('"');

        variables[clave.Replace("__", ":", StringComparison.Ordinal)] = valor;

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(clave)))
        {
            Environment.SetEnvironmentVariable(clave, valor);
        }
    }

    return variables;
}

static string? BuscarArchivoEnv(string directorio)
{
    string? actual = Path.GetFullPath(directorio);

    for (int nivel = 0; nivel < 5 && actual is not null; nivel++)
    {
        string ruta = Path.Combine(actual, ".env");

        if (File.Exists(ruta))
        {
            return ruta;
        }

        DirectoryInfo? padre = Directory.GetParent(actual);
        actual = padre?.FullName;
    }

    return null;
}

static string PrimeraNoVacia(params string?[] valores) =>
    valores.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor))
    ?? string.Empty;

static string PaginaEstado()
{
    return """
    <!DOCTYPE html>
    <html lang="es">
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <title>MicroServicioProductos</title>
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
        <h1>MicroServicioProductos</h1>
        <p><span class="estado">ACTIVO</span></p>
        <p>Microservicio hexagonal del catálogo de productos de la ferretería.</p>
        <ul>
          <li>API: <code>/api/productos</code></li>
          <li>Categorías: <code>/api/productos/categorias</code></li>
          <li>Marcas: <code>/api/productos/marcas</code></li>
          <li>Histórico: <code>/api/productos/{id}/historico</code></li>
          <li>Salud: <code>/health</code></li>
        </ul>
      </div>
    </body>
    </html>
    """;
}
