using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Productos;

/// <summary>Histórico de altas, actualizaciones y bajas de un producto, leído
/// desde <c>GET /api/productos/{id}/historico</c> a través del Adapter.</summary>
[Authorize]
public sealed class HistoricoModel : PageModel
{
    private readonly IServicioProductos _api;
    private readonly IServicioUsuarios _usuarios;
    private readonly ILogger<HistoricoModel> _logger;

    public HistoricoModel(
        IServicioProductos api,
        IServicioUsuarios usuarios,
        ILogger<HistoricoModel> logger)
    {
        _api = api;
        _usuarios = usuarios;
        _logger = logger;
    }

    public List<HistoricoProductoApi> Historial { get; set; } = new();

    public int IdProducto { get; set; }

    public string NombreProducto { get; set; } = "Producto";

    public string CodigoProducto { get; set; } = string.Empty;

    /// <summary>Nombres de empleados por identificador, para la columna Responsable.</summary>
    public Dictionary<int, string> Responsables { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ViewData["ActiveMenu"] = "Productos";
        IdProducto = id;

        try
        {
            ResultadoApi<ProductoApi> producto = await _api.ObtenerAsync(id);

            if (producto.Exitoso && producto.Datos is not null)
            {
                NombreProducto = producto.Datos.Nombre;
                CodigoProducto = producto.Datos.Codigo;
            }

            ResultadoApi<List<HistoricoProductoApi>> respuesta =
                await _api.ListarHistoricoAsync(id);

            if (respuesta.Exitoso && respuesta.Datos is not null)
            {
                Historial = respuesta.Datos;
            }
            else
            {
                TempData["MensajeError"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "No se pudo cargar el histórico del producto."
                    : respuesta.Mensaje;
            }

            await CargarResponsablesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar el histórico del producto {Id}.", id);
            TempData["MensajeError"] =
                "No se pudo cargar el histórico del producto.";
        }

        return Page();
    }

    private async Task CargarResponsablesAsync()
    {
        try
        {
            string? token = HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

            RespuestaApi<List<UsuarioApi>> respuesta =
                await _usuarios.ListarAsync(token);

            if (!respuesta.Exitoso || respuesta.Datos is null)
            {
                return;
            }

            Responsables = respuesta.Datos
                .Where(empleado => !string.IsNullOrWhiteSpace(empleado.Nombre))
                .GroupBy(empleado => empleado.IdEmpleado)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => $"{grupo.First().Nombre} {grupo.First().Apellido}".Trim());
        }
        catch (Exception ex)
        {
            // Si el servicio de usuarios no está disponible, se muestra el
            // identificador del empleado en la columna Responsable.
            _logger.LogWarning(ex, "No se pudieron resolver los nombres de los responsables.");
        }
    }
}
