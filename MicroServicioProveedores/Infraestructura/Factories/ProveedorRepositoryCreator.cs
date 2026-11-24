using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Infraestructura.Persistencia;
using MongoDB.Driver;

namespace MicroServicioProveedores.Infraestructura.Factories;

/// <summary>
/// Creador concreto del Factory Method. Hereda del creador abstracto de
/// Aplicación y es el único lugar que referencia al repositorio Mongo.
/// </summary>
public sealed class ProveedorRepositoryCreator
    : RepositoryCreatorBase<IRepositorioProveedores>
{
    private readonly IMongoDatabase _database;

    public ProveedorRepositoryCreator(IMongoDatabase database)
    {
        _database = database;
    }

    /// <summary>Implementación concreta del método de fábrica.</summary>
    protected override IRepositorioProveedores FactoryMethod()
        => new MongoProveedorRepositorio(_database);
}
