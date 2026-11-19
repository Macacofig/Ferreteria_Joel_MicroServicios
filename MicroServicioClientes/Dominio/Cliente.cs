namespace MicroServicioClientes.Dominio
{
    /// <summary>Cliente de la ferretería.</summary>
    public record Cliente(
        int Id,
        string Nombre,
        string Documento,
        string Correo,
        string Telefono);
}
