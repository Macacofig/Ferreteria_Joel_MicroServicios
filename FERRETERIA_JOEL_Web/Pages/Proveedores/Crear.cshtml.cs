using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Proveedores;

[Authorize(Roles = RolesSistema.Administrador)]
public class CrearModel : PageModel
{
    private readonly IServicioProveedores _proveedores;
    private readonly ILogger<CrearModel> _logger;

    public CrearModel(IServicioProveedores proveedores, ILogger<CrearModel> logger)
    {
        _proveedores = proveedores;
        _logger = logger;
    }

    [BindProperty]
    public ProveedorSolicitud Formulario { get; set; } = new();

    public void OnGet()
    {
        ViewData["ActiveMenu"] = "Proveedores";
        Formulario.NombreResponsable = ObtenerNombreResponsable();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["ActiveMenu"] = "Proveedores";
        Formulario.NombreResponsable = ObtenerNombreResponsable();

        try
        {
            var respuesta = await _proveedores.CrearAsync(ObtenerToken(), Formulario);
            if (respuesta.Exitoso)
            {
                TempData["Mensaje"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "Proveedor registrado correctamente."
                    : respuesta.Mensaje;
                return RedirectToPage("./Index");
            }

            AplicarErrores(respuesta);
            TempData["MensajeError"] = MensajeError(respuesta, "No se pudo registrar el proveedor.");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar proveedor.");
            TempData["MensajeError"] = "Ocurrió un error inesperado al registrar el proveedor.";
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
                nameof(ProveedorDto.NombreResponsable) => "Formulario.NombreResponsable",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(clave))
            {
                ModelState.AddModelError(clave, kv.Value);
            }
        }
    }

    private string? ObtenerToken() => HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);

    private string ObtenerNombreResponsable()
        => User.FindFirst("NombreCompleto")?.Value
           ?? User.Identity?.Name
           ?? string.Empty;

    private static string MensajeError<T>(RespuestaApi<T> respuesta, string defecto)
    {
        if (!string.IsNullOrWhiteSpace(respuesta.Mensaje)) return respuesta.Mensaje;
        return respuesta.Errores.FirstOrDefault() ?? defecto;
    }
}
