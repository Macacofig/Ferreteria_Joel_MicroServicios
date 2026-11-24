using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>
/// Puerto de persistencia del histórico de cambios de productos. El producto
/// genérico del Factory Method es <see cref="IRepository{T}"/>, especializado
/// para <see cref="HistoricoProducto"/>.
/// </summary>
public interface IRepositoryHistoricoProducto : IRepository<HistoricoProducto>
{
    Task RegistrarAsync(HistoricoProducto registro, CancellationToken ct = default);

    Task<IReadOnlyList<HistoricoProducto>> ListarPorProductoAsync(
        int idProducto,
        CancellationToken ct = default);
}
