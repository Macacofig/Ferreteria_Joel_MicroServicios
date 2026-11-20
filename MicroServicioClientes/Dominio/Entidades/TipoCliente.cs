namespace MicroServicioClientes.Dominio.Entidades;

/// <summary>Categoría comercial del cliente y su descuento (tabla <c>tipo_cliente</c>).</summary>
public sealed class TipoCliente
{
    public int IdTipoCliente { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal DescuentoPorcentaje { get; set; }

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreacion { get; set; } = string.Empty;

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }
}
