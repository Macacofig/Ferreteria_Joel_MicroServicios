using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MicroServicioProductos.Infraestructura.Repositorios;

namespace MicroServicioProductos.Infraestructura.Factories;

/// <summary>
/// Creador concreto del repositorio de histórico (tabla historico_producto).
/// </summary>
public sealed class HistoricoProductoRepositoryCreator
    : RepositoryCreator<IRepositoryHistoricoProducto>
{
    private readonly IMySqlUnitOfWork _unidad;

    public HistoricoProductoRepositoryCreator(IMySqlUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public override IRepositoryHistoricoProducto CreateRepository() =>
        new MySqlHistoricoProductoRepository(_unidad);
}
