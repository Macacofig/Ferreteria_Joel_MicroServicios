using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Proveedores;

[Authorize(Roles = RolesSistema.Administrador)]
public class IndexModel : PageModel
{
    private readonly IServicioProveedores _proveedores;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IServicioProveedores proveedores, ILogger<IndexModel> logger)
    {
        _proveedores = proveedores;
        _logger = logger;
    }

    public List<ProveedorDto> Proveedores { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Orden { get; set; } = "asc";

    public async Task OnGetAsync()
    {
        ViewData["ActiveMenu"] = "Proveedores";
        await CargarAsync();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        return await CambiarEstadoAsync(
            id,
            () => _proveedores.EliminarAsync(ObtenerToken(), id),
            "Proveedor dado de baja correctamente.",
            "No se pudo dar de baja el proveedor.");
    }

    public async Task<IActionResult> OnPostReactivarAsync(int id)
    {
        return await CambiarEstadoAsync(
            id,
            () => _proveedores.ReactivarAsync(ObtenerToken(), id),
            "Proveedor reactivado correctamente.",
            "No se pudo reactivar el proveedor.");
    }

    private async Task<IActionResult> CambiarEstadoAsync(
        int id,
        Func<Task<RespuestaApi<object>>> operacion,
        string mensajePorDefecto,
        string mensajeError)
    {
        try
        {
            var respuesta = await operacion();
            if (respuesta.Exitoso)
            {
                TempData["Mensaje"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? mensajePorDefecto
                    : respuesta.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = MensajeError(respuesta, mensajeError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al cambiar el estado del proveedor {ProveedorId}.", id);
            TempData["MensajeError"] = "Ocurrió un error inesperado al cambiar el estado del proveedor.";
        }

        return RedirectToPage(new { orden = NormalizarOrden(Orden) });
    }

    private async Task CargarAsync()
    {
        Orden = NormalizarOrden(Orden);

        try
        {
            var respuesta = await _proveedores.ListarAsync(ObtenerToken(), Orden);
            if (respuesta.Exitoso && respuesta.Datos is not null)
            {
                Proveedores = respuesta.Datos;
            }
            else
            {
                TempData["MensajeError"] = MensajeError(respuesta, "No se pudo cargar el listado de proveedores.");
                Proveedores = new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al listar proveedores.");
            TempData["MensajeError"] = "Ocurrió un error inesperado al cargar los proveedores.";
            Proveedores = new();
        }
    }

    private string? ObtenerToken() => HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);

    private static string NormalizarOrden(string? orden)
        => string.Equals(orden, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

    private static string MensajeError<T>(RespuestaApi<T> respuesta, string defecto)
    {
        if (!string.IsNullOrWhiteSpace(respuesta.Mensaje)) return respuesta.Mensaje;
        return respuesta.Errores.FirstOrDefault() ?? defecto;
    }
}
