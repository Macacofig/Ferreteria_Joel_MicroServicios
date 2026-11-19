namespace MicroServicioProductos.Dominio
{
    /// <summary>Producto del catálogo de la ferretería.</summary>
    public record Producto(
        int Id,
        string Nombre,
        string Categoria,
        decimal Precio,
        int Existencia);
}
