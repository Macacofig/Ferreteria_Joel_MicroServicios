using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Alta y edición de clientes en una sola página (la ruta lleva el id
    /// opcional). El front solo presenta el formulario: las validaciones y las
    /// reglas de negocio se aplican en MicroServicioClientes y sus mensajes se
    /// muestran junto a cada campo.
    /// </summary>
    [Authorize(Roles = RolesSistema.Personal)]
    public class ClienteFormularioModel : PageModel
    {
        private readonly ClientesApiCliente _api;
        private readonly ILogger<ClienteFormularioModel> _logger;

        public ClienteFormularioModel(
            ClientesApiCliente api,
            ILogger<ClienteFormularioModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        [BindProperty]
        public ClienteSolicitudApi Solicitud { get; set; } = new();

        public int? IdCliente { get; private set; }

        public bool EsEdicion => IdCliente.HasValue;

        public string? MensajeGeneral { get; private set; }

        public Dictionary<string, string> ErroresCampo { get; private set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            ViewData["ActiveMenu"] = "Clientes";
            IdCliente = id;

            if (!id.HasValue)
            {
                return Page();
            }

            try
            {
                RespuestaApi<ClienteApi> respuesta = await _api.ObtenerAsync(ObtenerToken(), id.Value);

                if (!respuesta.Exitoso || respuesta.Datos is null)
                {
                    TempData["MensajeError"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                        ? "No se encontró el cliente solicitado."
                        : respuesta.Mensaje;
                    return RedirectToPage("/Clientes");
                }

                ClienteApi cliente = respuesta.Datos;

                Solicitud = new ClienteSolicitudApi
                {
                    Ci = cliente.Ci,
                    Nombre = cliente.Nombre,
                    Apellido = cliente.Apellido,
                    Correo = cliente.Correo,
                    Telefono = cliente.Telefono,
                    Direccion = cliente.Direccion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el cliente {Id}.", id);
                TempData["MensajeError"] = "No se pudo cargar el cliente. Inténtelo nuevamente.";
                return RedirectToPage("/Clientes");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            ViewData["ActiveMenu"] = "Clientes";
            IdCliente = id;

            try
            {
                string? token = ObtenerToken();

                RespuestaApi<ClienteApi> respuesta = id.HasValue
                    ? await _api.ActualizarAsync(token, id.Value, Solicitud)
                    : await _api.CrearAsync(token, Solicitud);

                if (respuesta.Exitoso)
                {
                    TempData["Mensaje"] = respuesta.Mensaje;
                    return RedirectToPage("/Clientes");
                }

                MensajeGeneral = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "No se pudo guardar el cliente."
                    : respuesta.Mensaje;

                ErroresCampo = new Dictionary<string, string>(
                    respuesta.ErroresCampo, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar el cliente.");
                MensajeGeneral =
                    "No se pudo conectar con el servicio de clientes. Verifique que esté en ejecución.";
            }

            return Page();
        }

        private string? ObtenerToken() =>
            HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);
    }
}
