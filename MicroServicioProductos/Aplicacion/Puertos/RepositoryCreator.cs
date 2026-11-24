namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>
/// Creador abstracto del patrón Factory Method. Vive en Aplicación para que
/// los casos de uso dependan del creador y no de la infraestructura concreta:
/// cada creador concreto decide qué adaptador de persistencia devolver.
/// </summary>
/// <typeparam name="TRepository">Contrato de repositorio solicitado.</typeparam>
public abstract class RepositoryCreator<TRepository>
{
    /// <summary>Método de fábrica implementado por los creadores concretos.</summary>
    public abstract TRepository CreateRepository();
}
