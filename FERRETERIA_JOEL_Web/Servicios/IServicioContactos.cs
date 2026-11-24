namespace FERRETERIA__Joel.Servicios;

/// <summary>
/// Contrato que las Razor Pages usan para administrar la segunda tabla del
/// microservicio de clientes. Oculta el cliente HTTP y las rutas del Gateway.
/// </summary>
public interface IServicioContactos
{
    Task<RespuestaApi<List<ContactoApi>>> ListarAsync(
        string? token,
        int idCliente,
        bool soloActivos = true,
        CancellationToken cancelacion = default);

    Task<RespuestaApi<ContactoApi>> ObtenerAsync(
        string? token,
        int idContacto,
        CancellationToken cancelacion = default);

    Task<RespuestaApi<ContactoApi>> CrearAsync(
        string? token,
        int idCliente,
        ContactoSolicitudApi solicitud,
        CancellationToken cancelacion = default);

    Task<RespuestaApi<ContactoApi>> ActualizarAsync(
        string? token,
        int idContacto,
        ContactoSolicitudApi solicitud,
        CancellationToken cancelacion = default);

    Task<RespuestaApi<object>> DarDeBajaAsync(
        string? token,
        int idContacto,
        CancellationToken cancelacion = default);

    Task<RespuestaApi<object>> ReactivarAsync(
        string? token,
        int idContacto,
        CancellationToken cancelacion = default);
}
