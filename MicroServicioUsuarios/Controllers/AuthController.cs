using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Aplicacion.Servicios;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FERRETERIA__Joel.ServicioUsuarios.Controllers
{
    /// <summary>
    /// Autenticación y sesión. El token lo emite Keycloak: este controlador
    /// delega la verificación de credenciales en el proveedor de identidad y
    /// devuelve el JWT firmado por el realm. El front Razor es el único
    /// cliente: guarda el token en su sesión de servidor y nunca lo expone a
    /// JavaScript ni a localStorage.
    /// </summary>
    [ApiController]
    [Route("api/usuarios")]
    public class AuthController : ControllerBase
    {
        private readonly ServicioInicioSesion _inicioSesion;
        private readonly ServicioBitacora _bitacora;
        private readonly ServicioPersonal _personal;
        private readonly IRevocacionTokens _revocacion;
        private readonly ILogger<AuthController> _logger;
        private readonly IContextoUsuario _contextoUsuario;

        public AuthController(
            ServicioInicioSesion inicioSesion,
            ServicioBitacora bitacora,
            ServicioPersonal personal,
            IRevocacionTokens revocacion,
            ILogger<AuthController> logger,
            IContextoUsuario contextoUsuario)
        {
            _inicioSesion = inicioSesion;
            _bitacora = bitacora;
            _personal = personal;
            _revocacion = revocacion;
            _logger = logger;
            _contextoUsuario = contextoUsuario;
        }

        /// <summary>
        /// Inicia sesión con correo o nombre de usuario y contraseña. Devuelve
        /// el JWT emitido por Keycloak y la proyección pública del usuario.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest solicitud)
        {
            if (solicitud is null
                || string.IsNullOrWhiteSpace(solicitud.CorreoElectronico)
                || string.IsNullOrWhiteSpace(solicitud.Contrasena))
            {
                return BadRequest(RespuestaApi<LoginResponse>.Error(
                    "Ingrese su correo o usuario y su contraseña."));
            }

            var resultado = await _inicioSesion.IniciarAsync(
                new InicioSesionSolicitud
                {
                    CorreoElectronico = solicitud.CorreoElectronico,
                    Contrasena = solicitud.Contrasena
                });

            if (!resultado.Exitoso
                || resultado.Usuario is null
                || string.IsNullOrWhiteSpace(resultado.Token))
            {
                try
                {
                    await _bitacora.RegistrarLoginFallidoAsync(
                        solicitud.CorreoElectronico);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo registrar el intento fallido de inicio de sesión.");
                }
                return Unauthorized(RespuestaApi<LoginResponse>.Error(
                    resultado.Mensaje));
            }

            var usuario = resultado.Usuario;

            try
            {
                await _bitacora.RegistrarLoginAsync(
                    usuario.Id,
                    usuario.NombreCompleto,
                    usuario.Rol);
            }
            catch (Exception ex)
            {
                // La bitácora no debe impedir el inicio de sesión, pero sí
                // quedar registrada en el log de la aplicación.
                _logger.LogError(ex, "No se pudo registrar el login en la bitácora.");
            }

            Empleado? empleado = await _personal
                .ObtenerPorIdAsync(usuario.Id);

            LoginResponse respuesta = new()
            {
                Token = resultado.Token!,
                Expira = resultado.Expira,
                Usuario = empleado is not null
                    ? MapeosDto.AUsuario(empleado)
                    : new UsuarioDto
                    {
                        IdEmpleado = usuario.Id,
                        Nombre = usuario.NombreCompleto,
                        NombreUsuario = usuario.NombreUsuario,
                        Rol = usuario.Rol,
                        CorreoElectronico = usuario.CorreoElectronico
                    }
            };

            return Ok(RespuestaApi<LoginResponse>.Ok(respuesta, resultado.Mensaje));
        }

        /// <summary>
        /// Cierra la sesión: revoca el token actual (por su <c>jti</c>) y
        /// registra el evento. Keycloak emite el token; la revocación del
        /// acceso se controla en este proceso.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            string? jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            DateTime expira = DateTime.UtcNow.AddHours(1);

            if (Request.Headers.TryGetValue("Authorization", out var encabezado))
            {
                string valor = encabezado.ToString();

                if (valor.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        JwtSecurityToken token = new JwtSecurityTokenHandler()
                            .ReadJwtToken(valor["Bearer ".Length..].Trim());

                        expira = token.ValidTo == DateTime.MinValue
                            ? expira
                            : token.ValidTo;
                    }
                    catch
                    {
                        // Si el token no se puede leer, se revoca por una hora.
                    }
                }
            }

            _revocacion.Revocar(jti, expira);

            try
            {
                await _bitacora.RegistrarLogoutAsync(
                    _contextoUsuario.IdEmpleado,
                    User.FindFirstValue(ClaimTypes.Name),
                    User.FindFirstValue(ClaimTypes.Role) ?? RolesSistema.EmpleadoVenta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo registrar el logout en la bitácora.");
            }

            return Ok(RespuestaApi<object>.Ok(new { }, "Sesión cerrada."));
        }

        /// <summary>
        /// Devuelve la identidad actual releyendo el estado de la cuenta. Si la
        /// cuenta fue dada de baja, la respuesta es 401 y el front invalida su
        /// cookie: el bloqueo es inmediato aunque el JWT siga vigente.
        /// </summary>
        
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        int id = _contextoUsuario.IdEmpleado;

        Console.WriteLine($"[ME] IdEmpleado: {id}");
        Console.WriteLine("[ME] Claims del usuario:");

        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"[ME] {claim.Type}: {claim.Value}");
        }

        if (id <= 0)
        {
            return Unauthorized(RespuestaApi<UsuarioDto>.Error(
                "La sesión no es válida."));
        }

        Empleado? empleado =
            await _personal.ObtenerPorIdAsync(id);

        if (empleado is null || empleado.Estado != EstadoEmpleado.Activo)
        {
            return Unauthorized(RespuestaApi<UsuarioDto>.Error(
                "La cuenta no está activa."));
        }

        return Ok(RespuestaApi<UsuarioDto>.Ok(
            MapeosDto.AUsuario(empleado)));
    }

    }
}
