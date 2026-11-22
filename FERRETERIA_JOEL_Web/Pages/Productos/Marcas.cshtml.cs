using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Productos;

/// <summary>
/// Gestión de marcas en una sola vista. El alta, la edición, la baja lógica
/// y el listado se resuelven siempre a través del Adapter del microservicio
/// de productos; los mensajes del Result se muestran tal cual.
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
    [BindProperty(SupportsGet = true)]
    public short? IdEditar { get; set; }

    public List<MarcaApi> Marcas { get; set; } = new();

    public bool Editando => IdEditar.HasValue;

    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["ActiveMenu"] = "Productos";
        await CargarAsync();

        if (IdEditar.HasValue)
        {
            await CargarParaEditarAsync();
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync(
        CancellationToken cancelacion = default)
    {
        ViewData["ActiveMenu"] = "Productos";

        if (!ModelState.IsValid)
        {
            Error = PrimerErrorDeValidacion();
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

                Error = resultado.Mensaje;
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

                Error = resultado.Mensaje;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar la marca.");
            Error = Editando
                ? "No se pudo completar la edición. Inténtalo nuevamente."
                : "No se pudo completar el alta. Inténtalo nuevamente.";
        }

        await CargarAsync(cancelacion);
        return Page();
    }

    /// <summary>Abandona la edición y limpia el formulario.</summary>
    public IActionResult OnPostCancelar()
    {
        IdEditar = null;
        Solicitud = new GuardarMarcaApi();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRefrescarAsync(
        CancellationToken cancelacion = default)
    {
        ViewData["ActiveMenu"] = "Productos";
        await CargarAsync(cancelacion);

        if (IdEditar.HasValue)
        {
            await CargarParaEditarAsync(cancelacion);
        }

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

    private async Task CargarParaEditarAsync(
        CancellationToken cancelacion = default)
    {
        ResultadoApi<MarcaApi> resultado =
            await _api.ObtenerMarcaAsync(IdEditar!.Value, cancelacion);

        if (resultado.Exitoso && resultado.Datos is not null)
        {
            Solicitud = new GuardarMarcaApi
            {
                Nombre = resultado.Datos.Nombre,
                PaisOrigen = resultado.Datos.PaisOrigen
            };
            return;
        }

        Error = resultado.Mensaje;
        IdEditar = null;
    }

    private async Task CargarAsync(CancellationToken cancelacion = default)
    {
        try
        {
            ResultadoApi<List<MarcaApi>> respuesta =
                await _api.ListarMarcasAsync(cancelacion);

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
