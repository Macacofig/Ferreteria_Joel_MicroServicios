using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Cliente HTTP del front hacia MicroServicioClientes, siempre a través del
    /// ApiGateway. El JWT se envía en la cabecera Authorization y es el mismo
    /// que se guardó en la sesión al iniciar sesión.
    /// </summary>
    public class ClientesApiCliente
    {
        private static readonly JsonSerializerOptions OpcionesJson =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient _http;

        public ClientesApiCliente(HttpClient http)
        {
            _http = http;
        }

        public Task<RespuestaApi<List<ClienteApi>>> ListarAsync(
            string? token,
            string? buscar,
            bool soloActivos,
            string? ordenarPor,
            bool descendente,
            CancellationToken cancelacion = default)
        {
            string ruta =
                $"api/clientes?soloActivos={soloActivos.ToString().ToLowerInvariant()}" +
                $"&descendente={descendente.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta += $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            if (!string.IsNullOrWhiteSpace(ordenarPor))
            {
                ruta += $"&ordenarPor={Uri.EscapeDataString(ordenarPor)}";
            }

            return EnviarAsync<List<ClienteApi>>(HttpMethod.Get, ruta, token, null, cancelacion);
        }

        public Task<RespuestaApi<ClienteApi>> ObtenerAsync(
            string? token,
            int idCliente,
            CancellationToken cancelacion = default) =>
            EnviarAsync<ClienteApi>(HttpMethod.Get, $"api/clientes/{idCliente}", token, null, cancelacion);

        public Task<RespuestaApi<ClienteApi>> CrearAsync(
            string? token,
            ClienteSolicitudApi solicitud,
            CancellationToken cancelacion = default) =>
            EnviarAsync<ClienteApi>(HttpMethod.Post, "api/clientes", token, solicitud, cancelacion);

        public Task<RespuestaApi<ClienteApi>> ActualizarAsync(
            string? token,
            int idCliente,
            ClienteSolicitudApi solicitud,
            CancellationToken cancelacion = default) =>
            EnviarAsync<ClienteApi>(HttpMethod.Put, $"api/clientes/{idCliente}", token, solicitud, cancelacion);

        public Task<RespuestaApi<object>> DarDeBajaAsync(
            string? token,
            int idCliente,
            CancellationToken cancelacion = default) =>
            EnviarAsync<object>(HttpMethod.Delete, $"api/clientes/{idCliente}", token, null, cancelacion);

        public Task<RespuestaApi<object>> ReactivarAsync(
            string? token,
            int idCliente,
            CancellationToken cancelacion = default) =>
            EnviarAsync<object>(HttpMethod.Post, $"api/clientes/{idCliente}/reactivar", token, null, cancelacion);

        private async Task<RespuestaApi<T>> EnviarAsync<T>(
            HttpMethod metodo,
            string ruta,
            string? token,
            object? cuerpo,
            CancellationToken cancelacion)
        {
            using HttpRequestMessage peticion = new(metodo, ruta);

            if (!string.IsNullOrWhiteSpace(token))
            {
                peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (cuerpo is not null)
            {
                peticion.Content = JsonContent.Create(cuerpo);
            }

            using HttpResponseMessage respuesta = await _http.SendAsync(peticion, cancelacion);

            string texto = await respuesta.Content.ReadAsStringAsync(cancelacion);

            if (!string.IsNullOrWhiteSpace(texto))
            {
                try
                {
                    RespuestaApi<T>? contenido =
                        JsonSerializer.Deserialize<RespuestaApi<T>>(texto, OpcionesJson);

                    if (contenido is not null)
                    {
                        return contenido;
                    }
                }
                catch (JsonException)
                {
                    // Se devuelve un error controlado más abajo.
                }
            }

            return new RespuestaApi<T>
            {
                Exitoso = false,
                Mensaje = MensajeDeEstado(respuesta.StatusCode, respuesta.IsSuccessStatusCode)
            };
        }

        private static string MensajeDeEstado(HttpStatusCode estado, bool exito)
        {
            if (exito)
            {
                return "El servicio de clientes devolvió una respuesta vacía.";
            }

            return estado switch
            {
                HttpStatusCode.Unauthorized =>
                    "Su sesión no es válida o venció. Cierre sesión e ingrese de nuevo.",
                HttpStatusCode.Forbidden =>
                    "Su usuario no tiene permiso para realizar esta operación.",
                _ => $"El servicio de clientes respondió con el código {(int)estado}."
            };
        }
    }
}
