namespace MicroServicioProveedores.Dominio.Puertos;

/// <summary>
/// Creador abstracto del patrón Factory Method. Vive en Dominio y expone
/// <see cref="CreateRepository"/> a los casos de uso; el método de fábrica
/// <see cref="FactoryMethod"/> es implementado por el creador concreto de
/// Infraestructura, que decide qué adaptador de persistencia devolver.
/// <para>
/// El Producto del patrón es <see cref="IRepository{TEntidad}"/>: el puerto
/// <see cref="IRepositorioProveedores"/> lo especializa mediante
/// <see cref="IGestionarRepository{TEntidad}"/> y añade las búsquedas propias.
/// </para>
/// </summary>
/// <typeparam name="TRepositorio">Contrato de repositorio solicitado.</typeparam>
public abstract class RepositoryCreatorBase<TRepositorio> : IRepositoryCreator<TRepositorio>
{
    /// <summary>Operación pública que los casos de uso invocan.</summary>
    public TRepositorio CreateRepository() => FactoryMethod();

    /// <summary>Método de fábrica: lo sobrescribe el creador concreto.</summary>
    protected abstract TRepositorio FactoryMethod();
}
