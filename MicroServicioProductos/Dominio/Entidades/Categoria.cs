namespace MicroServicioProductos.Dominio.Entidades;

/// <summary>Categoría disponible para clasificar productos.</summary>
public sealed class Categoria
{
    public short IdCategoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public byte Estado { get; set; } = 1;

    // Columnas de auditoría de Scripts/02-productos.sql.
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
