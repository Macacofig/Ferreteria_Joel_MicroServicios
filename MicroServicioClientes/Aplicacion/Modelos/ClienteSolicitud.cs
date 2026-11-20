namespace MicroServicioClientes.Aplicacion.Modelos;

/// <summary>Datos que envía el front para registrar o editar un cliente.</summary>
public sealed record ClienteSolicitud
{
    public string Nombre { get; init; } = string.Empty;

    public string Documento { get; init; } = string.Empty;

    public string? Correo { get; init; }

    public string Telefono { get; init; } = string.Empty;

    public string? Direccion { get; init; }

    public int IdTipoCliente { get; init; }

    /// <summary>Quita espacios sobrantes y convierte los textos vacíos opcionales en nulos.</summary>
    public ClienteSolicitud Normalizada() => this with
    {
        Nombre = (Nombre ?? string.Empty).Trim(),
        Documento = (Documento ?? string.Empty).Trim(),
        Correo = string.IsNullOrWhiteSpace(Correo) ? null : Correo.Trim(),
        Telefono = (Telefono ?? string.Empty).Trim(),
        Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim()
    };
}