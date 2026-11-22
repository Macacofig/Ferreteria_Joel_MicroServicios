using System.ComponentModel.DataAnnotations;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Envoltorio propio del front para la API de productos. A diferencia de
    /// usuarios, este microservicio devuelve JSON directo en éxito (un objeto o
    /// un array) y <c>{ "mensaje": "..." }</c> en error, por lo que no se usa
    /// <see cref="RespuestaApi{T}"/>. La forma de la clase se mantiene similar
    /// para que las Razor Pages manejen exitoso/mensaje/datos de igual modo.
    /// </summary>
    public class ResultadoApi<T>
    {
        public bool Exitoso { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public T? Datos { get; set; }

        public List<string> Errores { get; set; } = new();
    }

    /// <summary>Producto con las denominaciones que muestra el front.</summary>
    public class ProductoApi
    {
        public int IdProducto { get; set; }

        public short IdCategoria { get; set; }

        public short IdMarca { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public string UnidadMedida { get; set; } = string.Empty;

        public decimal PrecioVenta { get; set; }

        public int CantidadStock { get; set; }

        public byte Estado { get; set; }

        public string CategoriaNombre { get; set; } = string.Empty;

        public string MarcaNombre { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }

    public class CategoriaApi
    {
        public short IdCategoria { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public byte Estado { get; set; }
    }

    public class MarcaApi
    {
        public short IdMarca { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? PaisOrigen { get; set; }

        public byte Estado { get; set; }
    }

    public class HistoricoProductoApi
    {
        public int IdHistorico { get; set; }

        public int IdProducto { get; set; }

        public string Accion { get; set; } = string.Empty;

        public decimal? PrecioAnterior { get; set; }

        public decimal? PrecioNuevo { get; set; }

        public int? StockAnterior { get; set; }

        public int? StockNuevo { get; set; }

        public string? Detalle { get; set; }

        public DateTime FechaCambio { get; set; }

        public int? IdEmpleadoResponsable { get; set; }
    }

    /// <summary>Cuerpo compartido por el alta y la edición de productos.</summary>
    public class GuardarProductoApi
    {
        [Range(1, short.MaxValue, ErrorMessage = "Seleccione una categoría.")]
        public short IdCategoria { get; set; }

        [Range(1, short.MaxValue, ErrorMessage = "Seleccione una marca.")]
        public short IdMarca { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "El código debe tener entre 3 y 30 caracteres.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
        [StringLength(30, ErrorMessage = "La unidad de medida no puede superar los 30 caracteres.")]
        public string UnidadMedida { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El precio de venta debe ser mayor a cero.")]
        public decimal PrecioVenta { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de stock no puede ser negativa.")]
        public int CantidadStock { get; set; }
    }

    public class CrearProductoRespuestaApi
    {
        public int IdProducto { get; set; }

        public string Mensaje { get; set; } = string.Empty;
    }

    /// <summary>Cuerpo compartido por el alta y la edición de categorías.</summary>
    public class GuardarCategoriaApi
    {
        [Required(ErrorMessage = "El código es obligatorio.")]
        [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "La descripción no puede superar los 255 caracteres.")]
        public string? Descripcion { get; set; }
    }

    /// <summary>Cuerpo compartido por el alta y la edición de marcas.</summary>
    public class GuardarMarcaApi
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(80, ErrorMessage = "El país de origen no puede superar los 80 caracteres.")]
        public string? PaisOrigen { get; set; }
    }

    public class CrearCategoriaRespuestaApi
    {
        public short IdCategoria { get; set; }

        public string Mensaje { get; set; } = string.Empty;
    }

    public class CrearMarcaRespuestaApi
    {
        public short IdMarca { get; set; }

        public string Mensaje { get; set; } = string.Empty;
    }

    public class MensajeApi
    {
        public string Mensaje { get; set; } = string.Empty;
    }
}
