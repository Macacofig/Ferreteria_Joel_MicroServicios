using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FERRETERIA__Joel.Servicios;

/// <summary>
/// Cliente HTTP (adaptee) que conoce la ruta del API Gateway para el recurso proveedores.
/// </summary>
public class ProveedoresApiCliente
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public ProveedoresApiCliente(HttpClient http)
    {
        _http = http;
    }

    public Task<RespuestaApi<List<ProveedorDto>>> ListarAsync(string? token, string? orden, CancellationToken ct = default)
    {
        string ruta = "api/proveedores";
        if (!string.IsNullOrWhiteSpace(orden))
        {
            ruta += $"?orden={Uri.EscapeDataString(orden)}";
        }
        return EnviarAsync<List<ProveedorDto>>(HttpMethod.Get, ruta, token, null, ct);
    }

    public Task<RespuestaApi<ProveedorDto>> ObtenerAsync(string? token, int id, CancellationToken ct = default)
        => EnviarAsync<ProveedorDto>(HttpMethod.Get, $"api/proveedores/{id}", token, null, ct);

    public Task<RespuestaApi<ProveedorDto>> CrearAsync(string? token, ProveedorSolicitud solicitud, CancellationToken ct = default)
        => EnviarAsync<ProveedorDto>(HttpMethod.Post, "api/proveedores", token, solicitud, ct);

    public Task<RespuestaApi<ProveedorDto>> ActualizarAsync(string? token, int id, ProveedorSolicitud solicitud, CancellationToken ct = default)
        => EnviarAsync<ProveedorDto>(HttpMethod.Put, $"api/proveedores/{id}", token, solicitud, ct);

    public Task<RespuestaApi<object>> EliminarAsync(string? token, int id, CancellationToken ct = default)
        => EnviarAsync<object>(HttpMethod.Delete, $"api/proveedores/{id}", token, null, ct);

    public Task<RespuestaApi<object>> ReactivarAsync(string? token, int id, CancellationToken ct = default)
        => EnviarAsync<object>(HttpMethod.Patch, $"api/proveedores/{id}/reactivar", token, null, ct);

    private async Task<RespuestaApi<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, string? token, object? cuerpo, CancellationToken ct)
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

        using HttpResponseMessage respuesta = await _http.SendAsync(peticion, ct);
        string texto = await respuesta.Content.ReadAsStringAsync(ct);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            try
            {
                RespuestaApi<T>? contenido = JsonSerializer.Deserialize<RespuestaApi<T>>(texto, OpcionesJson);
                if (contenido is not null)
                {
                    return contenido;
                }
            }
            catch (JsonException)
            {
                // se traduce abajo
            }
        }

        string mensaje = respuesta.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "No autorizado. Inicie sesión nuevamente.",
            System.Net.HttpStatusCode.Forbidden => "No tiene permisos para realizar esta acción.",
            _ => respuesta.IsSuccessStatusCode
                ? "La API devolvió una respuesta vacía."
                : $"La API respondió con el código {(int)respuesta.StatusCode}."
        };

        return new RespuestaApi<T>
        {
            Exitoso = false,
            Mensaje = mensaje,
            Errores = new List<string> { mensaje }
        };
    }
}
