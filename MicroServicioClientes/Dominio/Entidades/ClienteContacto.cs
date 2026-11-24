namespace MicroServicioClientes.Dominio.Entidades;

/// <summary>
/// Persona de contacto asociada a un cliente (tabla <c>cliente_contacto</c>).
/// La baja es lógica: la fila se conserva por trazabilidad.
/// </summary>
public sealed class ClienteContacto
{
    public int IdContacto { get; set; }

    public int IdCliente { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Cargo { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreacion { get; set; } = string.Empty;

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? UsuarioBaja { get; set; }
}
