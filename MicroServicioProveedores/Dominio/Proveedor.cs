namespace MicroServicioProveedores.Dominio
{
    /// <summary>Proveedor de mercadería de la ferretería.</summary>
    public record Proveedor(
        int Id,
        string RazonSocial,
        string Nit,
        string Contacto,
        string Telefono);
}
