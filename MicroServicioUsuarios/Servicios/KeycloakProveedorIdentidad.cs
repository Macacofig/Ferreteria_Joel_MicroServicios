using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Resultados;
using Microsoft.Extensions.Logging;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Adaptador del puerto <see cref="IProveedorIdentidad"/> contra Keycloak.
    ///
    /// <para>
    /// Usa dos APIs de Keycloak:
    /// <list type="bullet">
    /// <item>el endpoint de tokens del realm (flujo Resource Owner Password
    /// Credentials) para iniciar sesión;</item>
    /// <item>la Admin REST API (con un token de administrador obtenido por
    /// client credentials del cliente <c>admin-cli</c>) para provisionar y
    /// mantener las cuentas.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class KeycloakProveedorIdentidad : IProveedorIdentidad
    {
        private readonly HttpClient _http;
        private readonly ILogger<KeycloakProveedorIdentidad> _logger;
        private readonly string _tokenEndpoint;
        private readonly string _adminBaseUrl;
        private readonly string _realm;
        private readonly string _clientId;
        private readonly string _adminUser;
        private readonly string _adminPassword;

        public KeycloakProveedorIdentidad(
            HttpClient http,
            IConfiguration configuration,
            ILogger<KeycloakProveedorIdentidad> logger)
        {
            _http = http;
            _logger = logger;

            string authority = (configuration["Keycloak:Authority"]
                ?? "http://localhost:8081/realms/ferreteria").TrimEnd('/');

            _realm = configuration["Keycloak:Realm"] ?? "ferreteria";
            _clientId = configuration["Keycloak:ClientId"] ?? "ferreteria-web";
            _adminUser = configuration["Keycloak:AdminUser"] ?? "admin";
            _adminPassword = configuration["Keycloak:AdminPassword"] ?? "admin";

            // El endpoint de tokens y la Admin API se invocan por la red interna
            // de Docker; el issuer del token, en cambio, es la URL pública.
            string baseInterna = (configuration["Keycloak:BaseUrlInterna"]
                ?? authority).TrimEnd('/');

            _tokenEndpoint = configuration["Keycloak:TokenEndpoint"]
                ?? $"{baseInterna}/protocol/openid-connect/token";

            _adminBaseUrl = configuration["Keycloak:AdminBaseUrl"]
                ?? $"{baseInterna.Replace($"/realms/{_realm}", string.Empty)}/admin/realms/{_realm}";
        }

        public async Task<Result<TokenIdentidad>> IniciarSesionAsync(
            string identificador,
            string contrasena,
            CancellationToken cancelacion = default)
        {
            if (string.IsNullOrWhiteSpace(identificador)
                || string.IsNullOrWhiteSpace(contrasena))
            {
                return Result.Fallo<TokenIdentidad>(
                    "Ingrese su correo o usuario y su contraseña.");
            }

            using FormUrlEncodedContent cuerpo = new(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _clientId,
                ["username"] = identificador,
                ["password"] = contrasena,
                ["scope"] = "openid"
            });

            try
            {
                using HttpResponseMessage respuesta =
                    await _http.PostAsync(_tokenEndpoint, cuerpo, cancelacion);

                if (!respuesta.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Keycloak rechazó el inicio de sesión de {Usuario} ({Codigo}).",
                        identificador,
                        (int)respuesta.StatusCode);

                    return Result.Fallo<TokenIdentidad>(
                        "Correo o contraseña incorrectos.");
                }

                using JsonDocument documento = JsonDocument.Parse(
                    await respuesta.Content.ReadAsStringAsync(cancelacion));

                JsonElement raiz = documento.RootElement;

                string token = raiz.TryGetProperty("access_token", out JsonElement acceso)
                    ? acceso.GetString() ?? string.Empty
                    : string.Empty;

                string refresco = raiz.TryGetProperty("refresh_token", out JsonElement refresh)
                    ? refresh.GetString() ?? string.Empty
                    : string.Empty;

                int segundos = raiz.TryGetProperty("expires_in", out JsonElement expira)
                    ? expira.GetInt32()
                    : 300;

                if (string.IsNullOrWhiteSpace(token))
                {
                    return Result.Fallo<TokenIdentidad>(
                        "Keycloak no devolvió un token válido.");
                }

                return Result.Ok(new TokenIdentidad
                {
                    Token = token,
                    RefreshToken = refresco,
                    Expira = DateTime.UtcNow.AddSeconds(segundos)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error de comunicación con Keycloak.");
                return Result.Fallo<TokenIdentidad>(
                    "No se pudo contactar al proveedor de identidad.");
            }
        }

        public async Task<Result> RegistrarUsuarioAsync(
            SolicitudIdentidad solicitud,
            CancellationToken cancelacion = default)
        {
            string? tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);

            if (tokenAdmin is null)
            {
                return Result.Fallo(
                    "No se pudo autenticar contra la Admin API de Keycloak.");
            }

            (string nombre, string apellido) = SepararNombre(solicitud.NombreCompleto);

            var representacion = new Dictionary<string, object?>
            {
                ["username"] = solicitud.NombreUsuario,
                ["email"] = solicitud.CorreoElectronico,
                ["firstName"] = nombre,
                ["lastName"] = apellido,
                ["enabled"] = true,
                ["emailVerified"] = true,
                ["credentials"] = string.IsNullOrWhiteSpace(solicitud.Contrasena)
                    ? Array.Empty<object>()
                    : new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "password",
                            ["value"] = solicitud.Contrasena!,
                            ["temporary"] = false
                        }
                    }
            };

            using HttpResponseMessage respuesta = await EnviarAdminAsync(
                HttpMethod.Post, "/users", tokenAdmin, representacion, cancelacion);

            if (respuesta.StatusCode == HttpStatusCode.Conflict)
            {
                // El usuario ya existe: se actualiza en lugar de fallar.
                return await ActualizarUsuarioAsync(
                    solicitud.NombreUsuario, solicitud, cancelacion);
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Keycloak no pudo crear el usuario {Usuario} ({Codigo}).",
                    solicitud.NombreUsuario, (int)respuesta.StatusCode);
                return Result.Fallo("No se pudo crear el usuario en Keycloak.");
            }

            await AsignarRolAsync(solicitud.NombreUsuario, solicitud.Rol, tokenAdmin, cancelacion);

            return Result.Ok();
        }

        public async Task<Result> ActualizarUsuarioAsync(
            string identificadorActual,
            SolicitudIdentidad solicitud,
            CancellationToken cancelacion = default)
        {
            string? tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);

            if (tokenAdmin is null)
            {
                return Result.Fallo(
                    "No se pudo autenticar contra la Admin API de Keycloak.");
            }

            string? id = await BuscarIdUsuarioAsync(identificadorActual, tokenAdmin, cancelacion);

            if (id is null)
            {
                return Result.Fallo("El usuario no existe en Keycloak.");
            }

            (string nombre, string apellido) = SepararNombre(solicitud.NombreCompleto);

            var representacion = new Dictionary<string, object?>
            {
                ["email"] = solicitud.CorreoElectronico,
                ["firstName"] = nombre,
                ["lastName"] = apellido,
                ["emailVerified"] = true
            };

            using HttpResponseMessage respuesta = await EnviarAdminAsync(
                HttpMethod.Put, $"/users/{id}", tokenAdmin, representacion, cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return Result.Fallo("No se pudo actualizar el usuario en Keycloak.");
            }

            await AsignarRolAsync(identificadorActual, solicitud.Rol, tokenAdmin, cancelacion);

            return Result.Ok();
        }

        public async Task<Result> EstablecerContrasenaAsync(
            string identificador,
            string contrasena,
            CancellationToken cancelacion = default)
        {
            string? tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);

            if (tokenAdmin is null)
            {
                return Result.Fallo(
                    "No se pudo autenticar contra la Admin API de Keycloak.");
            }

            string? id = await BuscarIdUsuarioAsync(identificador, tokenAdmin, cancelacion);

            if (id is null)
            {
                return Result.Fallo("El usuario no existe en Keycloak.");
            }

            var representacion = new Dictionary<string, object>
            {
                ["type"] = "password",
                ["value"] = contrasena,
                ["temporary"] = false
            };

            using HttpResponseMessage respuesta = await EnviarAdminAsync(
                HttpMethod.Put,
                $"/users/{id}/reset-password",
                tokenAdmin,
                representacion,
                cancelacion);

            return respuesta.IsSuccessStatusCode
                ? Result.Ok()
                : Result.Fallo("No se pudo actualizar la contraseña en Keycloak.");
        }

        public async Task<Result> CambiarEstadoAsync(
            string identificador,
            bool habilitado,
            CancellationToken cancelacion = default)
        {
            string? tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);

            if (tokenAdmin is null)
            {
                return Result.Fallo(
                    "No se pudo autenticar contra la Admin API de Keycloak.");
            }

            string? id = await BuscarIdUsuarioAsync(identificador, tokenAdmin, cancelacion);

            if (id is null)
            {
                return Result.Fallo("El usuario no existe en Keycloak.");
            }

            var representacion = new Dictionary<string, object>
            {
                ["enabled"] = habilitado
            };

            using HttpResponseMessage respuesta = await EnviarAdminAsync(
                HttpMethod.Put, $"/users/{id}", tokenAdmin, representacion, cancelacion);

            return respuesta.IsSuccessStatusCode
                ? Result.Ok()
                : Result.Fallo("No se pudo cambiar el estado del usuario en Keycloak.");
        }

        // ---------------------------------------------------------------------
        // Auxiliares
        // ---------------------------------------------------------------------

        private async Task<string?> ObtenerTokenAdminAsync(CancellationToken cancelacion)
        {
            string endpoint = _tokenEndpoint.Replace(
                $"/realms/{_realm}/protocol",
                "/realms/master/protocol");

            using FormUrlEncodedContent cuerpo = new(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = _adminUser,
                ["password"] = _adminPassword
            });

            try
            {
                using HttpResponseMessage respuesta =
                    await _http.PostAsync(endpoint, cuerpo, cancelacion);

                if (!respuesta.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "La Admin API de Keycloak rechazó las credenciales ({Codigo}).",
                        (int)respuesta.StatusCode);
                    return null;
                }

                using JsonDocument documento = JsonDocument.Parse(
                    await respuesta.Content.ReadAsStringAsync(cancelacion));

                return documento.RootElement.TryGetProperty("access_token", out JsonElement token)
                    ? token.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el token de administrador de Keycloak.");
                return null;
            }
        }

        private async Task<string?> BuscarIdUsuarioAsync(
            string identificador,
            string tokenAdmin,
            CancellationToken cancelacion)
        {
            string ruta = $"/users?exact=true&username={Uri.EscapeDataString(identificador)}";

            using HttpResponseMessage respuesta = await EnviarAdminAsync(
                HttpMethod.Get, ruta, tokenAdmin, null, cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return null;
            }

            using JsonDocument documento = JsonDocument.Parse(
                await respuesta.Content.ReadAsStringAsync(cancelacion));

            if (documento.RootElement.ValueKind != JsonValueKind.Array
                || documento.RootElement.GetArrayLength() == 0)
            {
                // Reintento por correo cuando el identificador es el correo.
                string rutaCorreo =
                    $"/users?exact=true&email={Uri.EscapeDataString(identificador)}";

                using HttpResponseMessage porCorreo = await EnviarAdminAsync(
                    HttpMethod.Get, rutaCorreo, tokenAdmin, null, cancelacion);

                if (!porCorreo.IsSuccessStatusCode)
                {
                    return null;
                }

                using JsonDocument docCorreo = JsonDocument.Parse(
                    await porCorreo.Content.ReadAsStringAsync(cancelacion));

                return ExtraerId(docCorreo.RootElement);
            }

            return ExtraerId(documento.RootElement);
        }

        private static string? ExtraerId(JsonElement arreglo)
        {
            if (arreglo.ValueKind != JsonValueKind.Array
                || arreglo.GetArrayLength() == 0)
            {
                return null;
            }

            return arreglo[0].TryGetProperty("id", out JsonElement id)
                ? id.GetString()
                : null;
        }

        private async Task AsignarRolAsync(
            string identificador,
            string rol,
            string tokenAdmin,
            CancellationToken cancelacion)
        {
            if (string.IsNullOrWhiteSpace(rol))
            {
                return;
            }

            string? idUsuario = await BuscarIdUsuarioAsync(identificador, tokenAdmin, cancelacion);

            if (idUsuario is null)
            {
                return;
            }

            using HttpResponseMessage respuestaRol = await EnviarAdminAsync(
                HttpMethod.Get,
                $"/roles/{Uri.EscapeDataString(rol)}",
                tokenAdmin,
                null,
                cancelacion);

            if (!respuestaRol.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "El rol de realm {Rol} no existe en Keycloak.", rol);
                return;
            }

            using JsonDocument documento = JsonDocument.Parse(
                await respuestaRol.Content.ReadAsStringAsync(cancelacion));

            var representacion = new[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = documento.RootElement.GetProperty("id").GetString(),
                    ["name"] = documento.RootElement.GetProperty("name").GetString()
                }
            };

            using HttpResponseMessage asignacion = await EnviarAdminAsync(
                HttpMethod.Post,
                $"/users/{idUsuario}/role-mappings/realm",
                tokenAdmin,
                representacion,
                cancelacion);

            if (!asignacion.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "No se pudo asignar el rol {Rol} en Keycloak ({Codigo}).",
                    rol, (int)asignacion.StatusCode);
            }
        }

        private async Task<HttpResponseMessage> EnviarAdminAsync(
            HttpMethod metodo,
            string ruta,
            string tokenAdmin,
            object? cuerpo,
            CancellationToken cancelacion)
        {
            using HttpRequestMessage peticion = new(metodo, _adminBaseUrl + ruta);
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

            if (cuerpo is not null)
            {
                peticion.Content = JsonContent.Create(cuerpo);
            }

            return await _http.SendAsync(peticion, cancelacion);
        }

        private static (string Nombre, string Apellido) SepararNombre(string nombreCompleto)
        {
            string[] partes = (nombreCompleto ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return partes.Length switch
            {
                0 => (string.Empty, string.Empty),
                1 => (partes[0], string.Empty),
                _ => (partes[0], string.Join(' ', partes[1..]))
            };
        }
    }
}
