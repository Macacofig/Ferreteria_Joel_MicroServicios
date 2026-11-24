namespace MicroServicioProductos.Dominio.Resultados;

/// <summary>
/// Prefijos usados por los casos de uso para que la capa HTTP traduzca los
/// fallos de negocio a 404/409 sin inspeccionar excepciones.
/// </summary>
public static class PrefijosResultado
{
    public const string NoEncontrado = "[NO_ENCONTRADO]";
    public const string Conflicto = "[CONFLICTO]";
}
