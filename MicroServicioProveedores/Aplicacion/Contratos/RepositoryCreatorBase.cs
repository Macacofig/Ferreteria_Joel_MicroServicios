namespace MicroServicioProveedores.Aplicacion.Contratos;

/// <summary>
/// Creador abstracto del patrón Factory Method. Vive en Aplicación y expone
/// <see cref="CreateRepository"/> a los casos de uso; el método de fábrica
/// <see cref="FactoryMethod"/> es implementado por el creador concreto de
/// Infraestructura, que decide qué adaptador de persistencia devolver.
/// </summary>
/// <typeparam name="TContrato">Contrato de repositorio solicitado.</typeparam>
public abstract class RepositoryCreatorBase<TContrato> : IRepositoryCreator<TContrato>
{
    /// <summary>Operación pública que los casos de uso invocan.</summary>
    public TContrato CreateRepository() => FactoryMethod();

    /// <summary>Método de fábrica: lo sobrescribe el creador concreto.</summary>
    protected abstract TContrato FactoryMethod();
}
