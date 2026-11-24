using MicroServicioClientes.Dominio.Puertos;

namespace MicroServicioClientes.Infraestructura.Factories;

/// <summary>
/// Creador abstracto del patrón Factory Method para repositorios. Cada
/// creador concreto decide qué adaptador de persistencia devolver, de modo
/// que los casos de uso dependan solo del contrato de dominio y no de las
/// clases concretas de PostgreSQL.
/// <para>
/// El Producto del patrón es <see cref="IRepository{TEntidad}"/>: los puertos
/// <see cref="IClienteRepositorio"/> y <see cref="IClienteContactoRepositorio"/>
/// lo especializan mediante <see cref="IGestionarRepository{TEntidad}"/> y
/// añaden las operaciones propias de cada consulta.
/// </para>
/// </summary>
/// <typeparam name="TRepositorio">Contrato de repositorio solicitado.</typeparam>
public abstract class RepositoryCreator<TRepositorio>
{
    /// <summary>Método de fábrica implementado por los creadores concretos.</summary>
    public abstract TRepositorio CreateRepository();
}
