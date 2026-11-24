using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MicroServicioProductos.Infraestructura.Repositorios;

namespace MicroServicioProductos.Infraestructura.Factories;

/// <summary>Creador concreto del repositorio de categorías (tabla categoria).</summary>
public sealed class CategoriaRepositoryCreator
    : RepositoryCreator<IRepositoryCategoria>
{
    private readonly IMySqlUnitOfWork _unidad;

    public CategoriaRepositoryCreator(IMySqlUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public override IRepositoryCategoria CreateRepository() =>
        new MySqlCategoriaRepository(_unidad);
}
