namespace FERRETERIA__Joel.Servicios;

/// <summary>
/// Patrón Adapter (objeto): envuelve al cliente HTTP de contactos (adaptee)
/// y expone a las Razor Pages el contrato IServicioContactos, traduciendo los
/// mensajes del microservicio para su presentación.
/// </summary>
public class AdaptadorServicioContactos : IServicioContactos
{
    private readonly ClientesApiCliente _adaptee;

    public AdaptadorServicioContactos(ClientesApiCliente adaptee)
    {
        _adaptee = adaptee;
    }

    public async Task<RespuestaApi<List<ContactoApi>>> ListarAsync(
        string? token,
        int idCliente,
        bool soloActivos = true,
        CancellationToken cancelacion = default)
    {
        var respuesta = await _adaptee.ListarContactosAsync(
            token, idCliente, soloActivos, cancelacion);
        return Traducir(respuesta, "No se pudo obtener el listado de contactos.");
    }

    public async Task<RespuestaApi<ContactoApi>> ObtenerAsync(
        string? token,
        int idContacto,
        CancellationToken cancelacion = default)
    {
        var respuesta = await _adaptee.ObtenerContactoAsync(token, idContacto, cancelacion);
        return Traducir(respuesta, "No se pudo obtener el contacto solicitado.");
    }

    public async Task<RespuestaApi<ContactoApi>> CrearAsync(
        string? token,
        int idCliente,
        ContactoSolicitudApi solicitud,
        CancellationToken cancelacion = default)
    {
        var respuesta = await _adaptee.CrearContactoAsync(
            token, idCliente, solicitud, cancelacion);
        return Traducir(respuesta, "No se pudo registrar el contacto.");
    }

    public async Task<RespuestaApi<ContactoApi>> ActualizarAsync(
        string? token,
        int idContacto,
        ContactoSolicitudApi solicitud,
        CancellationToken cancelacion = default)
    {
        var respuesta = await _adaptee.ActualizarContactoAsync(
            token, idContacto, solicitud, cancelacion);
        return Traducir(respuesta, "No se pudo actualizar el contacto.");
    }

    public async Task<RespuestaApi<object>> DarDeBajaAsync(
        string? token,
        int idContacto,
        CancellationToken cancelacion = default)
    {
        var respuesta = await _adaptee.DarDeBajaContactoAsync(token, idContacto, cancelacion);
        return Traducir(respuesta, "No se pudo dar de baja al contacto.");
    }

    public async Task<RespuestaApi<object>> ReactivarAsync(
        string? token,
        int idContacto,
        CancellationToken cancelacion = default)
    {
        var respuesta = await _adaptee.ReactivarContactoAsync(token, idContacto, cancelacion);
        return Traducir(respuesta, "No se pudo reactivar al contacto.");
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
