using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Productos;

/// <summary>Edición de productos. Carga el producto y los catálogos desde la
/// API; muestra literalmente el mensaje de validación del backend.</summary>
[Authorize]
public sealed class EditarModel : PageModel
{
    private readonly IServicioProductos _api;
    private readonly ILogger<EditarModel> _logger;

    public EditarModel(IServicioProductos api, ILogger<EditarModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    [BindProperty]
    public int IdProducto { get; set; }

    [BindProperty]
    public GuardarProductoApi Solicitud { get; set; } = new();

    public List<CategoriaApi> Categorias { get; set; } = new();

    public List<MarcaApi> Marcas { get; set; } = new();

    public List<string> UnidadesMedida { get; set; } =
        UnidadesMedidaProducto.Valores.ToList();

    public string? NombreProducto { get; private set; }

    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ViewData["ActiveMenu"] = "Productos";
        IdProducto = id;

        try
        {
            ResultadoApi<ProductoApi> respuesta = await _api.ObtenerAsync(id);

            if (!respuesta.Exitoso || respuesta.Datos is null)
            {
                TempData["MensajeError"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "No se encontró el producto solicitado."
                    : respuesta.Mensaje;
                return RedirectToPage("/Productos/Index");
            }

            ProductoApi producto = respuesta.Datos;
            NombreProducto = producto.Nombre;
            Solicitud = new GuardarProductoApi
            {
                IdCategoria = producto.IdCategoria,
                IdMarca = producto.IdMarca,
                Codigo = producto.Codigo,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                UnidadMedida = UnidadesMedidaProducto.Normalizar(producto.UnidadMedida)
                    ?? producto.UnidadMedida
                    ?? string.Empty,
                PrecioVenta = producto.PrecioVenta,
                CantidadStock = producto.CantidadStock
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar el producto {Id}.", id);
            TempData["MensajeError"] = "No se pudo cargar el producto solicitado.";
            return RedirectToPage("/Productos/Index");
        }

        await CargarCombosAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancelacion = default)
    {
        ViewData["ActiveMenu"] = "Productos";
        NombreProducto = Solicitud.Nombre;

        if (!ModelState.IsValid)
        {
            Error = ModelState.Values
                .SelectMany(entrada => entrada.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(mensaje => !string.IsNullOrWhiteSpace(mensaje));

            await CargarCombosAsync(cancelacion);
            return Page();
        }

        try
        {
            ResultadoApi<MensajeApi> resultado =
                await _api.ActualizarAsync(IdProducto, Solicitud, cancelacion);

            if (resultado.Exitoso)
            {
                TempData["Mensaje"] = resultado.Mensaje;
                return RedirectToPage("/Productos/Index");
            }

            // Mensaje exacto del Patrón Result del microservicio (400/404/409).
            Error = resultado.Mensaje;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el producto {Id}.", IdProducto);
            Error = "No se pudo completar la actualización. Inténtalo nuevamente.";
        }

        await CargarCombosAsync(cancelacion);
        return Page();
    }

    private async Task CargarCombosAsync(CancellationToken cancelacion = default)
    {
        // Si el valor guardado venía en otro caso (p. ej. "caja"), se usa la
        // opción canónica; si no está en la lista, se agrega temporalmente.
        string? unidadCanonica =
            UnidadesMedidaProducto.Normalizar(Solicitud.UnidadMedida);

        if (unidadCanonica is not null)
        {
            Solicitud.UnidadMedida = unidadCanonica;
        }

        UnidadesMedida = UnidadesMedidaProducto.ObtenerConValorActual(
            Solicitud.UnidadMedida);

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
