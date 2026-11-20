namespace MicroServicioClientes.Dominio.Comunes;

/// <summary>Clasifica la causa de un fallo para que el adaptador HTTP elija el código adecuado.</summary>
public enum TipoError
{
    Ninguno,
    Validacion,
    NoEncontrado,
    Conflicto
}

/// <summary>
/// Patrón Result: las reglas de negocio devuelven éxito o fallo como valor,
/// sin lanzar excepciones para los casos esperados (datos inválidos, registro
/// inexistente, duplicados).
/// </summary>
public class Resultado
{
    private static readonly IReadOnlyDictionary<string, string> SinErrores =
        new Dictionary<string, string>();

    protected Resultado(
        bool exitoso,
        string mensaje,
        TipoError tipoError,
        IReadOnlyDictionary<string, string>? erroresCampo)
    {
        Exitoso = exitoso;
        Mensaje = mensaje;
        TipoError = tipoError;
        ErroresCampo = erroresCampo ?? SinErrores;
    }

    public bool Exitoso { get; }

    public string Mensaje { get; }

    public TipoError TipoError { get; }

    /// <summary>Mensaje de error por campo (clave = nombre de la propiedad).</summary>
    public IReadOnlyDictionary<string, string> ErroresCampo { get; }

    public static Resultado Exito(string mensaje = "") =>
        new(true, mensaje, TipoError.Ninguno, null);

    public static Resultado Falla(
        TipoError tipoError,
        string mensaje,
        IReadOnlyDictionary<string, string>? erroresCampo = null) =>
        new(false, mensaje, tipoError, erroresCampo);
}

/// <summary>Resultado que además transporta un valor cuando la operación tuvo éxito.</summary>
public sealed class Resultado<T> : Resultado
{
    private Resultado(
        bool exitoso,
        T? valor,
        string mensaje,
        TipoError tipoError,
        IReadOnlyDictionary<string, string>? erroresCampo)
        : base(exitoso, mensaje, tipoError, erroresCampo)
    {
        Valor = valor;
    }

    public T? Valor { get; }

    public static Resultado<T> Exito(T valor, string mensaje = "") =>
        new(true, valor, mensaje, TipoError.Ninguno, null);

    public static new Resultado<T> Falla(
        TipoError tipoError,
        string mensaje,
        IReadOnlyDictionary<string, string>? erroresCampo = null) =>
        new(false, default, mensaje, tipoError, erroresCampo);

    /// <summary>Propaga el fallo de otro resultado conservando causa y mensajes.</summary>
    public static Resultado<T> DesdeFallo(Resultado fallo) =>
        Falla(fallo.TipoError, fallo.Mensaje, fallo.ErroresCampo);
}
