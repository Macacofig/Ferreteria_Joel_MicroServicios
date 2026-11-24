using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Productos;

/// <summary>
/// Gestión de categorías en una sola vista. El alta y la edición se resuelven
/// en un modal de la propia página; la baja lógica y el listado siguen pasando
/// siempre por el Adapter del microservicio de productos. Los mensajes del
/// Result se muestran tal cual dentro del modal cuando falla el guardado.
/// </summary>
[Authorize]
public sealed class CategoriasModel : PageModel
{
    private readonly IServicioProductos _api;
    private readonly ILogger<CategoriasModel> _logger;

    public CategoriasModel(IServicioProductos api, ILogger<CategoriasModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    [BindProperty]
    public GuardarCategoriaApi Solicitud { get; set; } = new();

    /// <summary>Identificador en edición; vacío indica alta.</summary>
    [BindProperty]
    public short? IdEditar { get; set; }

    public List<CategoriaApi> Categorias { get; set; } = new();

    /// <summary>Código <c>CAT-XXX</c> autogenerado para el alta.</summary>
    public string SiguienteCodigo { get; private set; } = string.Empty;

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
                    await _api.ActualizarCategoriaAsync(
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
                ResultadoApi<CrearCategoriaRespuestaApi> resultado =
                    await _api.CrearCategoriaAsync(Solicitud, cancelacion);

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
            _logger.LogError(ex, "Error al guardar la categoría.");
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

    /// <summary>Da de baja la categoría previa confirmación en el modal.</summary>
    public async Task<IActionResult> OnPostEliminarAsync(
        short idCategoria,
        CancellationToken cancelacion = default)
    {
        try
        {
            ResultadoApi<MensajeApi> resultado =
                await _api.DarDeBajaCategoriaAsync(idCategoria, cancelacion);

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
            _logger.LogError(ex, "Error al dar de baja la categoría {Id}.", idCategoria);
            TempData["MensajeError"] =
                "No se pudo completar la eliminación. Inténtalo nuevamente.";
        }

        return RedirectToPage();
    }

    /// <summary>Reactiva una categoría dada de baja.</summary>
    public async Task<IActionResult> OnPostReactivarAsync(
        short idCategoria,
        CancellationToken cancelacion = default)
    {
        try
        {
            ResultadoApi<MensajeApi> resultado =
                await _api.ReactivarCategoriaAsync(idCategoria, cancelacion);

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
            _logger.LogError(ex, "Error al reactivar la categoría {Id}.", idCategoria);
            TempData["MensajeError"] =
                "No se pudo reactivar la categoría. Inténtalo nuevamente.";
        }

        return RedirectToPage();
    }

    private async Task CargarAsync(CancellationToken cancelacion = default)
    {
        await CargarSiguienteCodigoAsync(cancelacion);

        try
        {
            ResultadoApi<List<CategoriaApi>> respuesta =
                await _api.ListarCategoriasAsync(incluirInactivos: true, cancelacion);

            if (respuesta.Exitoso && respuesta.Datos is not null)
            {
                Categorias = respuesta.Datos;
            }
            else
            {
                TempData["MensajeError"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "No se pudo cargar el listado de categorías."
                    : respuesta.Mensaje;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar las categorías.");
            TempData["MensajeError"] =
                "No se pudo cargar el listado de categorías.";
        }
    }

    /// <summary>
    /// Se consulta en cada carga (alta, refresco y errores de validación) para
    /// que el campo readonly del modal siempre muestre un código vigente.
    /// </summary>
    private async Task CargarSiguienteCodigoAsync(
        CancellationToken cancelacion)
    {
        try
        {
            ResultadoApi<SiguienteCodigoApi> respuesta =
                await _api.ObtenerSiguienteCodigoCategoriaAsync(cancelacion);

            if (respuesta.Exitoso
                && respuesta.Datos is not null
                && !string.IsNullOrWhiteSpace(respuesta.Datos.Codigo))
            {
                SiguienteCodigo = respuesta.Datos.Codigo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el siguiente código de categoría.");
        }
    }

    private string? PrimerErrorDeValidacion() =>
        ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(mensaje => !string.IsNullOrWhiteSpace(mensaje));
}
