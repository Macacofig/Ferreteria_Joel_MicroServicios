using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Resultados;
using FERRETERIA__Joel.Infraestructura.Factories;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Edición de los datos personales de una cuenta. No permite modificar la
    /// contraseña desde el formulario genérico; audita quién realizó el cambio
    /// en <c>IdEmpleadoResponsable</c> y conserva el estado y la activación.
    /// Los datos también se reflejan en Keycloak.
    /// </summary>
    public class ServicioEdicionEmpleado
    {
        private readonly RepositoryCreator<IRepositoryEmpleado> _creadorRepositorio;
        private readonly RegistroCredencialesValidador _validador;
        private readonly IProveedorIdentidad _identidad;
        private readonly ILogger<ServicioEdicionEmpleado> _logger;
        private IRepositoryEmpleado? _repositorio;

        public ServicioEdicionEmpleado(
            RepositoryCreator<IRepositoryEmpleado> creadorRepositorio,
            RegistroCredencialesValidador validador,
            IProveedorIdentidad identidad,
            ILogger<ServicioEdicionEmpleado> logger)
        {
            _creadorRepositorio = creadorRepositorio;
            _validador = validador;
            _identidad = identidad;
            _logger = logger;
        }

        private IRepositoryEmpleado Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<ResultadoRegistro> ActualizarAsync(
            int idEmpleado,
            EdicionEmpleadoSolicitud solicitud,
            int idEmpleadoResponsable)
        {
            Empleado? actual =
                await Repositorio.ObtenerPorIdSinCredencialesAsync(idEmpleado);

            if (actual is null)
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    Errores = new List<string> { "El empleado no existe." }
                };
            }

            Dictionary<string, string> errores =
                _validador.Validar(solicitud);

            string correo = solicitud.CorreoElectronico.Trim();

            if (!errores.ContainsKey(
                    nameof(EdicionEmpleadoSolicitud.CorreoElectronico))
                && !string.Equals(
                    correo,
                    actual.CorreoElectronico,
                    StringComparison.OrdinalIgnoreCase)
                && await Repositorio.ExistePorCorreoAsync(correo))
            {
                errores[nameof(EdicionEmpleadoSolicitud.CorreoElectronico)] =
                    "El correo ya está registrado.";
            }

            if (errores.Any())
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    ErroresCampo = errores
                };
            }

            actual.Nombre = solicitud.Nombre.Trim();
            actual.Apellido = solicitud.Apellido.Trim();
            actual.Telefono = solicitud.Telefono.Trim();
            actual.CorreoElectronico = correo;
            actual.Rol = RolesSistema.Normalizar(solicitud.Rol);

            bool aplicado = await Repositorio.ActualizarDatosAsync(
                actual,
                idEmpleadoResponsable);

            if (!aplicado)
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    Errores = new List<string>
                    {
                        "No se pudo actualizar el empleado."
                    }
                };
            }

            await SincronizarConKeycloakAsync(actual);

            return new ResultadoRegistro
            {
                Exitoso = true,
                Id = idEmpleado,
                NombreUsuario = actual.NombreUsuario
            };
        }

        private async Task SincronizarConKeycloakAsync(Empleado empleado)
        {
            if (string.IsNullOrWhiteSpace(empleado.NombreUsuario))
            {
                return;
            }

            try
            {
                Result resultado = await _identidad.ActualizarUsuarioAsync(
                    empleado.NombreUsuario,
                    new SolicitudIdentidad
                    {
                        NombreUsuario = empleado.NombreUsuario,
                        CorreoElectronico = empleado.CorreoElectronico ?? string.Empty,
                        NombreCompleto =
                            $"{empleado.Nombre} {empleado.Apellido}".Trim(),
                        Rol = RolesSistema.Normalizar(empleado.Rol)
                    });

                if (resultado.EsFallo)
                {
                    _logger.LogWarning(
                        "No se pudo sincronizar el empleado {IdEmpleado} con Keycloak: {Error}",
                        empleado.IdEmpleado,
                        resultado.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al sincronizar el empleado {IdEmpleado} con Keycloak.",
                    empleado.IdEmpleado);
            }
        }
    }
}

