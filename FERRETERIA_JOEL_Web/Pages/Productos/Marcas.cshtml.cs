using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Productos;

/// <summary>
/// Gestión de marcas en una sola vista. El alta y la edición se resuelven en
/// un modal de la propia página; la baja lógica y el listado siguen pasando
/// siempre por el Adapter del microservicio de productos. Los mensajes del
/// Result se muestran tal cual dentro del modal cuando falla el guardado.
/// </summary>
[Authorize]
public sealed class MarcasModel : PageModel
{
    private readonly IServicioProductos _api;
    private readonly ILogger<MarcasModel> _logger;

    public MarcasModel(IServicioProductos api, ILogger<MarcasModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    [BindProperty]
    public GuardarMarcaApi Solicitud { get; set; } = new();

    /// <summary>Identificador en edición; vacío indica alta.</summary>
    [BindProperty]
    public short? IdEditar { get; set; }

    public List<MarcaApi> Marcas { get; set; } = new();

    /// <summary>Reabre el modal tras una validación fallida.</summary>
    public bool AbrirModal { get; private set; }

    /// <summary>Mensaje exacto devuelto por la API cuando falla el guardado.</summary>
    public string? ErrorModal { get; private set; }

    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["ActiveMenu"] = "Productos";
        await CargarAsync();
    }

    public async Task<IActionResult> OnPostGuardarAsync(
        CancellationToken cancelacion = default)
    {
        ViewData["ActiveMenu"] = "Productos";

        if (!ModelState.IsValid)
        {
            ErrorModal = PrimerErrorDeValidacion();
            AbrirModal = true;
            await CargarAsync(cancelacion);
            return Page();
        }

        try
        {
            if (IdEditar.HasValue)
            {
                ResultadoApi<MensajeApi> resultado =
                    await _api.ActualizarMarcaAsync(
                        IdEditar.Value, Solicitud, cancelacion);

                if (resultado.Exitoso)
                {
                    TempData["Mensaje"] = resultado.Mensaje;
                    return RedirectToPage();
                }

                ErrorModal = resultado.Mensaje;
            }
            else
            {
                ResultadoApi<CrearMarcaRespuestaApi> resultado =
                    await _api.CrearMarcaAsync(Solicitud, cancelacion);

                if (resultado.Exitoso)
                {
                    TempData["Mensaje"] = resultado.Mensaje;
                    return RedirectToPage();
                }

                ErrorModal = resultado.Mensaje;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar la marca.");
            ErrorModal = IdEditar.HasValue
                ? "No se pudo completar la edición. Inténtalo nuevamente."
                : "No se pudo completar el alta. Inténtalo nuevamente.";
        }

        AbrirModal = true;
        await CargarAsync(cancelacion);
        return Page();
    }

    public async Task<IActionResult> OnPostRefrescarAsync(
        CancellationToken cancelacion = default)
    {
        ViewData["ActiveMenu"] = "Productos";
        await CargarAsync(cancelacion);
        return Page();
    }

    /// <summary>Da de baja la marca previa confirmación en el modal.</summary>
    public async Task<IActionResult> OnPostEliminarAsync(
        short idMarca,
        CancellationToken cancelacion = default)
    {
        try
        {
            ResultadoApi<MensajeApi> resultado =
                await _api.DarDeBajaMarcaAsync(idMarca, cancelacion);

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
            _logger.LogError(ex, "Error al dar de baja la marca {Id}.", idMarca);
            TempData["MensajeError"] =
                "No se pudo completar la eliminación. Inténtalo nuevamente.";
        }

        return RedirectToPage();
    }

    /// <summary>Reactiva una marca dada de baja.</summary>
    public async Task<IActionResult> OnPostReactivarAsync(
        short idMarca,
        CancellationToken cancelacion = default)
    {
        try
        {
            ResultadoApi<MensajeApi> resultado =
                await _api.ReactivarMarcaAsync(idMarca, cancelacion);

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
            _logger.LogError(ex, "Error al reactivar la marca {Id}.", idMarca);
            TempData["MensajeError"] =
                "No se pudo reactivar la marca. Inténtalo nuevamente.";
        }

        return RedirectToPage();
    }

    private async Task CargarAsync(CancellationToken cancelacion = default)
    {
        try
        {
            ResultadoApi<List<MarcaApi>> respuesta =
                await _api.ListarMarcasAsync(incluirInactivos: true, cancelacion);

            if (respuesta.Exitoso && respuesta.Datos is not null)
            {
                Marcas = respuesta.Datos;
            }
            else
            {
                TempData["MensajeError"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "No se pudo cargar el listado de marcas."
                    : respuesta.Mensaje;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar las marcas.");
            TempData["MensajeError"] =
                "No se pudo cargar el listado de marcas.";
        }
    }

    private string? PrimerErrorDeValidacion() =>
        ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(mensaje => !string.IsNullOrWhiteSpace(mensaje));
}
