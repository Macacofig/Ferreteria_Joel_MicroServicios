namespace MicroServicioClientes.Dominio.Entidades;

/// <summary>Cliente de la ferretería (tabla <c>cliente</c>).</summary>
public sealed class Cliente
{
    public int IdCliente { get; set; }

    /// <summary>Cédula de identidad. Es opcional, pero única cuando se registra.</summary>
    public string? Ci { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Apellido { get; set; } = string.Empty;

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    /// <summary>La baja es lógica: nunca se borra la fila.</summary>
    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreacion { get; set; } = string.Empty;

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }
}
