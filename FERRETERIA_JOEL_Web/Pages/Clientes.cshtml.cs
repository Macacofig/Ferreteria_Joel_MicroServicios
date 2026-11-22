using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Listado de clientes. Toda la información se pide a MicroServicioClientes
    /// por la gateway; la página no accede a ninguna base de datos. Administrador
    /// y EmpleadoVenta consultan y editan; solo el administrador da de baja.
    /// </summary>
    [Authorize(Roles = RolesSistema.Personal)]
    public class ClientesModel : PageModel
    {
        private readonly ClientesApiCliente _api;
        private readonly ILogger<ClientesModel> _logger;

        public ClientesModel(ClientesApiCliente api, ILogger<ClientesModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? Buscar { get; set; }

        [BindProperty(SupportsGet = true)]
        public string OrdenarPor { get; set; } = "nombre";

        [BindProperty(SupportsGet = true)]
        public bool Descendente { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IncluirInactivos { get; set; }

        public List<ClienteApi> Clientes { get; private set; } = new();

        public bool EsAdministrador => User.IsInRole(RolesSistema.Administrador);

        public async Task OnGetAsync()
        {
            ViewData["ActiveMenu"] = "Clientes";

            try
            {
                RespuestaApi<List<ClienteApi>> respuesta = await _api.ListarAsync(
                    ObtenerToken(), Buscar, !IncluirInactivos, OrdenarPor, Descendente);

                if (respuesta.Exitoso && respuesta.Datos is not null)
                {
                    Clientes = respuesta.Datos;
                }
                else
                {
                    TempData["MensajeError"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                        ? "No se pudo cargar el listado de clientes."
                        : respuesta.Mensaje;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar los clientes.");
                TempData["MensajeError"] =
                    "No se pudo conectar con el servicio de clientes. Verifique que esté en ejecución.";
            }
        }

        /// <summary>Baja lógica del cliente (solo administrador).</summary>
        public async Task<IActionResult> OnPostBajaAsync(int idCliente)
        {
            await EjecutarAsync(
                () => _api.DarDeBajaAsync(ObtenerToken(), idCliente),
                "No se pudo dar de baja al cliente. Inténtelo nuevamente.");

            return RedirectToPage();
        }

        /// <summary>Reactiva a un cliente dado de baja (solo administrador).</summary>
        public async Task<IActionResult> OnPostReactivarAsync(int idCliente)
        {
            await EjecutarAsync(
                () => _api.ReactivarAsync(ObtenerToken(), idCliente),
                "No se pudo reactivar al cliente. Inténtelo nuevamente.");

            return RedirectToPage();
        }

        private async Task EjecutarAsync(
            Func<Task<RespuestaApi<object>>> operacion,
            string mensajeError)
        {
            try
            {
                RespuestaApi<object> resultado = await operacion();

                if (resultado.Exitoso)
                {
                    TempData["Mensaje"] = resultado.Mensaje;
                }
                else
                {
                    TempData["MensajeError"] = resultado.Mensaje;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al operar sobre un cliente.");
                TempData["MensajeError"] = mensajeError;
            }
        }

        private string? ObtenerToken() =>
            HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);
    }
}
