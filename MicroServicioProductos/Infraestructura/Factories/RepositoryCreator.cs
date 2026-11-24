using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MicroServicioProductos.Infraestructura.Repositorios;

namespace MicroServicioProductos.Infraestructura.Factories;

/// <summary>
/// Factory Method de repositorios: la aplicación conoce solo el contrato del
/// dominio y el creador concreto decide qué adaptador MySQL instanciar.
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

public sealed class CategoriaRepositoryCreator : RepositoryCreator<IRepositoryCategoria>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CategoriaRepositoryCreator(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override IRepositoryCategoria CreateRepository() =>
        new MySqlCategoriaRepository(_connectionFactory);
}

public sealed class MarcaRepositoryCreator : RepositoryCreator<IRepositoryMarca>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MarcaRepositoryCreator(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override IRepositoryMarca CreateRepository() =>
        new MySqlMarcaRepository(_connectionFactory);
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
