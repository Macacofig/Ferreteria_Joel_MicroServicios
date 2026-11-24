using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioClientes.Controllers;

/// <summary>
/// Adaptador de entrada HTTP de la segunda tabla del microservicio:
/// personas de contacto de cada cliente. Acceso por la gateway en /api/clientes.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Authorize(Roles = RolesSistema.AdministradorOEmpleado)]
public sealed class ContactosController : ControllerBase
{
    private readonly IServicioContactos _servicio;

    public ContactosController(IServicioContactos servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("{idCliente:int}/contactos")]
    public async Task<IActionResult> Listar(
        int idCliente,
        [FromQuery] bool soloActivos = true,
        CancellationToken cancelacion = default) =>
        this.Adaptar(await _servicio.ListarAsync(idCliente, soloActivos, cancelacion));

    [HttpGet("contactos/{idContacto:int}")]
    public async Task<IActionResult> Obtener(
        int idContacto,
        CancellationToken cancelacion) =>
        this.Adaptar(await _servicio.ObtenerAsync(idContacto, cancelacion));

    [HttpPost("{idCliente:int}/contactos")]
    public async Task<IActionResult> Crear(
        int idCliente,
        [FromBody] ContactoSolicitud solicitud,
        CancellationToken cancelacion) =>
        this.AdaptarCreado(await _servicio.CrearAsync(idCliente, solicitud, cancelacion));

    [HttpPut("contactos/{idContacto:int}")]
    public async Task<IActionResult> Actualizar(
        int idContacto,
        [FromBody] ContactoSolicitud solicitud,
        CancellationToken cancelacion) =>
        this.Adaptar(await _servicio.ActualizarAsync(idContacto, solicitud, cancelacion));

    [HttpDelete("contactos/{idContacto:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    public async Task<IActionResult> DarDeBaja(
        int idContacto,
        CancellationToken cancelacion) =>
        this.AdaptarSinDatos(await _servicio.DarDeBajaAsync(idContacto, cancelacion));

    [HttpPost("contactos/{idContacto:int}/reactivar")]
    [Authorize(Roles = RolesSistema.Administrador)]
    public async Task<IActionResult> Reactivar(
        int idContacto,
        CancellationToken cancelacion) =>
        this.AdaptarSinDatos(await _servicio.ReactivarAsync(idContacto, cancelacion));
}
