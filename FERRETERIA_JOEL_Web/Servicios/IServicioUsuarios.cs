using FERRETERIA__Joel.Servicios;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Target del patrón Adapter para el microservicio de usuarios. Las páginas
    /// Razor dependen de este contrato y no del cliente HTTP concreto, de modo
    /// que la UI permanece intacta aunque cambie la forma de comunicarse con la
    /// API (antes directa, ahora a través del ApiGateway).
    /// </summary>
    public interface IServicioUsuarios
    {
        Task<RespuestaApi<LoginApiRespuesta>> LoginAsync(
            string identificador,
            string contrasena,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<object>> LogoutAsync(
            string? token,
            CancellationToken cancelacion = default);

        Task<UsuarioApi?> MeAsync(
            string? token,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<List<UsuarioApi>>> ListarAsync(
            string? token,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<List<UsuarioApi>>> PendientesAsync(
            string? token,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<UsuarioApi>> ObtenerAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<CrearUsuarioApiRespuesta>> CrearAsync(
            string? token,
            CrearUsuarioApi solicitud,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<UsuarioApi>> ActualizarAsync(
            string? token,
            int idEmpleado,
            ActualizarUsuarioApi solicitud,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<object>> DarDeBajaAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<object>> ReactivarAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<object>> ReenviarActivacionAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<ActivacionApiResultado>> ValidarActivacionAsync(
            string tokenActivacion,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<ActivacionApiResultado>> ActivarAsync(
            string tokenActivacion,
            string contrasena,
            string confirmarContrasena,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<CambioContrasenaApiRespuesta>> CambiarContrasenaAsync(
            string? token,
            CambioContrasenaApi solicitud,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<object>> RegistrarBitacoraAsync(
            string? token,
            RegistroBitacoraApi solicitud,
            CancellationToken cancelacion = default);

        Task<RespuestaApi<List<BitacoraApi>>> ListarBitacoraAsync(
            string? token,
            int limite = 100,
            CancellationToken cancelacion = default);
    }
}
