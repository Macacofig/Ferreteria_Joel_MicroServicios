using MicroServicioClientes.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioClientes.Controllers
{
    /// <summary>
    /// API mínima de clientes. El listado semilla se mantiene en memoria para
    /// que el microservicio arranque sin depender de la base de datos; su base
    /// es PostgreSQL (PostgressClientes).
    /// </summary>
    [ApiController]
    [Route("api/clientes")]
    public class ClientesController : ControllerBase
    {
        private static readonly List<Cliente> Clientes = new()
        {
            new Cliente(1, "María López", "10234567", "maria.lopez@correo.com", "700-12345"),
            new Cliente(2, "Carlos Rojas", "8765432", "carlos.rojas@correo.com", "700-67890"),
            new Cliente(3, "Ana Fernández", "5678901", "ana.fernandez@correo.com", "700-45678")
        };

        [HttpGet]
        public ActionResult<IEnumerable<Cliente>> Listar() => Ok(Clientes);

        [HttpGet("{id:int}")]
        public ActionResult<Cliente> Obtener(int id)
        {
            Cliente? cliente = Clientes.FirstOrDefault(c => c.Id == id);

            return cliente is null
                ? NotFound(new { mensaje = "Cliente no encontrado." })
                : Ok(cliente);
        }
    }
}
