using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MicroServicioClientes.Infraestructura.Seguridad;

/// <summary>
/// Configura la validación del JWT emitido por Keycloak (firma por JWKS,
/// emisor y vigencia) y convierte los roles del realm en claims de rol.
/// </summary>
public static class AutenticacionKeycloak
{
    public static IServiceCollection AgregarAutenticacionKeycloak(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        string autoridad = PrimeraNoVacia(
            configuracion["Keycloak:Authority"],
            "http://localhost:8081/realms/ferreteria");

        string metadatos = PrimeraNoVacia(
            configuracion["Keycloak:MetadataAddress"],
            $"{autoridad.TrimEnd('/')}/.well-known/openid-configuration");

        // Se aceptan varios emisores (interno de Docker y público) separados por coma o punto y coma.
        string[] emisores = PrimeraNoVacia(
                configuracion["Keycloak:ValidIssuers"],
                configuracion["Keycloak:ValidIssuer"],
                autoridad)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        servicios
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.MetadataAddress = metadatos;
                opciones.RequireHttpsMetadata = false;

                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = emisores,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                opciones.Events = new JwtBearerEvents
                {
                    OnTokenValidated = contexto =>
                    {
                        if (contexto.Principal?.Identity is not ClaimsIdentity identidad)
                        {
                            contexto.Fail("El token no contiene una identidad válida.");
                            return Task.CompletedTask;
                        }

                        foreach (string rol in ExtraerRoles(contexto.Principal))
                        {
                            if (!identidad.HasClaim(ClaimTypes.Role, rol))
                            {
                                identidad.AddClaim(new Claim(ClaimTypes.Role, rol));
                            }
                        }

                        string? nombre = contexto.Principal.FindFirst("preferred_username")?.Value
                            ?? contexto.Principal.FindFirst("name")?.Value;

                        if (!string.IsNullOrWhiteSpace(nombre)
                            && !identidad.HasClaim(c => c.Type == ClaimTypes.Name))
                        {
                            identidad.AddClaim(new Claim(ClaimTypes.Name, nombre));
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        servicios.AddAuthorization();

        return servicios;
    }

    private static string PrimeraNoVacia(params string?[] valores) =>
        valores.First(v => !string.IsNullOrWhiteSpace(v))!;

    private static IEnumerable<string> ExtraerRoles(ClaimsPrincipal principal)
    {
        string? accesoRealm = principal.FindFirst("realm_access")?.Value;

        if (string.IsNullOrWhiteSpace(accesoRealm))
        {
            return Array.Empty<string>();
        }

        try
        {
            using JsonDocument documento = JsonDocument.Parse(accesoRealm);

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
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}