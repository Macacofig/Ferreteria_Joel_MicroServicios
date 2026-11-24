using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MicroServicioProductos.Infraestructura.Repositorios;

namespace MicroServicioProductos.Infraestructura.Factories;

/// <summary>Creador concreto del repositorio de marcas (tabla marca).</summary>
public sealed class MarcaRepositoryCreator : RepositoryCreator<IRepositoryMarca>
{
    private readonly IMySqlUnitOfWork _unidad;

    public MarcaRepositoryCreator(IMySqlUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public override IRepositoryMarca CreateRepository() =>
        new MySqlMarcaRepository(_unidad);
}
