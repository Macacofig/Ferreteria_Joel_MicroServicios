using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Infraestructura.Persistencia;
using MongoDB.Driver;

namespace MicroServicioProveedores.Infraestructura.Factories;

/// <summary>
/// Creador concreto de proveedores. Es el único lugar que referencia al
/// repositorio Mongo; Aplicación solo conoce <see cref="IRepositoryCreator{TContrato}"/>.
/// </summary>
public sealed class ProveedorRepositoryCreator : RepositoryCreator<IRepositorioProveedores>
{
    private readonly IMongoDatabase _database;

    public ProveedorRepositoryCreator(IMongoDatabase database)
    {
        _database = database;
    }

    public override IRepositorioProveedores CreateRepository()
        => new MongoProveedorRepositorio(_database);
}
