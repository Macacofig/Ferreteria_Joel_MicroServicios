namespace MicroServicioClientes.Controllers;

/// <summary>Sobre común de todas las respuestas JSON de la API.</summary>
public sealed record RespuestaApi<T>(
    bool Exitoso,
    string Mensaje,
    T? Datos,
    IReadOnlyDictionary<string, string>? ErroresCampo);
