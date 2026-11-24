namespace MicroServicioProveedores.Aplicacion.Contratos;

/// <summary>
/// Factory Method: los servicios de Aplicación piden este creador y nunca
/// conocen la implementación concreta del repositorio (Mongo, MySQL, etc.).
/// El contrato vive en Aplicación para que el núcleo no dependa de
/// Infraestructura (regla de dependencias de Clean Architecture).
/// </summary>
public interface IRepositoryCreator<TContrato>
{
    TContrato CreateRepository();
}
