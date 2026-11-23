namespace MicroServicioProductos.Dominio.Entidades;

/// <summary>Marca comercial de los productos del catálogo.</summary>
public sealed class Marca
{
    public short IdMarca { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? PaisOrigen { get; set; }
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
