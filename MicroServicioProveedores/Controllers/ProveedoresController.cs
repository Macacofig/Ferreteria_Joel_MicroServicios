using MicroServicioProveedores.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProveedores.Controllers
{
    /// <summary>
    /// API mínima de proveedores. El listado semilla se mantiene en memoria
    /// para que el microservicio arranque sin depender de la base de datos; su
    /// base es MongoDB (MongoProveedores).
    /// </summary>
    [ApiController]
    [Route("api/proveedores")]
    public class ProveedoresController : ControllerBase
    {
        private static readonly List<Proveedor> Proveedores = new()
        {
            new Proveedor(1, "Ferretería Central S.A.", "100200300", "Jorge Vargas", "700-11111"),
            new Proveedor(2, "Importadora Andina Ltda.", "200300400", "Lucía Paredes", "700-22222"),
            new Proveedor(3, "Distribuidora El Tornillo", "300400500", "Pedro Chávez", "700-33333")
        };

        [HttpGet]
        public ActionResult<IEnumerable<Proveedor>> Listar() => Ok(Proveedores);

        [HttpGet("{id:int}")]
        public ActionResult<Proveedor> Obtener(int id)
        {
            Proveedor? proveedor = Proveedores.FirstOrDefault(p => p.Id == id);

            return proveedor is null
                ? NotFound(new { mensaje = "Proveedor no encontrado." })
                : Ok(proveedor);
        }
    }
}
