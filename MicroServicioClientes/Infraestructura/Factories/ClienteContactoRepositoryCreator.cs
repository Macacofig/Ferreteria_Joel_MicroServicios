using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Dominio.Puertos;
using MicroServicioClientes.Infraestructura.Conexion;
using MicroServicioClientes.Infraestructura.Repositorios;

namespace MicroServicioClientes.Infraestructura.Factories;

/// <summary>
/// Patrón Factory Method (Concrete Creator): fabrica el repositorio de
/// contactos sobre PostgreSQL. Es el único lugar que conoce la clase concreta.
/// </summary>
public sealed class ClienteContactoRepositoryCreator : RepositoryCreator<IClienteContactoRepositorio>
{
    private readonly IFabricaConexion _fabricaConexion;

    public ClienteContactoRepositoryCreator(IFabricaConexion fabricaConexion)
    {
        _fabricaConexion = fabricaConexion;
    }

    public override IClienteContactoRepositorio CreateRepository()
        => new PostgresClienteContactoRepositorio(_fabricaConexion);
}
