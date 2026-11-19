using System.Security.Claims;
using System.Text.Json;
using FERRETERIA__Joel.Aplicacion.Servicios;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Utilidades;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Infraestructura.Conexion;
using FERRETERIA__Joel.Infraestructura.Factories;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// El .env de la raíz aporta MySQL, Keycloak, protección de enlaces y SMTP.
// Nunca se versiona.
Dictionary<string, string?> configuracionLocal =
    CargarVariablesEntorno(builder.Environment.ContentRootPath);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddInMemoryCollection(configuracionLocal);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(ConfiguracionSwaggerBearer);

// ---------------------------------------------------------------------------
// Persistencia: MySQL como único motor (paquete MySql.Data). La API solo usa
// las tablas del microservicio de usuarios (empleado, bitacora_sesion).
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();

// Repositorios: patrón Factory Method. Los servicios reciben creadores y
// piden el repositorio concreto a través del contrato del dominio; nunca
// conocen las clases MySql*.
builder.Services.AddScoped<RepositoryCreator<IRepositoryEmpleado>, EmpleadoRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IRepositoryBitacora>, BitacoraRepositoryCreator>();

// ---------------------------------------------------------------------------
// Proveedor de identidad: Keycloak. Emite los JWT y guarda las credenciales;
// el microservicio solo valida la firma del token (JWKS).
// ---------------------------------------------------------------------------
builder.Services.AddHttpClient<IProveedorIdentidad, KeycloakProveedorIdentidad>();

// ---------------------------------------------------------------------------
// Servicios de aplicación (hexágono). El hash Argon2 se conserva como respaldo
// local, pero la autenticación efectiva ocurre en Keycloak.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IHasheadorContrasena, Argon2HasheadorContrasena>();
builder.Services.AddSingleton<ICredencialesValidaciones, CredencialesValidaciones>();
builder.Services.AddScoped<IEnviadorCorreo, SmtpEnviadorCorreo>();
builder.Services.AddScoped<IConstructorEnlaceActivacion, ConstructorEnlaceActivacionApi>();
builder.Services.AddScoped<RegistroCredencialesValidador>();
builder.Services.AddScoped<ServicioRegistroEmpleado>();
builder.Services.AddScoped<ServicioActivacionCuenta>();
builder.Services.AddScoped<ServicioInicioSesion>();
builder.Services.AddScoped<ServicioPersonal>();
builder.Services.AddScoped<ServicioCambioContrasena>();
builder.Services.AddScoped<ServicioEdicionEmpleado>();
builder.Services.AddScoped<ServicioBitacora>();

// La identidad dentro de la API se resuelve desde el JWT validado.
builder.Services.AddScoped<IContextoUsuario, ContextoUsuarioJwt>();

builder.Services.AddSingleton<RevocacionTokens>();

builder.Services.AddSingleton<IRevocacionTokens>(sp =>
    sp.GetRequiredService<RevocacionTokens>());

// ---------------------------------------------------------------------------
// Clave de protección del enlace de activación (AES-GCM compartido con el
// front). Ya no se usa una clave simétrica para firmar JWT: la firma la
// aporta Keycloak.
// ---------------------------------------------------------------------------
string claveUrl = PrimeraNoVacia(
    builder.Configuration["UrlProteccion:Clave"],
    Environment.GetEnvironmentVariable("UrlProteccion__Clave"));

if (string.IsNullOrWhiteSpace(claveUrl))
{
    // En desarrollo, sin clave explícita, se usa una por defecto para no
    // impedir el arranque; en Docker se inyecta por variable de entorno.
    claveUrl = "ferreteria-joel-url-proteccion-clave-desarrollo";
}

UrlProtectorApi.Inicializar(claveUrl);

// ---------------------------------------------------------------------------
// JWT Bearer: se validan los tokens emitidos por Keycloak usando el JWKS del
// realm. El issuer se valida contra la URL configurada y las claves de firma
// se descargan del documento de descubrimiento.
// ---------------------------------------------------------------------------
string metadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
    ?? $"{PrimeraNoVacia(builder.Configuration["Keycloak:Authority"], "http://localhost:8081/realms/ferreteria").TrimEnd('/')}/.well-known/openid-configuration";

string issuerValido = PrimeraNoVacia(
    builder.Configuration["Keycloak:ValidIssuer"],
    builder.Configuration["Keycloak:Authority"],
    "http://localhost:8081/realms/ferreteria");

