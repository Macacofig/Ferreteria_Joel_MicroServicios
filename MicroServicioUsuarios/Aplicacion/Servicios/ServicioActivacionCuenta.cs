using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Resultados;
using FERRETERIA__Joel.Infraestructura.Factories;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Activa la cuenta de un empleado a partir del token recibido por correo.
    /// El token es de un solo uso y vence a los 10 minutos; al validarse, el
    /// empleado define su contraseña y la cuenta pasa al estado activo.
    /// </summary>
    public class ServicioActivacionCuenta
    {
        private const string MensajeTokenInvalido =
            "El enlace de activación no es válido o ya fue utilizado.";

        private const string MensajeTokenVencido =
            "El enlace de activación venció. Solicite uno nuevo al administrador.";

        private const string MensajeNoPendiente =
            "Esta cuenta ya fue activada. Puede iniciar sesión.";

        private readonly RepositoryCreator<IRepositoryEmpleado> _creadorRepositorio;
        private readonly IHasheadorContrasena _hasheador;
        private readonly RegistroCredencialesValidador _validador;
        private readonly IProveedorIdentidad _identidad;
        private readonly ILogger<ServicioActivacionCuenta> _logger;
        private IRepositoryEmpleado? _repositorio;

        public ServicioActivacionCuenta(
            RepositoryCreator<IRepositoryEmpleado> creadorRepositorio,
            IHasheadorContrasena hasheador,
            RegistroCredencialesValidador validador,
            IProveedorIdentidad identidad,
            ILogger<ServicioActivacionCuenta> logger)
        {
            _creadorRepositorio = creadorRepositorio;
            _hasheador = hasheador;
            _validador = validador;
            _identidad = identidad;
            _logger = logger;
        }

        private IRepositoryEmpleado Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        /// <summary>
        /// Comprueba el token y devuelve el nombre del empleado si el enlace
        /// sigue siendo válido (para mostrar la pantalla de definir
        /// contraseña).
        /// </summary>
        public async Task<ResultadoActivacion> ValidarTokenAsync(string? token)
        {
            (Empleado? empleado, string? error) =
                await BuscarPorTokenAsync(token);

            if (empleado is null)
            {
                return ResultadoActivacion.Invalido(error!);
            }

            return ResultadoActivacion.Valido(
                $"{empleado.Nombre} {empleado.Apellido}".Trim());
        }

        /// <summary>
        /// Define la contraseña y activa la cuenta. Revalida el token antes de
        /// aplicar los cambios.
        /// </summary>
        public async Task<ResultadoActivacion> ActivarAsync(
            ActivacionSolicitud solicitud)
        {
            (Empleado? empleado, string? error) =
                await BuscarPorTokenAsync(solicitud.Token);

            if (empleado is null)
            {
                return ResultadoActivacion.Invalido(error!);
            }

            Dictionary<string, string> errores =
                _validador.ValidarContrasenaActivacion(
                    solicitud.Contrasena,
                    solicitud.ConfirmarContrasena);

            string nombreEmpleado =
                $"{empleado.Nombre} {empleado.Apellido}".Trim();

            if (errores.Any())
            {
                return new ResultadoActivacion
                {
                    Exitoso = true,
                    NombreEmpleado = nombreEmpleado,
                    ErroresCampo = errores
                };
            }

            await Repositorio.ActivarCuentaAsync(
                empleado.IdEmpleado,
                _hasheador.Hashear(solicitud.Contrasena),
                EstadoEmpleado.Activo);

            // Las credenciales viven en Keycloak: al activar la cuenta se crea
            // el usuario en el realm con la contraseña elegida y su rol.
            await SincronizarConKeycloakAsync(empleado, solicitud.Contrasena);

            return ResultadoActivacion.Valido(nombreEmpleado);
        }

        private async Task SincronizarConKeycloakAsync(
            Empleado empleado,
            string contrasena)
        {
            try
            {
                Result resultado = await _identidad.RegistrarUsuarioAsync(
                    new SolicitudIdentidad
                    {
                        NombreUsuario = empleado.NombreUsuario
                            ?? empleado.CorreoElectronico
                            ?? empleado.IdEmpleado.ToString(),
                        CorreoElectronico = empleado.CorreoElectronico ?? string.Empty,
                        NombreCompleto =
                            $"{empleado.Nombre} {empleado.Apellido}".Trim(),
                        Contrasena = contrasena,
                        Rol = RolesSistema.Normalizar(empleado.Rol)
                    });

                if (resultado.EsFallo)
                {
                    _logger.LogWarning(
                        "La cuenta {IdEmpleado} se activó localmente, pero no se " +
                        "pudo crear en Keycloak: {Error}",
                        empleado.IdEmpleado,
                        resultado.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al provisionar el usuario {IdEmpleado} en Keycloak.",
                    empleado.IdEmpleado);
            }
        }

        private async Task<(Empleado? Empleado, string? Error)> BuscarPorTokenAsync(
            string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (null, MensajeTokenInvalido);
            }

            Empleado? empleado =
                await Repositorio.ObtenerPorTokenActivacionAsync(token.Trim());

            if (empleado is null || empleado.TokenUsado)
            {
                return (null, MensajeTokenInvalido);
            }

            if (empleado.Estado == EstadoEmpleado.Activo)
            {
                return (null, MensajeNoPendiente);
            }

            if (empleado.Estado != EstadoEmpleado.PendienteActivacion)
            {
                return (null, MensajeTokenInvalido);
            }

            if (empleado.TokenExpira is null
                || empleado.TokenExpira.Value < DateTime.Now)
            {
                return (null, MensajeTokenVencido);
            }

            return (empleado, null);
        }
    }
}

