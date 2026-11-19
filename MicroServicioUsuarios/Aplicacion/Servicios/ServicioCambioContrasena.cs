using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Resultados;
using FERRETERIA__Joel.Infraestructura.Factories;
using FERRETERIA__Joel.Dominio.Utilidades;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Cambio de contraseña de un empleado autenticado. Verifica la contraseña
    /// actual contra Keycloak, aplica la política (mínimo 8, mayúscula,
    /// minúscula, número y símbolo), exige que la nueva sea distinta de la
    /// actual y la actualiza en Keycloak (y en el hash local como respaldo).
    /// La contraseña anterior deja de ser válida para el siguiente inicio de
    /// sesión. Devuelve un token nuevo emitido por Keycloak.
    /// </summary>
    public class ServicioCambioContrasena
    {
        private const string MensajeActualObligatoria =
            "Debe ingresar su contraseña actual.";

        private const string MensajeActualIncorrecta =
            "La contraseña actual no es correcta.";

        private const string MensajeNuevaObligatoria =
            "Debe ingresar la nueva contraseña.";

        private const string MensajeConfirmacion =
            "La confirmación de la nueva contraseña no coincide.";

        private const string MensajeIgual =
            "La nueva contraseña debe ser diferente de la actual.";

        private static readonly string MensajePolitica =
            "La nueva contraseña debe tener entre 8 y 64 caracteres e incluir al menos una mayúscula, una minúscula, un número y un símbolo.";

        private readonly RepositoryCreator<IRepositoryEmpleado> _creadorRepositorio;
        private readonly IHasheadorContrasena _hasheador;
        private readonly IProveedorIdentidad _identidad;
        private readonly ICredencialesValidaciones _validaciones;
        private readonly IContextoUsuario _contextoUsuario;
        private readonly ILogger<ServicioCambioContrasena> _logger;
        private IRepositoryEmpleado? _repositorio;

        public ServicioCambioContrasena(
            RepositoryCreator<IRepositoryEmpleado> creadorRepositorio,
            IHasheadorContrasena hasheador,
            IProveedorIdentidad identidad,
            ICredencialesValidaciones validaciones,
            IContextoUsuario contextoUsuario,
            ILogger<ServicioCambioContrasena> logger)
        {
            _creadorRepositorio = creadorRepositorio;
            _hasheador = hasheador;
            _identidad = identidad;
            _validaciones = validaciones;
            _contextoUsuario = contextoUsuario;
            _logger = logger;
        }

        private IRepositoryEmpleado Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<ResultadoCambioContrasena> CambiarAsync(
            int idEmpleado,
            CambioContrasenaSolicitud solicitud)
        {
            Dictionary<string, string> errores = new();

            string actual = solicitud.ContrasenaActual ?? string.Empty;
            string nueva = solicitud.ContrasenaNueva ?? string.Empty;
            string confirmacion = solicitud.ConfirmarContrasena ?? string.Empty;

            if (string.IsNullOrWhiteSpace(actual))
            {
                errores[nameof(CambioContrasenaSolicitud.ContrasenaActual)] =
                    MensajeActualObligatoria;
            }

            if (string.IsNullOrWhiteSpace(nueva))
            {
                errores[nameof(CambioContrasenaSolicitud.ContrasenaNueva)] =
                    MensajeNuevaObligatoria;
            }
            else if (!_validaciones.EsContrasenaValida(nueva))
            {
                errores[nameof(CambioContrasenaSolicitud.ContrasenaNueva)] =
                    MensajePolitica;
            }

            if (!string.IsNullOrWhiteSpace(nueva)
                && !_validaciones.EsConfirmacionValida(nueva, confirmacion))
            {
                errores[nameof(CambioContrasenaSolicitud.ConfirmarContrasena)] =
                    MensajeConfirmacion;
            }

            if (errores.Any())
            {
                return new ResultadoCambioContrasena
                {
                    Exitoso = false,
                    Mensaje = "Revise los campos señalados.",
                    ErroresCampo = errores
                };
            }

            Empleado? empleado =
                await Repositorio.ObtenerPorIdConCredencialesAsync(idEmpleado);

            if (empleado is null)
            {
                return ResultadoCambioContrasena.Error(
                    "La cuenta no existe.");
            }

            string usuario = !string.IsNullOrWhiteSpace(empleado.NombreUsuario)
                ? empleado.NombreUsuario!
                : empleado.CorreoElectronico ?? idEmpleado.ToString();

            // Verificación de la contraseña actual contra Keycloak.
            Result<TokenIdentidad> verificacion =
                await _identidad.IniciarSesionAsync(usuario, actual);

            if (verificacion.EsFallo)
            {
                return new ResultadoCambioContrasena
                {
                    Exitoso = false,
                    Mensaje = MensajeActualIncorrecta,
                    ErroresCampo = new Dictionary<string, string>
                    {
                        [nameof(CambioContrasenaSolicitud.ContrasenaActual)] =
                            MensajeActualIncorrecta
                    }
                };
            }

            if (string.Equals(actual, nueva, StringComparison.Ordinal))
            {
                return new ResultadoCambioContrasena
                {
                    Exitoso = false,
                    Mensaje = MensajeIgual,
                    ErroresCampo = new Dictionary<string, string>
                    {
                        [nameof(CambioContrasenaSolicitud.ContrasenaNueva)] =
                            MensajeIgual
                    }
                };
            }

            Result actualizacion = await _identidad.EstablecerContrasenaAsync(
                usuario, nueva);

            if (actualizacion.EsFallo)
            {
                return ResultadoCambioContrasena.Error(actualizacion.Error);
            }

            // Respaldo local del hash (defensa en profundidad) y auditoría.
            try
            {
                await Repositorio.ActualizarContrasenaAsync(
                    idEmpleado,
                    _hasheador.Hashear(nueva),
                    _contextoUsuario.IdEmpleado > 0
                        ? _contextoUsuario.IdEmpleado
                        : idEmpleado);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo actualizar el hash local del empleado {IdEmpleado}.",
                    idEmpleado);
            }

            // Token nuevo con la contraseña recién definida para que la sesión
            // actual continúe sin volver a iniciar sesión.
            Result<TokenIdentidad> renovado =
                await _identidad.IniciarSesionAsync(usuario, nueva);

            return new ResultadoCambioContrasena
            {
                Exitoso = true,
                Mensaje =
                    "Contraseña actualizada correctamente. Su sesión actual continúa activa.",
                Token = renovado.EsExitoso ? renovado.Valor?.Token : null,
                Expira = renovado.EsExitoso
                    ? renovado.Valor?.Expira ?? default
                    : default
            };
        }
    }
}
