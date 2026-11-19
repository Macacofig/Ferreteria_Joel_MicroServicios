namespace FERRETERIA__Joel.Dominio.Resultados
{
    /// <summary>
    /// Patrón <em>Result</em> del dominio. Los casos de uso no lanzan
    /// excepciones para errores esperados (credenciales inválidas, cuenta
    /// inactiva, token vencido): devuelven un <see cref="Result"/> que indica
    /// si la operación tuvo éxito y, en caso contrario, el motivo. Así el
    /// controlador decide el código HTTP sin inspeccionar excepciones.
    /// </summary>
    public class Result
    {
        protected Result(bool esExitoso, string error)
        {
            EsExitoso = esExitoso;
            Error = error;
        }

        /// <summary>Indica si la operación se completó correctamente.</summary>
        public bool EsExitoso { get; }

        /// <summary>Indica si la operación falló.</summary>
        public bool EsFallo => !EsExitoso;

        /// <summary>Motivo del fallo; cadena vacía cuando la operación tuvo éxito.</summary>
        public string Error { get; }

        public static Result Ok() => new(true, string.Empty);

        public static Result Fallo(string error) => new(false, error);

        public static Result<T> Ok<T>(T valor) => new(valor, true, string.Empty);

        public static Result<T> Fallo<T>(string error) =>
            new(default, false, error);
    }

    /// <summary>Resultado que además transporta un valor cuando es exitoso.</summary>
    public sealed class Result<T> : Result
    {
        internal Result(T? valor, bool esExitoso, string error)
            : base(esExitoso, error)
        {
            Valor = valor;
        }

        /// <summary>Valor de la operación; <c>default</c> cuando falló.</summary>
        public T? Valor { get; }

        /// <summary>Devuelve el valor o lanza si la operación falló.</summary>
        public T ValorRequerido => EsExitoso
            ? Valor!
            : throw new InvalidOperationException(Error);
    }
}
