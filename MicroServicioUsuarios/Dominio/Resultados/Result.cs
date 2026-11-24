namespace FERRETERIA__Joel.Dominio.Resultados
{
    /// <summary>
    /// Patrón <em>Result</em> del dominio. Los casos de uso no lanzan
    /// excepciones para errores esperados (credenciales inválidas, cuenta
    /// inactiva, token vencido, nombre de usuario agotado): devuelven un
    /// <see cref="Result"/> con el motivo o con un mensaje de éxito.
    /// </summary>
    public class Result
    {
        protected Result(bool esExitoso, string error, string mensaje)
        {
            EsExitoso = esExitoso;
            Error = error;
            Mensaje = mensaje;
        }

        /// <summary>Indica si la operación se completó correctamente.</summary>
        public bool EsExitoso { get; }

        /// <summary>Indica si la operación falló.</summary>
        public bool EsFallo => !EsExitoso;

        /// <summary>Motivo del fallo; cadena vacía cuando la operación tuvo éxito.</summary>
        public string Error { get; }

        /// <summary>Mensaje de éxito; cadena vacía cuando la operación falló.</summary>
        public string Mensaje { get; }

        public static Result Ok() => new(true, string.Empty, string.Empty);

        public static Result OkConMensaje(string mensaje) =>
            new(true, string.Empty, mensaje);

        public static Result Fallo(string error) => new(false, error, string.Empty);

        public static Result<T> Ok<T>(T valor) =>
            new(valor, true, string.Empty, string.Empty);

        public static Result<T> OkConMensaje<T>(T valor, string mensaje) =>
            new(valor, true, string.Empty, mensaje);

        public static Result<T> Fallo<T>(string error) =>
            new(default, false, error, string.Empty);
    }

    /// <summary>Resultado que además transporta un valor cuando es exitoso.</summary>
    public sealed class Result<T> : Result
    {
        internal Result(T? valor, bool esExitoso, string error, string mensaje)
            : base(esExitoso, error, mensaje)
        {
            Valor = valor;
        }

        /// <summary>Valor de la operación; <c>default</c> cuando falló.</summary>
        public T? Valor { get; }
    }
}
