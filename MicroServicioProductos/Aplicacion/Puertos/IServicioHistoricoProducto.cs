using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;

namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>Consulta del historial de cambios de un producto.</summary>
public interface IServicioHistoricoProducto
{
    Task<Result<IReadOnlyList<HistoricoProducto>>> ListarPorProductoAsync(
        int idProducto,
        CancellationToken ct = default);
}
