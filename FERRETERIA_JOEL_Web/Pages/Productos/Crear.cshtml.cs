using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Productos;

/// <summary>Alta de productos. Carga los catálogos auxiliares desde la API y
/// muestra literalmente el mensaje que devuelve el backend al fallar.</summary>
[Authorize]
public sealed class CrearModel : PageModel
{
    private readonly IServicioProductos _api;
    private readonly ILogger<CrearModel> _logger;

    public CrearModel(IServicioProductos api, ILogger<CrearModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    [BindProperty]
    public GuardarProductoApi Solicitud { get; set; } = new();

    public List<CategoriaApi> Categorias { get; set; } = new();

    public List<MarcaApi> Marcas { get; set; } = new();

    public List<string> UnidadesMedida { get; set; } =
        UnidadesMedidaProducto.Valores.ToList();

    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["ActiveMenu"] = "Productos";
        await CargarCombosAsync();
        await AsegurarCodigoAsync();
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancelacion = default)
    {
        ViewData["ActiveMenu"] = "Productos";

        if (!ModelState.IsValid)
        {
            Error = ModelState.Values
                .SelectMany(entrada => entrada.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(mensaje => !string.IsNullOrWhiteSpace(mensaje));

            await CargarCombosAsync(cancelacion);
            await AsegurarCodigoAsync(cancelacion);
            return Page();
        }

        try
        {
            ResultadoApi<CrearProductoRespuestaApi> resultado =
                await _api.CrearAsync(Solicitud, cancelacion);

            if (resultado.Exitoso)
            {
                TempData["Mensaje"] = resultado.Mensaje;
                return RedirectToPage("/Productos/Index");
            }

            // Mensaje exacto del Patrón Result del microservicio (400/409).
            Error = resultado.Mensaje;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el producto.");
            Error = "No se pudo completar el alta. Inténtalo nuevamente.";
        }

        await CargarCombosAsync(cancelacion);
        await AsegurarCodigoAsync(cancelacion);
        return Page();
    }

    /// <summary>
    /// Mantiene el código autogenerado en el formulario. Si la petición vino
    /// sin él (alta nueva o validación fallida), se solicita uno al backend.
    /// </summary>
    private async Task AsegurarCodigoAsync(
        CancellationToken cancelacion = default)
    {
        if (!string.IsNullOrWhiteSpace(Solicitud.Codigo))
        {
            return;
        }

        try
        {
            ResultadoApi<SiguienteCodigoApi> respuesta =
                await _api.ObtenerSiguienteCodigoAsync(cancelacion);

            if (respuesta.Exitoso
                && respuesta.Datos is not null
                && !string.IsNullOrWhiteSpace(respuesta.Datos.Codigo))
            {
                Solicitud.Codigo = respuesta.Datos.Codigo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el siguiente código de producto.");
        }
    }

    private async Task CargarCombosAsync(CancellationToken cancelacion = default)
    {
        try
        {
            ResultadoApi<List<CategoriaApi>> categorias =
                await _api.ListarCategoriasAsync(cancelacion);
            ResultadoApi<List<MarcaApi>> marcas =
                await _api.ListarMarcasAsync(cancelacion);

            if (categorias.Exitoso && categorias.Datos is not null)
            {
                Categorias = categorias.Datos;
            }

            if (marcas.Exitoso && marcas.Datos is not null)
            {
                Marcas = marcas.Datos;
            }

            if (!categorias.Exitoso || !marcas.Exitoso)
            {
                Error ??= "No se pudieron cargar las categorías o las marcas.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar los catálogos auxiliares.");
            Error ??= "No se pudieron cargar las categorías o las marcas.";
        }
    }
}
