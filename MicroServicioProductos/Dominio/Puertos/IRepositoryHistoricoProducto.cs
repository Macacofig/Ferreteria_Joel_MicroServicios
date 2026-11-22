using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>Puerto de persistencia del histórico de cambios de productos.</summary>
public interface IRepositoryHistoricoProducto
{
    Task RegistrarAsync(HistoricoProducto registro, CancellationToken ct = default);

    Task<IReadOnlyList<HistoricoProducto>> ListarPorProductoAsync(
        int idProducto,
        CancellationToken ct = default);
}
