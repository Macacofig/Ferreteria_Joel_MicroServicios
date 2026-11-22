namespace MicroServicioProductos.Dominio.Entidades;

/// <summary>Marca comercial de los productos del catálogo.</summary>
public sealed class Marca
{
    public short IdMarca { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? PaisOrigen { get; set; }
    public byte Estado { get; set; } = 1;
}
