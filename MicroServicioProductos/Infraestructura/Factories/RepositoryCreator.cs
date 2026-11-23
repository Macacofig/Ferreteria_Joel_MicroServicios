using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MicroServicioProductos.Infraestructura.Repositorios;

namespace MicroServicioProductos.Infraestructura.Factories;

/// <summary>
/// Factory Method de repositorios: la aplicación conoce solo el contrato del
/// dominio y el creador concreto decide qué adaptador MySQL instantiar.
/// </summary>
public abstract class RepositoryCreator<TRepository>
{
    public abstract TRepository CreateRepository();
}

public sealed class ProductoRepositoryCreator : RepositoryCreator<IRepositoryProducto>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductoRepositoryCreator(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override IRepositoryProducto CreateRepository() =>
        new MySqlProductoRepository(_connectionFactory);
}

public sealed class CatalogoAuxiliarRepositoryCreator
    : RepositoryCreator<IRepositoryCatalogoAuxiliar>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CatalogoAuxiliarRepositoryCreator(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override IRepositoryCatalogoAuxiliar CreateRepository() =>
        new MySqlCatalogoAuxiliarRepository(_connectionFactory);
}

public sealed class HistoricoProductoRepositoryCreator
    : RepositoryCreator<IRepositoryHistoricoProducto>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HistoricoProductoRepositoryCreator(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override IRepositoryHistoricoProducto CreateRepository() =>
        new MySqlHistoricoProductoRepository(_connectionFactory);
}
