namespace MicroServicioClientes.Aplicacion.Modelos;

/// <summary>Datos que envía el front para registrar o editar un contacto.</summary>
public sealed record ContactoSolicitud
{
    public string Nombre { get; init; } = string.Empty;

    public string? Cargo { get; init; }

    public string? Telefono { get; init; }

    public string? Correo { get; init; }

    /// <summary>Quita espacios sobrantes y convierte los textos vacíos opcionales en nulos.</summary>
    public ContactoSolicitud Normalizada() => this with
    {
        Nombre = (Nombre ?? string.Empty).Trim(),
        Cargo = Opcional(Cargo),
        Telefono = Opcional(Telefono),
        Correo = Opcional(Correo)
    };

    private static string? Opcional(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
