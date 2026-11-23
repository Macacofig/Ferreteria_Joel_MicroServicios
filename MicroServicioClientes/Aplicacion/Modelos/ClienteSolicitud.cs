namespace MicroServicioClientes.Aplicacion.Modelos;

/// <summary>Datos que envía el front para registrar o editar un cliente.</summary>
public sealed record ClienteSolicitud
{
    public string? Ci { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string Apellido { get; init; } = string.Empty;

    public string? Correo { get; init; }

    public string? Telefono { get; init; }

    public string? Direccion { get; init; }

    /// <summary>Quita espacios sobrantes y convierte los textos vacíos opcionales en nulos.</summary>
    public ClienteSolicitud Normalizada() => this with
    {
        Ci = Opcional(Ci),
        Nombre = (Nombre ?? string.Empty).Trim(),
        Apellido = (Apellido ?? string.Empty).Trim(),
        Correo = Opcional(Correo),
        Telefono = Opcional(Telefono),
        Direccion = Opcional(Direccion)
    };

    private static string? Opcional(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}