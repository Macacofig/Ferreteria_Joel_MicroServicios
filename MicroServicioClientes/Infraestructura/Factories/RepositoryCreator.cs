using MicroServicioClientes.Aplicacion.Puertos;

namespace MicroServicioClientes.Infraestructura.Factories;

/// <summary>
/// Base del patrón Factory Method para repositorios. Cada creador concreto
/// produce un repositorio sobre la conexión configurada, de modo que los
/// casos de uso dependan solo del contrato de dominio y no de las clases
/// de PostgreSQL.
/// </summary>
public abstract class RepositoryCreator<TContrato> : IRepositoryCreator<TContrato>
{
    public abstract TContrato CreateRepository();
}
