using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Proveedores;

[Authorize(Roles = RolesSistema.Administrador)]
public class DetalleModel : PageModel
{
    private readonly IServicioProveedores _proveedores;
    private readonly ILogger<DetalleModel> _logger;

    public DetalleModel(IServicioProveedores proveedores, ILogger<DetalleModel> logger)
    {
        _proveedores = proveedores;
        _logger = logger;
    }

    public ProveedorDto? Proveedor { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ViewData["ActiveMenu"] = "Proveedores";

        try
        {
            var respuesta = await _proveedores.ObtenerAsync(ObtenerToken(), id);
            if (respuesta.Exitoso && respuesta.Datos is not null)
            {
                Proveedor = respuesta.Datos;
                return Page();
            }

            TempData["MensajeError"] = MensajeError(respuesta, "No se pudo obtener el proveedor solicitado.");
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al consultar proveedor {ProveedorId}.", id);
            TempData["MensajeError"] = "Ocurrió un error inesperado al consultar el proveedor.";
            return RedirectToPage("./Index");
        }
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

        return RedirectToPage("./Index");
    }

    private string? ObtenerToken() => HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);

    private static string MensajeError<T>(RespuestaApi<T> respuesta, string defecto)
    {
        if (!string.IsNullOrWhiteSpace(respuesta.Mensaje)) return respuesta.Mensaje;
        return respuesta.Errores.FirstOrDefault() ?? defecto;
    }
}
