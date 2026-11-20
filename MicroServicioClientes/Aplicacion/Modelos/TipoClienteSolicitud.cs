namespace MicroServicioClientes.Aplicacion.Modelos;

/// <summary>Datos para crear o editar un tipo de cliente.</summary>
public sealed record TipoClienteSolicitud
{
    public string Nombre { get; init; } = string.Empty;

    public decimal DescuentoPorcentaje { get; init; }

    public string? Descripcion { get; init; }

    public bool Activo { get; init; } = true;

    public TipoClienteSolicitud Normalizada() => this with
    {
        Nombre = (Nombre ?? string.Empty).Trim(),
        Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim()
    };
}
