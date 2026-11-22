using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>Puerto de persistencia del catálogo de productos.</summary>
public interface IRepositoryProducto
{
    Task<IReadOnlyList<Producto>> ListarAsync(
        string? ordenarPor,
        string? direccion,
        bool incluirInactivos,
        CancellationToken ct = default);

    Task<Producto?> ObtenerPorIdAsync(int idProducto, CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(
        string codigo,
        int? excluirIdProducto = null,
        CancellationToken ct = default);

    /// <summary>
    /// Devuelve el siguiente código <c>PROD-XXX</c> disponible. Considera
    /// productos activos e inactivos para no reutilizar códigos dados de baja.
    /// </summary>
    Task<string> ObtenerSiguienteCodigoAsync(CancellationToken ct = default);

    Task<int> InsertarAsync(Producto producto, CancellationToken ct = default);

    Task<bool> ActualizarAsync(Producto producto, CancellationToken ct = default);

    Task<bool> DarDeBajaAsync(
        int idProducto,
        int? usuarioBaja,
        CancellationToken ct = default);
}
