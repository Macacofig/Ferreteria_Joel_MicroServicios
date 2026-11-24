using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;

namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>
/// Casos de uso del catálogo de productos: CRUD, código autogenerado
/// <c>PROD-XXX</c> e histórico de cambios.
/// </summary>
public interface IServicioProductos
{
    Task<Result<IReadOnlyList<Producto>>> ListarAsync(
        string? ordenarPor,
        string? direccion,
        bool incluirInactivos,
        CancellationToken ct = default);

    Task<Result<Producto>> ObtenerPorIdAsync(
        int idProducto,
        CancellationToken ct = default);

    Task<Result<string>> ObtenerSiguienteCodigoAsync(
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<HistoricoProducto>>> ListarHistoricoAsync(
        int idProducto,
        CancellationToken ct = default);

    Task<Result<int>> CrearAsync(
        CrearProductoRequest solicitud,
        CancellationToken ct = default);

    Task<Result> ActualizarAsync(
        int idProducto,
        ActualizarProductoRequest solicitud,
        CancellationToken ct = default);

    Task<Result> DarDeBajaAsync(
        int idProducto,
        CancellationToken ct = default);

    Task<Result> ReactivarAsync(
        int idProducto,
        CancellationToken ct = default);
}
