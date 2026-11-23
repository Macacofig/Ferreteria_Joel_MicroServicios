namespace MicroServicioProductos.Dominio.Entidades;

/// <summary>Producto del catálogo de la ferretería.</summary>
public sealed class Producto
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
    public byte Estado { get; set; } = 1;

    // Nombres resueltos por el repositorio para la presentación del catálogo.
    public string CategoriaNombre { get; set; } = string.Empty;
    public string MarcaNombre { get; set; } = string.Empty;

    // Auditoría.
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public int? UsuarioCreacion { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? UsuarioModificacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public int? UsuarioBaja { get; set; }
    public DateTime? FechaBaja { get; set; }
    public int? IdEmpleadoResponsable { get; set; }
}