// Se aceptan uno o varios emisores (separados por coma o punto y coma). Con
// hostname-backchannel-dynamic, Keycloak firma con el issuer interno
// (http://keycloak:8080/realms/ferreteria); en local el issuer es
// http://localhost:8081/realms/ferreteria. Aceptar ambos mantiene la
// validación robusta en los dos escenarios.
string[] emisoresValidos = (
    PrimeraNoVacia(
        builder.Configuration["Keycloak:ValidIssuers"],
        issuerValido))
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
            // Los access tokens de Keycloak traen aud "account"; la autoridad
            // del emisor se garantiza con el issuer y la firma.
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


        OnTokenValidated = async contexto =>
        {
            Console.WriteLine("[JWT] TOKEN VALIDADO CORRECTAMENTE");

            ClaimsIdentity? identidad =
                contexto.Principal?.Identity as ClaimsIdentity;

            if (identidad is null)
                return;

            // 1. Convertir los roles de Keycloak en claims de ASP.NET Core.
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
                                continue;

                            string? rol = elemento.GetString();

                            if (string.IsNullOrWhiteSpace(rol))
                                continue;

                            if (!identidad.HasClaim(ClaimTypes.Role, rol))
                            {
                                identidad.AddClaim(
                                    new Claim(ClaimTypes.Role, rol));
                            }

                            Console.WriteLine(
                                $"[JWT] Rol reconocido: {rol}");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine(
                        $"[JWT] Error leyendo realm_access: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[JWT] No se encontró realm_access.");
            }

            // 2. Obtener el nombre de usuario de Keycloak.
            string? nombreUsuario =
                identidad.FindFirst("preferred_username")?.Value;

            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                Console.WriteLine(
                    "[JWT] No se encontró preferred_username.");

                return;
            }

            // 3. Buscar el empleado en la base de datos local.
            try
            {
                var creador = contexto.HttpContext.RequestServices
                    .GetRequiredService<
                        RepositoryCreator<IRepositoryEmpleado>>();

                var repositorio = creador.CreateRepository();

                FERRETERIA__Joel.Dominio.Entidades.Empleado? empleado =
                    await repositorio
                        .ObtenerPorIdentificadorConCredencialesAsync(
                            nombreUsuario);

                if (empleado is null)
                {
                    Console.WriteLine(
                        $"[JWT] No existe empleado local para: {nombreUsuario}");

                    return;
                }

                // 4. Añadir el identificador local del empleado.
                if (!identidad.HasClaim(c => c.Type == "id_empleado"))
                {
                    identidad.AddClaim(
                        new Claim(
                            "id_empleado",
                            empleado.IdEmpleado.ToString()));
                }

                Console.WriteLine(
                    $"[JWT] Empleado encontrado. IdEmpleado: {empleado.IdEmpleado}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[JWT] Error buscando empleado local:");

                Console.WriteLine(ex);
            }

            // 5. Mostrar los roles que ASP.NET Core utilizará.
            foreach (Claim claim in identidad.FindAll(ClaimTypes.Role))
            {
                Console.WriteLine(
                    $"[JWT] Claim de rol ASP.NET: {claim.Value}");
            }
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

// Solo se redirige a HTTPS si el servicio está configurado con un endpoint
// HTTPS. En Docker la API escucha en http://+:8080 y una redirección dejaría
// inaccesible el puerto.
bool sirveHttps =
    app.Urls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    || (builder.Configuration["Urls"] ?? string.Empty)
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Any(url => url.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (sirveHttps)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "MicroServicioUsuarios v1");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Sonda de salud usada por el front y por el orquestador.
app.MapGet("/health", () => Results.Ok(new
{
    servicio = "MicroServicioUsuarios",
    estado = "saludable",
    fecha = DateTime.UtcNow
}));

// Vista HTML de estado: se abre en el navegador para comprobar que el
// microservicio está activo.
app.MapGet("/index", () => Results.Content(PaginaEstado(), "text/html"));

app.Run();

// Extrae los roles del realm del claim realm_access (objeto JSON anidado).
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

// Lee clave=valor del .env sin modificar el entorno del proceso.
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

static void ConfiguracionSwaggerBearer(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions opciones)
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MicroServicioUsuarios",
        Version = "v1",
        Description = "API de autenticación (Keycloak), empleados, bitácora y activación de cuentas."
    });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el JWT emitido por Keycloak (obtenido en api/usuarios/login).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
}

static string PrimeraNoVacia(params string?[] valores)
{
    return valores.FirstOrDefault(
            valor => !string.IsNullOrWhiteSpace(valor))
        ?? string.Empty;
}

static string PaginaEstado()
{
    return """
    <!DOCTYPE html>
    <html lang="es">
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <title>MicroServicioUsuarios</title>
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
        <h1>MicroServicioUsuarios</h1>
        <p><span class="estado">ACTIVO</span></p>
        <p>Microservicio hexagonal de usuarios: autenticación con Keycloak, gestión de empleados y bitácora.</p>
        <ul>
          <li>API: <code>/api/usuarios</code></li>
          <li>Swagger: <code>/swagger</code></li>
          <li>Salud: <code>/health</code></li>
        </ul>
      </div>
    </body>
    </html>
    """;
}
