using MicroServicioClientes.Dominio.Puertos;
using MicroServicioClientes.Infraestructura.Conexion;
using MicroServicioClientes.Infraestructura.Repositorios;

namespace MicroServicioClientes.Infraestructura.Factories;

/// <summary>
/// Patrón Factory Method (Concrete Creator): fabrica el repositorio de
/// clientes sobre PostgreSQL.
/// </summary>
public sealed class ClienteRepositoryCreator : RepositoryCreator<IClienteRepositorio>
{
    private readonly IFabricaConexion _fabricaConexion;

    public ClienteRepositoryCreator(IFabricaConexion fabricaConexion)
    {
        _fabricaConexion = fabricaConexion;
    }

    public override IClienteRepositorio CreateRepository() =>
        new PostgresClienteRepositorio(_fabricaConexion);
}