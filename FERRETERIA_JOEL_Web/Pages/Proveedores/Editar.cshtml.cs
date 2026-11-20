using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Proveedores;

[Authorize(Roles = RolesSistema.Administrador)]
public class EditarModel : PageModel
{
    private readonly IServicioProveedores _proveedores;
    private readonly ILogger<EditarModel> _logger;

    public EditarModel(IServicioProveedores proveedores, ILogger<EditarModel> logger)
    {
        _proveedores = proveedores;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public ProveedorSolicitud Formulario { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ViewData["ActiveMenu"] = "Proveedores";
        Id = id;
        return await CargarFormularioAsync(id, mostrarErrorComoTempData: true);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["ActiveMenu"] = "Proveedores";

        try
        {
            var respuesta = await _proveedores.ActualizarAsync(ObtenerToken(), Id, Formulario);
            if (respuesta.Exitoso)
            {
                TempData["Mensaje"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "Proveedor actualizado correctamente."
                    : respuesta.Mensaje;
                return RedirectToPage("./Index");
            }

            AplicarErrores(respuesta);
            TempData["MensajeError"] = MensajeError(respuesta, "No se pudo actualizar el proveedor.");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar proveedor {ProveedorId}.", Id);
            TempData["MensajeError"] = "Ocurrió un error inesperado al actualizar el proveedor.";
            return Page();
        }
    }

    private async Task<IActionResult> CargarFormularioAsync(int id, bool mostrarErrorComoTempData)
    {
        try
        {
            var respuesta = await _proveedores.ObtenerAsync(ObtenerToken(), id);
            if (respuesta.Exitoso && respuesta.Datos is not null)
            {
                Formulario = new ProveedorSolicitud
                {
                    RazonSocial = respuesta.Datos.RazonSocial,
                    Nit = respuesta.Datos.Nit,
                    Contacto = respuesta.Datos.Contacto,
                    Telefono = respuesta.Datos.Telefono
                };
                return Page();
            }

            string mensaje = MensajeError(respuesta, "No se pudo cargar los datos del proveedor.");
            if (mostrarErrorComoTempData)
            {
                TempData["MensajeError"] = mensaje;
                return RedirectToPage("./Index");
            }

            ModelState.AddModelError(string.Empty, mensaje);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al cargar proveedor {ProveedorId}.", id);
            if (mostrarErrorComoTempData)
            {
                TempData["MensajeError"] = "Ocurrió un error inesperado al cargar el proveedor.";
                return RedirectToPage("./Index");
            }

            ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al cargar el proveedor.");
            return Page();
        }
    }

    private void AplicarErrores(RespuestaApi<ProveedorDto> respuesta)
    {
        foreach (var kv in respuesta.ErroresCampo)
        {
            string clave = kv.Key switch
            {
                nameof(ProveedorDto.RazonSocial) => "Formulario.RazonSocial",
                nameof(ProveedorDto.Nit) => "Formulario.Nit",
                nameof(ProveedorDto.Contacto) => "Formulario.Contacto",
                nameof(ProveedorDto.Telefono) => "Formulario.Telefono",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(clave))
            {
                ModelState.AddModelError(clave, kv.Value);
            }
            else
            {
                ModelState.AddModelError(string.Empty, kv.Value);
            }
        }
    }

    private string? ObtenerToken() => HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);

    private static string MensajeError<T>(RespuestaApi<T> respuesta, string defecto)
    {
        if (!string.IsNullOrWhiteSpace(respuesta.Mensaje)) return respuesta.Mensaje;
        return respuesta.Errores.FirstOrDefault() ?? defecto;
    }
}
