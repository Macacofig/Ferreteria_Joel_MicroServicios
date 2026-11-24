namespace FERRETERIA__Joel.Servicios;

/// <summary>
/// Contrato que las páginas Razor usan para trabajar con clientes. Oculta
/// el cliente HTTP y las rutas del ApiGateway.
/// </summary>
public interface IServicioClientes
{
    Task<RespuestaApi<List<ClienteApi>>> ListarAsync(string? token, string? buscar, bool soloActivos, string? ordenarPor, bool descendente, CancellationToken ct = default);
    Task<RespuestaApi<ClienteApi>> ObtenerAsync(string? token, int idCliente, CancellationToken ct = default);
    Task<RespuestaApi<ClienteApi>> CrearAsync(string? token, ClienteSolicitudApi solicitud, CancellationToken ct = default);
    Task<RespuestaApi<ClienteApi>> ActualizarAsync(string? token, int idCliente, ClienteSolicitudApi solicitud, CancellationToken ct = default);
    Task<RespuestaApi<object>> DarDeBajaAsync(string? token, int idCliente, CancellationToken ct = default);
    Task<RespuestaApi<object>> ReactivarAsync(string? token, int idCliente, CancellationToken ct = default);
}