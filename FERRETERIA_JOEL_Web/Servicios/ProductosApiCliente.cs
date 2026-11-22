using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Cliente HTTP del front hacia el ApiGateway, bajo el prefijo
    /// <c>/api/productos</c>. Es el único punto del proyecto Razor que conoce
    /// las rutas del microservicio de productos y el formato de sus respuestas
    /// (JSON directo en éxito, <c>{ mensaje }</c> en error).
    /// </summary>
    public class ProductosApiCliente
    {
        private static readonly JsonSerializerOptions OpcionesJson =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient _http;

        public ProductosApiCliente(HttpClient http)
        {
            _http = http;
        }

        public Task<ResultadoApi<List<ProductoApi>>> ListarAsync(
            string? token,
            string? ordenarPor,
            string? direccion,
            bool incluirInactivos,
            CancellationToken cancelacion = default)
        {
            List<string> parametros = new()
            {
                $"ordenarPor={Uri.EscapeDataString(ordenarPor ?? string.Empty)}",
                $"direccion={Uri.EscapeDataString(direccion ?? string.Empty)}",
                $"incluirInactivos={(incluirInactivos ? "true" : "false")}"
            };

            return EnviarAsync<List<ProductoApi>>(
                HttpMethod.Get,
                "api/productos?" + string.Join('&', parametros),
                token,
                null,
                cancelacion);
        }

        public Task<ResultadoApi<List<CategoriaApi>>> ListarCategoriasAsync(
            string? token,
            CancellationToken cancelacion = default) =>
            EnviarAsync<List<CategoriaApi>>(
                HttpMethod.Get,
                "api/productos/categorias",
                token,
                null,
                cancelacion);

        public Task<ResultadoApi<List<MarcaApi>>> ListarMarcasAsync(
            string? token,
            CancellationToken cancelacion = default) =>
            EnviarAsync<List<MarcaApi>>(
                HttpMethod.Get,
                "api/productos/marcas",
                token,
                null,
                cancelacion);

        public Task<ResultadoApi<ProductoApi>> ObtenerAsync(
            string? token,
            int idProducto,
            CancellationToken cancelacion = default) =>
            EnviarAsync<ProductoApi>(
                HttpMethod.Get,
                $"api/productos/{idProducto}",
                token,
                null,
                cancelacion);

        public Task<ResultadoApi<CrearProductoRespuestaApi>> CrearAsync(
            string? token,
            GuardarProductoApi solicitud,
            CancellationToken cancelacion = default) =>
            EnviarAsync<CrearProductoRespuestaApi>(
                HttpMethod.Post,
                "api/productos",
                token,
                solicitud,
                cancelacion);

        public Task<ResultadoApi<MensajeApi>> ActualizarAsync(
            string? token,
            int idProducto,
            GuardarProductoApi solicitud,
            CancellationToken cancelacion = default) =>
            EnviarAsync<MensajeApi>(
                HttpMethod.Put,
                $"api/productos/{idProducto}",
                token,
                solicitud,
                cancelacion);

        public Task<ResultadoApi<MensajeApi>> DarDeBajaAsync(
            string? token,
            int idProducto,
            CancellationToken cancelacion = default) =>
            EnviarAsync<MensajeApi>(
                HttpMethod.Delete,
                $"api/productos/{idProducto}",
                token,
                null,
                cancelacion);

        public Task<ResultadoApi<List<HistoricoProductoApi>>> ListarHistoricoAsync(
            string? token,
            int idProducto,
            CancellationToken cancelacion = default) =>
            EnviarAsync<List<HistoricoProductoApi>>(
                HttpMethod.Get,
                $"api/productos/{idProducto}/historico",
                token,
                null,
                cancelacion);

        private async Task<ResultadoApi<T>> EnviarAsync<T>(
            HttpMethod metodo,
            string ruta,
            string? token,
            object? cuerpo,
            CancellationToken cancelacion)
        {
            using HttpRequestMessage peticion = new(metodo, ruta);

            if (!string.IsNullOrWhiteSpace(token))
            {
                peticion.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            if (cuerpo is not null)
            {
                peticion.Content = JsonContent.Create(cuerpo);
            }

            using HttpResponseMessage respuesta =
                await _http.SendAsync(peticion, cancelacion);

            string texto = await respuesta.Content.ReadAsStringAsync(cancelacion);

            if (respuesta.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(texto))
                {
                    return new ResultadoApi<T>
                    {
                        Exitoso = true,
                        Mensaje = "Operación completada correctamente."
                    };
                }

                try
                {
                    T? datos = JsonSerializer.Deserialize<T>(texto, OpcionesJson);

                    return new ResultadoApi<T>
                    {
                        Exitoso = true,
                        Mensaje = LeerMensaje(texto)
                            ?? "Operación completada correctamente.",
                        Datos = datos
                    };
                }
                catch (JsonException ex)
                {
                    return new ResultadoApi<T>
                    {
                        Exitoso = false,
                        Mensaje = "La API devolvió una respuesta inesperada.",
                        Errores = new List<string> { ex.Message }
                    };
                }
            }

            string mensajeError = LeerMensaje(texto)
                ?? $"La API respondió con el código {(int)respuesta.StatusCode}.";

            return new ResultadoApi<T>
            {
                Exitoso = false,
                Mensaje = mensajeError,
                Errores = new List<string> { mensajeError }
            };
        }

        /// <summary>
        /// Lee el campo <c>mensaje</c> que el microservicio usa tanto en errores
        /// como en las respuestas de alta, edición y baja.
        /// </summary>
        private static string? LeerMensaje(string texto)
        {
            try
            {
                using JsonDocument documento = JsonDocument.Parse(texto);

                if (documento.RootElement.ValueKind == JsonValueKind.Object
                    && documento.RootElement.TryGetProperty(
                        "mensaje",
                        out JsonElement mensaje)
                    && mensaje.ValueKind == JsonValueKind.String)
                {
                    return mensaje.GetString();
                }
            }
            catch (JsonException)
            {
                // El cuerpo no era JSON; se usa el mensaje genérico.
            }

            return null;
        }
    }
}
