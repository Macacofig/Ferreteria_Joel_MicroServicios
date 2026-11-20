using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioClientes.Controllers;

/// <summary>
/// Adaptador de entrada HTTP de clientes. Solo traduce la petición a un caso
/// de uso y el Resultado a una respuesta; no contiene reglas de negocio.
/// El acceso llega por la gateway: <c>/api/clientes</c>.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Authorize(Roles = RolesSistema.AdministradorOEmpleado)]
public sealed class ClientesController : ControllerBase
{
    private readonly IServicioClientes _servicio;

    public ClientesController(IServicioClientes servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Listado con búsqueda, filtro por tipo y ordenación.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? buscar,
        [FromQuery] int? idTipoCliente,
        [FromQuery] bool soloActivos = true,
        [FromQuery] string? ordenarPor = null,
        [FromQuery] bool descendente = false,
        CancellationToken cancelacion = default)
    {
        var filtro = new FiltroClientes(buscar, idTipoCliente, soloActivos, ordenarPor, descendente);

        return this.Adaptar(await _servicio.ListarAsync(filtro, cancelacion));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancelacion) =>
        this.Adaptar(await _servicio.ObtenerAsync(id, cancelacion));

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] ClienteSolicitud solicitud,
        CancellationToken cancelacion) =>
        this.AdaptarCreado(await _servicio.CrearAsync(solicitud, cancelacion));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] ClienteSolicitud solicitud,
        CancellationToken cancelacion) =>
        this.Adaptar(await _servicio.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Baja lógica. Solo el administrador (se combina con el rol exigido a la clase).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    public async Task<IActionResult> DarDeBaja(int id, CancellationToken cancelacion) =>
        this.AdaptarSinDatos(await _servicio.DarDeBajaAsync(id, cancelacion));
}
