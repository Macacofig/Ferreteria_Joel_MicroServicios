using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioClientes.Controllers;

/// <summary>
/// CRUD de tipos de cliente. Cuelga de <c>api/clientes/tipos</c> para que la
/// gateway lo enrute con la regla existente <c>/api/clientes/**</c>.
/// Consultar lo permiten ambos roles; crear, editar y eliminar, solo el administrador.
/// </summary>
[ApiController]
[Route("api/clientes/tipos")]
[Authorize(Roles = RolesSistema.AdministradorOEmpleado)]
public sealed class TiposClienteController : ControllerBase
{
    private readonly IServicioTiposCliente _servicio;

    public TiposClienteController(IServicioTiposCliente servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] bool soloActivos = true,
        CancellationToken cancelacion = default) =>
        this.Adaptar(await _servicio.ListarAsync(soloActivos, cancelacion));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancelacion) =>
        this.Adaptar(await _servicio.ObtenerAsync(id, cancelacion));

    [HttpPost]
    [Authorize(Roles = RolesSistema.Administrador)]
    public async Task<IActionResult> Crear(
        [FromBody] TipoClienteSolicitud solicitud,
        CancellationToken cancelacion) =>
        this.AdaptarCreado(await _servicio.CrearAsync(solicitud, cancelacion));

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] TipoClienteSolicitud solicitud,
        CancellationToken cancelacion) =>
        this.Adaptar(await _servicio.ActualizarAsync(id, solicitud, cancelacion));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancelacion) =>
        this.AdaptarSinDatos(await _servicio.EliminarAsync(id, cancelacion));
}