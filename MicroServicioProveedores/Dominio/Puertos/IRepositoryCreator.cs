namespace MicroServicioProveedores.Dominio.Puertos;

/// <summary>
/// Contrato del creador del Factory Method. Vive en Dominio para que
/// Aplicación y Dominio puedan pedir repositorios sin depender de
/// Infraestructura (regla de dependencias de Clean Architecture).
/// </summary>
/// <typeparam name="TRepositorio">Contrato de repositorio solicitado.</typeparam>
public interface IRepositoryCreator<TRepositorio>
{
    TRepositorio CreateRepository();
}
