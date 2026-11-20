namespace MicroServicioClientes.Dominio.Entidades;

/// <summary>Cliente de la ferretería (tabla <c>cliente</c>).</summary>
public sealed class Cliente
{
    public int IdCliente { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>CI o NIT. Es único entre clientes.</summary>
    public string Documento { get; set; } = string.Empty;

    public string? Correo { get; set; }

    public string Telefono { get; set; } = string.Empty;

    public string? Direccion { get; set; }

    public int IdTipoCliente { get; set; }

    /// <summary>Datos del tipo, cargados por el repositorio mediante JOIN.</summary>
    public string TipoClienteNombre { get; set; } = string.Empty;

    public decimal DescuentoPorcentaje { get; set; }

    /// <summary>La baja es lógica: nunca se borra la fila.</summary>
    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreacion { get; set; } = string.Empty;

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }
}
