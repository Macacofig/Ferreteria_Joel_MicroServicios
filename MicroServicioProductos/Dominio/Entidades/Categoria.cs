namespace MicroServicioProductos.Dominio.Entidades;

/// <summary>Categoría disponible para clasificar productos.</summary>
public sealed class Categoria
{
    public short IdCategoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public byte Estado { get; set; } = 1;
}
