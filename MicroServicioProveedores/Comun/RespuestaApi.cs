namespace MicroServicioProveedores.Comun;

public class RespuestaApi<T>
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public T? Datos { get; set; }
    public List<string> Errores { get; set; } = new();
    public Dictionary<string, string> ErroresCampo { get; set; } = new();

    public static RespuestaApi<T> Ok(T? datos, string mensaje = "") => new()
    {
        Exitoso = true,
        Mensaje = mensaje,
        Datos = datos
    };

    public static RespuestaApi<T> Error(string mensaje, List<string>? errores = null, Dictionary<string, string>? erroresCampo = null) => new()
    {
        Exitoso = false,
        Mensaje = mensaje,
        Errores = errores ?? new List<string>(),
        ErroresCampo = erroresCampo ?? new Dictionary<string, string>()
    };
}
