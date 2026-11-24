namespace MicroServicioProveedores.Dominio.Resultados;

public enum TipoError
{
    Ninguno = 0,
    Validacion = 1,
    NoEncontrado = 2,
    Conflicto = 3,
    NoAutorizado = 4
}

public class ErrorResultado
{
    public TipoError Tipo { get; }
    public string Mensaje { get; }
    public IReadOnlyDictionary<string, string> ErroresCampo { get; }

    private ErrorResultado(TipoError tipo, string mensaje, IDictionary<string, string>? erroresCampo = null)
    {
        Tipo = tipo;
        Mensaje = mensaje;
        ErroresCampo = erroresCampo is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(erroresCampo);
    }

    public static ErrorResultado Validacion(string mensaje, IDictionary<string, string>? erroresCampo = null)
        => new(TipoError.Validacion, mensaje, erroresCampo);

    public static ErrorResultado NoEncontrado(string mensaje)
        => new(TipoError.NoEncontrado, mensaje);

    public static ErrorResultado Conflicto(string mensaje, IDictionary<string, string>? erroresCampo = null)
        => new(TipoError.Conflicto, mensaje, erroresCampo);
}

/// <summary>
/// Resultado de caso de uso: errores esperados de negocio se representan con
/// <see cref="Error"/> y los éxitos llevan un <see cref="Mensaje"/> listo para
/// publicar en la respuesta HTTP. No se lanzan excepciones de negocio.
/// </summary>
public class Result
{
    public bool EsExitoso { get; }
    public bool EsFallo => !EsExitoso;
    public ErrorResultado? Error { get; }

    /// <summary>Mensaje de éxito; cadena vacía cuando la operación falló.</summary>
    public string Mensaje { get; }

    protected Result(bool esExitoso, ErrorResultado? error, string mensaje)
    {
        EsExitoso = esExitoso;
        Error = error;
        Mensaje = mensaje;
    }

    public static Result Ok() => new(true, null, string.Empty);

    public static Result OkConMensaje(string mensaje) =>
        new(true, null, mensaje);

    public static Result Fallo(ErrorResultado error) => new(false, error, string.Empty);

    public static Result<T> Ok<T>(T valor) => new(valor, true, null, string.Empty);

    public static Result<T> OkConMensaje<T>(T valor, string mensaje) =>
        new(valor, true, null, mensaje);

    public static Result<T> Fallo<T>(ErrorResultado error) =>
        new(default, false, error, string.Empty);
}

public sealed class Result<T> : Result
{
    public T? Valor { get; }

    internal Result(T? valor, bool esExitoso, ErrorResultado? error, string mensaje)
        : base(esExitoso, error, mensaje)
    {
        Valor = valor;
    }
}
