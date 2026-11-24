using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Personas de contacto de un cliente (segunda tabla del microservicio).
    /// Toda la información se obtiene por el ApiGateway; la página solo
    /// coordina los casos de uso a través de los adaptadores.
    /// </summary>
    [Authorize(Roles = RolesSistema.Personal)]
    public class ClienteContactosModel : PageModel
    {
        private readonly IServicioContactos _contactos;
        private readonly IServicioClientes _clientes;
        private readonly ILogger<ClienteContactosModel> _logger;

        public ClienteContactosModel(
            IServicioContactos contactos,
            IServicioClientes clientes,
            ILogger<ClienteContactosModel> logger)
        {
            _contactos = contactos;
            _clientes = clientes;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int IdCliente { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SoloActivos { get; set; } = true;

        [BindProperty]
        public ContactoSolicitudApi Solicitud { get; set; } = new();

        public List<ContactoApi> Contactos { get; private set; } = new();

        public ClienteApi? Cliente { get; private set; }

        public bool EsAdministrador => User.IsInRole(RolesSistema.Administrador);

        public async Task OnGetAsync()
        {
            ViewData["ActiveMenu"] = "Clientes";

            if (IdCliente <= 0)
            {
                TempData["MensajeError"] = "Seleccione un cliente para ver sus contactos.";
                return;
            }

            string? token = ObtenerToken();

            RespuestaApi<ClienteApi> clienteResp = await _clientes.ObtenerAsync(token, IdCliente);
            if (clienteResp.Exitoso)
            {
                Cliente = clienteResp.Datos;
            }
            else
            {
                TempData["MensajeError"] = string.IsNullOrWhiteSpace(clienteResp.Mensaje)
                    ? "No se pudo obtener el cliente solicitado."
                    : clienteResp.Mensaje;
            }

            RespuestaApi<List<ContactoApi>> respuesta = await _contactos.ListarAsync(
                token, IdCliente, SoloActivos);

            if (respuesta.Exitoso && respuesta.Datos is not null)
            {
                Contactos = respuesta.Datos;
            }
            else if (!respuesta.Exitoso)
            {
                TempData["MensajeError"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "No se pudo cargar el listado de contactos."
                    : respuesta.Mensaje;
            }
        }

        /// <summary>Alta de un contacto para el cliente seleccionado.</summary>
        public async Task<IActionResult> OnPostCrearAsync()
        {
            if (IdCliente <= 0)
            {
                TempData["MensajeError"] = "Seleccione un cliente antes de registrar contactos.";
                return RedirectToPage("/Clientes");
            }

            await EjecutarAsync(
                () => _contactos.CrearAsync(ObtenerToken(), IdCliente, Solicitud),
                "No se pudo registrar el contacto.");

            Solicitud = new ContactoSolicitudApi();
            return Redireccionar();
        }

        /// <summary>Edición de un contacto activo.</summary>
        public async Task<IActionResult> OnPostActualizarAsync(int idContacto)
        {
            await EjecutarAsync(
                () => _contactos.ActualizarAsync(ObtenerToken(), idContacto, Solicitud),
                "No se pudo actualizar el contacto.");

            Solicitud = new ContactoSolicitudApi();
            return Redireccionar();
        }

        /// <summary>Baja lógica del contacto (solo administrador).</summary>
        public async Task<IActionResult> OnPostBajaAsync(int idContacto)
        {
            await EjecutarAsync(
                () => _contactos.DarDeBajaAsync(ObtenerToken(), idContacto),
                "No se pudo dar de baja al contacto.");

            return Redireccionar();
        }

        /// <summary>Reactivación de un contacto dado de baja (solo administrador).</summary>
        public async Task<IActionResult> OnPostReactivarAsync(int idContacto)
        {
            await EjecutarAsync(
                () => _contactos.ReactivarAsync(ObtenerToken(), idContacto),
                "No se pudo reactivar al contacto.");

            return Redireccionar();
        }

        private IActionResult Redireccionar() =>
            RedirectToPage(new { idCliente = IdCliente, soloActivos = SoloActivos });

        private async Task EjecutarAsync(
            Func<Task<RespuestaApi<object>>> operacion,
            string mensajeError)
        {
            try
            {
                RespuestaApi<object> resultado = await operacion();
                TempData[resultado.Exitoso ? "Mensaje" : "MensajeError"] =
                    string.IsNullOrWhiteSpace(resultado.Mensaje) ? mensajeError : resultado.Mensaje;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al operar sobre un contacto de cliente.");
                TempData["MensajeError"] = mensajeError;
            }
        }

        private async Task EjecutarAsync(
            Func<Task<RespuestaApi<ContactoApi>>> operacion,
            string mensajeError)
        {
            try
            {
                RespuestaApi<ContactoApi> resultado = await operacion();
                TempData[resultado.Exitoso ? "Mensaje" : "MensajeError"] =
                    string.IsNullOrWhiteSpace(resultado.Mensaje) ? mensajeError : resultado.Mensaje;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar un contacto de cliente.");
                TempData["MensajeError"] = mensajeError;
            }
        }

        private string? ObtenerToken() =>
            HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);
    }
}
