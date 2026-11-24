using MicroServicioProveedores.Aplicacion.Contratos;

namespace MicroServicioProveedores.Infraestructura.Factories;

/// <summary>
/// Base del patrón Factory Method para repositorios, equivalente a la usada
/// en el microservicio de Usuarios. Cada creador concreto decide qué
/// implementación devolver para el contrato solicitado.
/// </summary>
public abstract class RepositoryCreator<TContrato> : IRepositoryCreator<TContrato>
{
    public abstract TContrato CreateRepository();
}
