using FERRETERIA_Web.Models;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Adapter de objetos del microservicio de usuarios.
    ///
    /// <para>
    /// Envuelve a <see cref="UsuariosApiCliente"/> (el <em>adaptee</em>): el
    /// cliente HTTP que conoce la URL del ApiGateway, las rutas <c>api/usuarios/**</c>
    /// y el contrato <see cref="RespuestaApi{T}"/>. El adaptor ofrece a las
    /// páginas Razor el contrato del front (<see cref="IServicioUsuarios"/> y
    /// <see cref="FERRETERIA_Web.Services.IServicioUsuariosClient"/>), de modo
    /// que la UI, las restricciones por rol y el control de URL no cambian: lo
    /// único que cambia respecto al proyecto anterior es la forma de
    /// comunicarse con la API.
    /// </para>
    /// </summary>
    public class AdaptadorServicioUsuarios
        : IServicioUsuarios, FERRETERIA_Web.Services.IServicioUsuariosClient
    {
        private readonly UsuariosApiCliente _adaptee;
        private readonly IHttpContextAccessor _contexto;

        public AdaptadorServicioUsuarios(
            UsuariosApiCliente adaptee,
            IHttpContextAccessor contexto)
        {
            _adaptee = adaptee;
            _contexto = contexto;
        }

        private string? TokenDeSesion() =>
            _contexto.HttpContext?.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

        // ---------------------------------------------------------------------
        // IServicioUsuarios: el contrato que consumen las páginas "clásicas".
        // El adaptador delega en el adaptee sin alterar la semántica de la UI.
        // ---------------------------------------------------------------------

        public Task<RespuestaApi<LoginApiRespuesta>> LoginAsync(
            string identificador,
            string contrasena,
            CancellationToken cancelacion = default) =>
            _adaptee.LoginAsync(identificador, contrasena, cancelacion);

        public Task<RespuestaApi<object>> LogoutAsync(
            string? token,
            CancellationToken cancelacion = default) =>
            _adaptee.LogoutAsync(token, cancelacion);

        public Task<UsuarioApi?> MeAsync(
            string? token,
            CancellationToken cancelacion = default) =>
            _adaptee.MeAsync(token, cancelacion);

        public Task<RespuestaApi<List<UsuarioApi>>> ListarAsync(
            string? token,
            CancellationToken cancelacion = default) =>
            _adaptee.ListarAsync(token, cancelacion);

        public Task<RespuestaApi<List<UsuarioApi>>> PendientesAsync(
            string? token,
            CancellationToken cancelacion = default) =>
            _adaptee.PendientesAsync(token, cancelacion);

        public Task<RespuestaApi<UsuarioApi>> ObtenerAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default) =>
            _adaptee.ObtenerAsync(token, idEmpleado, cancelacion);

        public Task<RespuestaApi<CrearUsuarioApiRespuesta>> CrearAsync(
            string? token,
            CrearUsuarioApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.CrearAsync(token, solicitud, cancelacion);

        public Task<RespuestaApi<UsuarioApi>> ActualizarAsync(
            string? token,
            int idEmpleado,
            ActualizarUsuarioApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.ActualizarAsync(token, idEmpleado, solicitud, cancelacion);

        public Task<RespuestaApi<object>> DarDeBajaAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default) =>
            _adaptee.DarDeBajaAsync(token, idEmpleado, cancelacion);

        public Task<RespuestaApi<object>> ReactivarAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default) =>
            _adaptee.ReactivarAsync(token, idEmpleado, cancelacion);

        public Task<RespuestaApi<object>> ReenviarActivacionAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default) =>
            _adaptee.ReenviarActivacionAsync(token, idEmpleado, cancelacion);

        public Task<RespuestaApi<ActivacionApiResultado>> ValidarActivacionAsync(
            string tokenActivacion,
            CancellationToken cancelacion = default) =>
            _adaptee.ValidarActivacionAsync(tokenActivacion, cancelacion);

        public Task<RespuestaApi<ActivacionApiResultado>> ActivarAsync(
            string tokenActivacion,
            string contrasena,
            string confirmarContrasena,
            CancellationToken cancelacion = default) =>
            _adaptee.ActivarAsync(
                tokenActivacion, contrasena, confirmarContrasena, cancelacion);

        public Task<RespuestaApi<CambioContrasenaApiRespuesta>> CambiarContrasenaAsync(
            string? token,
            CambioContrasenaApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.CambiarContrasenaAsync(token, solicitud, cancelacion);

        public Task<RespuestaApi<object>> RegistrarBitacoraAsync(
            string? token,
            RegistroBitacoraApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.RegistrarBitacoraAsync(token, solicitud, cancelacion);

        public Task<RespuestaApi<List<BitacoraApi>>> ListarBitacoraAsync(
            string? token,
            int limite = 100,
            CancellationToken cancelacion = default) =>
            _adaptee.ListarBitacoraAsync(token, limite, cancelacion);

        // ---------------------------------------------------------------------
        // IServicioUsuariosClient: el otro contrato del front (páginas
        // NuevoUsuario y CambiarPassword). Aquí el adaptador traduce los DTOs
        // del front a los DTOs HTTP del adaptee y viceversa.
        // ---------------------------------------------------------------------

        public async Task<(bool Exitoso, string Token, string Error)> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            RespuestaApi<LoginApiRespuesta> respuesta = await _adaptee.LoginAsync(
                request.NombreUsuario, request.Password, cancellationToken);

            return respuesta.Exitoso
                ? (true, respuesta.Datos?.Token ?? string.Empty, string.Empty)
                : (false, string.Empty, Mensaje(respuesta));
        }

        public async Task<IReadOnlyList<UsuarioResponse>> ListarAsync(
            CancellationToken cancellationToken = default)
        {
            RespuestaApi<List<UsuarioApi>> respuesta = await _adaptee.ListarAsync(
                TokenDeSesion(), cancellationToken);

            return (respuesta.Datos ?? new List<UsuarioApi>())
                .Select(usuario => Mapear(usuario)!)
                .ToList();
        }

        public async Task<UsuarioResponse?> ObtenerAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            RespuestaApi<UsuarioApi> respuesta = await _adaptee.ObtenerAsync(
                TokenDeSesion(), (int)id, cancellationToken);

            return respuesta.Exitoso ? Mapear(respuesta.Datos) : null;
        }

        public async Task<(bool Exitoso, UsuarioResponse? Usuario, string Error)> CrearAsync(
            CrearUsuarioRequest request,
            CancellationToken cancellationToken = default)
        {
            (string nombre, string apellido) = SepararNombre(request.NombreCompleto);
            (string primerApellido, string? segundoApellido) =
                SepararApellido(apellido);

            RespuestaApi<CrearUsuarioApiRespuesta> respuesta = await _adaptee.CrearAsync(
                TokenDeSesion(),
                new CrearUsuarioApi
                {
                    Nombre = nombre,
                    Apellido = apellido,
                    PrimerApellido = primerApellido,
                    SegundoApellido = segundoApellido,
                    CorreoElectronico = request.Correo,
                    Rol = request.Rol
                },
                cancellationToken);

            return (respuesta.Exitoso,
                Mapear(respuesta.Datos?.Usuario),
                respuesta.Exitoso ? string.Empty : Mensaje(respuesta));
        }

        public async Task<(bool Exitoso, string Error)> EditarAsync(
            long id,
            EditarUsuarioRequest request,
            CancellationToken cancellationToken = default)
        {
            (string nombre, string apellido) = SepararNombre(request.NombreCompleto);
            (string primerApellido, string? segundoApellido) =
                SepararApellido(apellido);

            RespuestaApi<UsuarioApi> respuesta = await _adaptee.ActualizarAsync(
                TokenDeSesion(),
                (int)id,
                new ActualizarUsuarioApi
                {
                    Nombre = nombre,
                    Apellido = apellido,
                    PrimerApellido = primerApellido,
                    SegundoApellido = segundoApellido,
                    CorreoElectronico = request.Correo,
                    Rol = request.Rol
                },
                cancellationToken);

            return (respuesta.Exitoso,
                respuesta.Exitoso ? string.Empty : Mensaje(respuesta));
        }

        public async Task<(bool Exitoso, string Error)> DarDeBajaAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            RespuestaApi<object> respuesta = await _adaptee.DarDeBajaAsync(
                TokenDeSesion(), (int)id, cancellationToken);

            return (respuesta.Exitoso,
                respuesta.Exitoso ? string.Empty : Mensaje(respuesta));
        }

        public async Task<(bool Exitoso, string Error)> ActivarAsync(
            string token,
            ActivarCuentaRequest request,
            CancellationToken cancellationToken = default)
        {
            RespuestaApi<ActivacionApiResultado> respuesta = await _adaptee.ActivarAsync(
                token,
                request.Password,
                request.PasswordConfirmacion,
                cancellationToken);

            return (respuesta.Exitoso,
                respuesta.Exitoso ? string.Empty : Mensaje(respuesta));
        }

        public async Task<(bool Exitoso, string Error)> CambiarPasswordAsync(
            CambiarPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            RespuestaApi<CambioContrasenaApiRespuesta> respuesta =
                await _adaptee.CambiarContrasenaAsync(
                    TokenDeSesion(),
                    new CambioContrasenaApi
                    {
                        ContrasenaActual = request.PasswordActual,
                        ContrasenaNueva = request.PasswordNueva,
                        ConfirmarContrasena = request.PasswordConfirmacion
                    },
                    cancellationToken);

            return (respuesta.Exitoso,
                respuesta.Exitoso ? string.Empty : Mensaje(respuesta));
        }

        // ---------------------------------------------------------------------
        // Traducción de contratos
        // ---------------------------------------------------------------------

        private static UsuarioResponse? Mapear(UsuarioApi? usuario) =>
            usuario is null
                ? null
                : new UsuarioResponse(
                    usuario.IdEmpleado,
                    usuario.NombreCompleto,
                    usuario.NombreUsuario ?? string.Empty,
                    usuario.CorreoElectronico ?? string.Empty,
                    usuario.Rol,
                    usuario.Estado == 1,
                    false);

        private static string Mensaje<T>(RespuestaApi<T> respuesta)
        {
            if (!string.IsNullOrWhiteSpace(respuesta.Mensaje))
            {
                return respuesta.Mensaje;
            }

            return respuesta.Errores.FirstOrDefault()
                ?? "Ocurrió un error al procesar la solicitud.";
        }

        private static (string Nombre, string Apellido) SepararNombre(string completo)
        {
            string[] partes = (completo ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return partes.Length switch
            {
                0 => (string.Empty, string.Empty),
                1 => (partes[0], string.Empty),
                _ => (partes[0], string.Join(' ', partes[1..]))
            };
        }

        /// <summary>
        /// Divide el apellido compuesto que usan los contratos antiguos del
        /// front en primer y segundo apellido para enviarlos a la API.
        /// </summary>
        private static (string PrimerApellido, string? SegundoApellido) SepararApellido(
            string apellido)
        {
            string[] partes = (apellido ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return partes.Length switch
            {
                0 => (string.Empty, null),
                1 => (partes[0], null),
                _ => (partes[0], string.Join(' ', partes[1..]))
            };
        }
    }
}
