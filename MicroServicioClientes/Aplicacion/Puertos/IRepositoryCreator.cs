namespace MicroServicioClientes.Aplicacion.Puertos;

/// <summary>
/// Factory Method: los servicios de Aplicación piden este creador y nunca
/// conocen la implementación concreta del repositorio (PostgreSQL, etc.).
/// El contrato vive en Aplicación para que el núcleo no dependa de
/// Infraestructura (regla de dependencias de la arquitectura hexagonal).
/// </summary>
public interface IRepositoryCreator<TContrato>
{
    TContrato CreateRepository();
}
