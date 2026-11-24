using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Productos;

/// <summary>
/// Catálogo administrativo de productos. El listado, la baja lógica y la
/// ordenación se resuelven siempre a través del Adapter del microservicio.
/// </summary>
[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IServicioProductos _api;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IServicioProductos api, ILogger<IndexModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    public List<ProductoApi> Productos { get; set; } = new();

    public string OrdenarPor { get; set; } = "codigo";

    public string Direccion { get; set; } = "ASC";

    public async Task OnGetAsync(string? ordenarPor, string? direccion)
    {
        ViewData["ActiveMenu"] = "Productos";
        NormalizarOrden(ordenarPor, direccion);
        await CargarAsync();
    }

    /// <summary>Refresca el listado respetando los controles de ordenación.</summary>
    public async Task<IActionResult> OnPostRefrescarAsync(
        string? ordenarPor,
        string? direccion)
    {
        NormalizarOrden(ordenarPor, direccion);
        await CargarAsync();
        return Page();
    }

    /// <summary>Da de baja un producto (baja lógica) previa confirmación.</summary>
    public async Task<IActionResult> OnPostEliminarAsync(
        int idProducto,
        string? ordenarPor,
        string? direccion)
    {
        try
        {
            ResultadoApi<MensajeApi> resultado =
                await _api.DarDeBajaAsync(idProducto);

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
            _logger.LogError(ex, "Error al dar de baja el producto {Id}.", idProducto);
            TempData["MensajeError"] =
                "No se pudo completar la eliminación. Inténtalo nuevamente.";
        }

        NormalizarOrden(ordenarPor, direccion);

        return RedirectToPage(new
        {
            ordenarPor = OrdenarPor,
            direccion = Direccion
        });
    }

    /// <summary>Reactiva un producto dado de baja (baja lógica inversa).</summary>
    public async Task<IActionResult> OnPostReactivarAsync(
        int idProducto,
        string? ordenarPor,
        string? direccion)
    {
        try
        {
            ResultadoApi<MensajeApi> resultado =
                await _api.ReactivarAsync(idProducto);

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
            _logger.LogError(ex, "Error al reactivar el producto {Id}.", idProducto);
            TempData["MensajeError"] =
                "No se pudo reactivar el producto. Inténtalo nuevamente.";
        }

        NormalizarOrden(ordenarPor, direccion);

        return RedirectToPage(new
        {
            ordenarPor = OrdenarPor,
            direccion = Direccion
        });
    }

    private void NormalizarOrden(string? ordenarPor, string? direccion)
    {
        string[] columnas =
            { "codigo", "nombre", "precio", "stock", "categoria", "marca" };

        OrdenarPor = columnas.Contains(ordenarPor?.Trim().ToLowerInvariant())
            ? ordenarPor!.Trim().ToLowerInvariant()
            : "codigo";

        Direccion = string.Equals(
            direccion,
            "DESC",
            StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";
    }

    private async Task CargarAsync()
    {
        try
        {
            ResultadoApi<List<ProductoApi>> respuesta =
                await _api.ListarAsync(OrdenarPor, Direccion, incluirInactivos: true);

            if (respuesta.Exitoso && respuesta.Datos is not null)
            {
                Productos = respuesta.Datos;
            }
            else
            {
                TempData["MensajeError"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "No se pudo cargar el listado de productos."
                    : respuesta.Mensaje;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar los productos.");
            TempData["MensajeError"] =
                "No se pudo cargar el listado de productos.";
        }
    }
}
