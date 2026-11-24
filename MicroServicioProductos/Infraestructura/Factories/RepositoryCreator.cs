using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MicroServicioProductos.Infraestructura.Repositorios;

namespace MicroServicioProductos.Infraestructura.Factories;

/// <summary>
/// Creadores concretos del Factory Method. Heredan del creador abstracto de
/// Aplicación y son los únicos que conocen los adaptadores MySQL.
/// </summary>
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
