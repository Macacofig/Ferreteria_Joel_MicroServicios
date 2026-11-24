using MicroServicioProductos.Dominio.Puertos;

namespace MicroServicioProductos.Infraestructura.Factories;

/// <summary>
/// Creador abstracto del patrón Factory Method. Cada creador concreto decide
/// qué adaptador de persistencia devolver, de modo que los casos de uso
/// dependan solo del contrato de dominio y no de las clases concretas de MySQL.
/// <para>
/// El Producto del patrón es <see cref="IRepository{TEntidad}"/>: los puertos
/// <see cref="IRepositoryProducto"/> e <see cref="IRepositoryHistoricoProducto"/>
/// lo especializan (el primero vía <see cref="IGestionarRepository{TEntidad}"/>)
/// y añaden las consultas propias del catálogo.
/// </para>
/// </summary>
/// <typeparam name="TRepositorio">Contrato de repositorio solicitado.</typeparam>
public abstract class RepositoryCreator<TRepositorio>
{
    /// <summary>Método de fábrica implementado por los creadores concretos.</summary>
    public abstract TRepositorio CreateRepository();
}
