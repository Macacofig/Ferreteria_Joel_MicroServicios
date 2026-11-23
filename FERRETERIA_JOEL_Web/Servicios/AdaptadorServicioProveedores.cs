namespace FERRETERIA__Joel.Servicios;

/// <summary>
/// Adaptador HTTP de Proveedores: envuelve al cliente HTTP (adaptee) que
/// consume el API Gateway y expone a las páginas Razor un contrato propio,
/// traduciendo las respuestas del servicio para su presentación.
/// </summary>
public class AdaptadorServicioProveedores : IServicioProveedores
{
    private readonly ProveedoresApiCliente _adaptee;

    public AdaptadorServicioProveedores(ProveedoresApiCliente adaptee)
    {
        _adaptee = adaptee;
    }

    public async Task<RespuestaApi<List<ProveedorDto>>> ListarAsync(string? token, string? orden = null, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.ListarAsync(token, orden, ct);
        return Traducir(respuesta, "No se pudo obtener el listado de proveedores.");
    }

    public async Task<RespuestaApi<ProveedorDto>> ObtenerAsync(string? token, int id, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.ObtenerAsync(token, id, ct);
        return Traducir(respuesta, "No se pudo obtener el proveedor solicitado.");
    }

    public async Task<RespuestaApi<ProveedorDto>> CrearAsync(string? token, ProveedorSolicitud solicitud, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.CrearAsync(token, solicitud, ct);
        return Traducir(respuesta, "No se pudo registrar el proveedor.");
    }

    public async Task<RespuestaApi<ProveedorDto>> ActualizarAsync(string? token, int id, ProveedorSolicitud solicitud, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.ActualizarAsync(token, id, solicitud, ct);
        return Traducir(respuesta, "No se pudo actualizar el proveedor.");
    }

    public async Task<RespuestaApi<object>> EliminarAsync(string? token, int id, CancellationToken ct = default)
    {
        var respuesta = await _adaptee.EliminarAsync(token, id, ct);
        return Traducir(respuesta, "No se pudo eliminar el proveedor.");
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
