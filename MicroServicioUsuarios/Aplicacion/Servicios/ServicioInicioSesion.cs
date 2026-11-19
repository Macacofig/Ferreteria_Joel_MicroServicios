using System.Collections.Concurrent;
using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Resultados;
using FERRETERIA__Joel.Dominio.Utilidades;
using FERRETERIA__Joel.Infraestructura.Factories;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Inicio de sesión exclusivo para empleados. La contraseña ya no se
    /// verifica contra MySQL: la autenticación se delega al proveedor de
    /// identidad (Keycloak). El microservicio solo comprueba el estado de la
    /// cuenta en la tabla <c>empleado</c> y pide el token a Keycloak; la firma
    /// de ese token se valida después mediante JWKS.
    /// </summary>
    public class ServicioInicioSesion
    {
        private const int IntentosPermitidos = 5;
        private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);
        private static readonly ConcurrentDictionary<string, IntentosLogin> Intentos =
            new(StringComparer.OrdinalIgnoreCase);

        private const string MensajeCredenciales =
            "Correo o contraseña incorrectos.";

        private const string MensajeFormato =
            "Ingrese un correo y una contraseña válidos.";

        private const string MensajeInactivo =
            "La cuenta está inactiva. Contacte al administrador.";

        private const string MensajePendienteActivacion =
            "La cuenta aún no está activada. Revise el enlace enviado a su correo.";

        private readonly RepositoryCreator<IRepositoryEmpleado> _creadorRepositorio;
        private readonly IProveedorIdentidad _identidad;
        private readonly ICredencialesValidaciones _credenciales;
        private IRepositoryEmpleado? _repositorio;

        public ServicioInicioSesion(
            RepositoryCreator<IRepositoryEmpleado> creadorRepositorio,
            IProveedorIdentidad identidad,
            ICredencialesValidaciones credenciales)
        {
            _creadorRepositorio = creadorRepositorio;
            _identidad = identidad;
            _credenciales = credenciales;
        }

        private IRepositoryEmpleado Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<ResultadoInicioSesion> IniciarAsync(
            InicioSesionSolicitud solicitud)
        {
            string correo = solicitud.CorreoElectronico?.Trim() ?? string.Empty;
            string contrasena = solicitud.Contrasena ?? string.Empty;

            if (EstaBloqueado(correo, out TimeSpan restante))
            {
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = $"Demasiados intentos fallidos. Intente nuevamente en {Math.Ceiling(restante.TotalMinutes)} minutos."
                };
            }

            bool identificadorValido = correo.Contains('@')
                ? _credenciales.EsCorreoValido(correo)
                : !string.IsNullOrWhiteSpace(correo);

            if (!identificadorValido || string.IsNullOrEmpty(contrasena))
            {
                RegistrarIntentoFallido(correo);
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = MensajeFormato
                };
            }

            // Se recupera la cuenta para conocer su estado y su nombre de
            // usuario canónico (el que existe en Keycloak).
            Empleado? empleado =
                await Repositorio.ObtenerPorIdentificadorConCredencialesAsync(correo);

            if (empleado is null)
            {
                RegistrarIntentoFallido(correo);
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = MensajeCredenciales
                };
            }

            if (empleado.Estado == EstadoEmpleado.PendienteActivacion)
            {
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = MensajePendienteActivacion
                };
            }

            if (empleado.Estado != EstadoEmpleado.Activo)
            {
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = MensajeInactivo
                };
            }

            string usuarioKeycloak = !string.IsNullOrWhiteSpace(empleado.NombreUsuario)
                ? empleado.NombreUsuario!
                : empleado.CorreoElectronico ?? correo;

            Result<TokenIdentidad> autenticacion =
                await _identidad.IniciarSesionAsync(usuarioKeycloak, contrasena);

            if (autenticacion.EsFallo || autenticacion.Valor is null)
            {
                RegistrarIntentoFallido(correo);
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = string.IsNullOrWhiteSpace(autenticacion.Error)
                        ? MensajeCredenciales
                        : autenticacion.Error
                };
            }

            Intentos.TryRemove(correo, out _);

            return new ResultadoInicioSesion
            {
                Exitoso = true,
                Token = autenticacion.Valor.Token,
                RefreshToken = autenticacion.Valor.RefreshToken,
                Expira = autenticacion.Valor.Expira,
                Usuario = new SesionUsuario
                {
                    Id = empleado.IdEmpleado,
                    NombreCompleto =
                        $"{empleado.Nombre} {empleado.Apellido}".Trim(),
                    NombreUsuario = empleado.NombreUsuario,
                    CorreoElectronico =
                        empleado.CorreoElectronico ?? string.Empty,
                    Rol = RolesSistema.Normalizar(empleado.Rol)
                }
            };
        }

        private static bool EstaBloqueado(string identificador, out TimeSpan restante)
        {
            restante = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(identificador)
                || !Intentos.TryGetValue(identificador, out IntentosLogin? intento))
                return false;
            if (intento.BloqueadoHasta <= DateTime.UtcNow) return false;
            restante = intento.BloqueadoHasta - DateTime.UtcNow;
            return true;
        }

        private static void RegistrarIntentoFallido(string identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador)) return;
            var estado = Intentos.GetOrAdd(identificador, _ => new IntentosLogin());
            estado.Fallidos++;
            if (estado.Fallidos >= IntentosPermitidos)
            {
                estado.BloqueadoHasta = DateTime.UtcNow.Add(DuracionBloqueo);
            }
        }

        private sealed class IntentosLogin
        {
            public int Fallidos;
            public DateTime BloqueadoHasta;
        }
    }
}
