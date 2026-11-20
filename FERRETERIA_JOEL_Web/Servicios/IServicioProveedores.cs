namespace FERRETERIA__Joel.Servicios;

public interface IServicioProveedores
{
    Task<RespuestaApi<List<ProveedorDto>>> ListarAsync(string? token, string? orden = null, CancellationToken ct = default);
    Task<RespuestaApi<ProveedorDto>> ObtenerAsync(string? token, int id, CancellationToken ct = default);
    Task<RespuestaApi<ProveedorDto>> CrearAsync(string? token, ProveedorSolicitud solicitud, CancellationToken ct = default);
    Task<RespuestaApi<ProveedorDto>> ActualizarAsync(string? token, int id, ProveedorSolicitud solicitud, CancellationToken ct = default);
    Task<RespuestaApi<object>> EliminarAsync(string? token, int id, CancellationToken ct = default);
}
