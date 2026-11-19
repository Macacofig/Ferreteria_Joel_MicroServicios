using MicroServicioProductos.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers
{
    /// <summary>
    /// API mínima del catálogo de productos. El listado semilla se mantiene en
    /// memoria para que el microservicio arranque sin depender de la base de
    /// datos; su base es MySQL (MysqlProductos).
    /// </summary>
    [ApiController]
    [Route("api/productos")]
    public class ProductosController : ControllerBase
    {
        private static readonly List<Producto> Catalogo = new()
        {
            new Producto(1, "Martillo de acero 16 oz", "Herramientas", 45.50m, 25),
            new Producto(2, "Taladro percutor 650 W", "Herramientas", 320.00m, 10),
            new Producto(3, "Cemento gris 50 kg", "Construcción", 55.75m, 80),
            new Producto(4, "Pintura látex blanca 1 galón", "Pintura", 89.90m, 40)
        };

        [HttpGet]
        public ActionResult<IEnumerable<Producto>> Listar() => Ok(Catalogo);

        [HttpGet("{id:int}")]
        public ActionResult<Producto> Obtener(int id)
        {
            Producto? producto = Catalogo.FirstOrDefault(p => p.Id == id);

            return producto is null
                ? NotFound(new { mensaje = "Producto no encontrado." })
                : Ok(producto);
        }
    }
}
