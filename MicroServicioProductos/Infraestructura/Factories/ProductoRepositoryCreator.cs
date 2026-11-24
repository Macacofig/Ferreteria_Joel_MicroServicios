using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MicroServicioProductos.Infraestructura.Repositorios;

namespace MicroServicioProductos.Infraestructura.Factories;

/// <summary>Creador concreto del repositorio de productos (tabla producto).</summary>
public sealed class ProductoRepositoryCreator
    : RepositoryCreator<IRepositoryProducto>
{
    private readonly IMySqlUnitOfWork _unidad;

    public ProductoRepositoryCreator(IMySqlUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public override IRepositoryProducto CreateRepository() =>
        new MySqlProductoRepository(_unidad);
}
