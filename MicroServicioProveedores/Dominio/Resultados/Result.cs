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

public class Result
{
    public bool EsExitoso { get; }
    public bool EsFallo => !EsExitoso;
    public ErrorResultado? Error { get; }

    protected Result(bool esExitoso, ErrorResultado? error)
    {
        EsExitoso = esExitoso;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fallo(ErrorResultado error) => new(false, error);

    public static Result<T> Ok<T>(T valor) => new(valor, true, null);
    public static Result<T> Fallo<T>(ErrorResultado error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    public T? Valor { get; }

    internal Result(T? valor, bool esExitoso, ErrorResultado? error) : base(esExitoso, error)
    {
        Valor = valor;
    }
}
