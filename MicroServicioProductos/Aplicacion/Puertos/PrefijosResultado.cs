namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>
/// Prefijos que permiten a los adaptadores HTTP distinguir 404 y 409 del resto
/// de validaciones sin inspeccionar excepciones. Se comparten entre todos los
/// servicios de Aplicación.
/// </summary>
public static class PrefijosResultado
{
    public const string NoEncontrado = "[NO_ENCONTRADO]";
    public const string Conflicto = "[CONFLICTO]";
}
