namespace FERRETERIA__Joel.Servicios;

/// <summary>
/// Patrón Adapter (objeto): envuelve al cliente HTTP (adaptee) que consume
/// el ApiGateway y expone a las páginas Razor el contrato IServicioClientes,
/// traduciendo las respuestas del microservicio para su presentación.
/// </summary>
public class AdaptadorServicioClientes : IServicioClientes
{
    private readonly ClientesApiCliente _adaptee;

    public AdaptadorServicioClientes(ClientesApiCliente adaptee)
    {
        _adaptee = adaptee;
    }

    public async Task<RespuestaApi<List<ClienteApi>>> ListarAsync(string? token, string? buscar, bool soloActivos, string? ordenarPor, bool descendente, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.ListarAsync(token, buscar, soloActivos, ordenarPor, descendente, ct);
        return Traducir(respuesta, "No se pudo obtener el listado de clientes.");
    }

    public async Task<RespuestaApi<ClienteApi>> ObtenerAsync(string? token, int idCliente, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.ObtenerAsync(token, idCliente, ct);
        return Traducir(respuesta, "No se pudo obtener el cliente solicitado.");
    }

    public async Task<RespuestaApi<ClienteApi>> CrearAsync(string? token, ClienteSolicitudApi solicitud, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.CrearAsync(token, solicitud, ct);
        return Traducir(respuesta, "No se pudo registrar el cliente.");
    }

    public async Task<RespuestaApi<ClienteApi>> ActualizarAsync(string? token, int idCliente, ClienteSolicitudApi solicitud, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.ActualizarAsync(token, idCliente, solicitud, ct);
        return Traducir(respuesta, "No se pudo actualizar el cliente.");
    }

    public async Task<RespuestaApi<object>> DarDeBajaAsync(string? token, int idCliente, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.DarDeBajaAsync(token, idCliente, ct);
        return Traducir(respuesta, "No se pudo dar de baja al cliente.");
    }

    public async Task<RespuestaApi<object>> ReactivarAsync(string? token, int idCliente, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.ReactivarAsync(token, idCliente, ct);
        return Traducir(respuesta, "No se pudo reactivar al cliente.");
    }

    private static RespuestaApi<T> Traducir<T>(RespuestaApi<T> respuesta, string mensajePorDefecto)
    {
        if (respuesta.Exitoso)
        {
            return respuesta;
        }

        if (string.IsNullOrWhiteSpace(respuesta.Mensaje))
        {
            respuesta.Mensaje = respuesta.Errores.FirstOrDefault() ?? mensajePorDefecto;
        }

        if (respuesta.Errores.Count == 0 && !string.IsNullOrWhiteSpace(respuesta.Mensaje))
        {
            respuesta.Errores.Add(respuesta.Mensaje);
        }

        return respuesta;
    }
}