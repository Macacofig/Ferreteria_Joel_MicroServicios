using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Dominio.Resultados;
using MicroServicioProductos.Infraestructura.Factories;

namespace MicroServicioProductos.Aplicacion.Servicios;

/// <summary>Consulta del historial de un producto existente.</summary>
public sealed class ServicioHistoricoProducto : IServicioHistoricoProducto
{
    private readonly IRepositoryProducto _productos;
    private readonly IRepositoryHistoricoProducto _historico;

    public ServicioHistoricoProducto(
        RepositoryCreator<IRepositoryProducto> creadorProductos,
        RepositoryCreator<IRepositoryHistoricoProducto> creadorHistorico)
    {
        _productos = creadorProductos.CreateRepository();
        _historico = creadorHistorico.CreateRepository();
    }

    public async Task<Result<IReadOnlyList<HistoricoProducto>>> ListarPorProductoAsync(
        int idProducto,
        CancellationToken ct = default)
    {
        Producto? producto = await _productos.ObtenerPorIdAsync(idProducto, ct);

        if (producto is null)
        {
            return Result.Fallo<IReadOnlyList<HistoricoProducto>>(
                PrefijosResultado.NoEncontrado + "El producto no existe.");
        }

        IReadOnlyList<HistoricoProducto> historial =
            await _historico.ListarPorProductoAsync(idProducto, ct);

        return Result.OkConMensaje(
            historial,
            $"Se encontraron {historial.Count} registro(s) de historial.");
    }
}
