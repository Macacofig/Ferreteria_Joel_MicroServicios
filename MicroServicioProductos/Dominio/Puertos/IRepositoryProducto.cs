using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>
/// Puerto de persistencia del catálogo de productos. El producto genérico del
/// Factory Method es <see cref="IGestionarRepository{T}"/>, especializado para
/// <see cref="Producto"/> con las consultas propias del catálogo.
/// </summary>
public interface IRepositoryProducto : IGestionarRepository<Producto>
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

    /// <summary>
    /// Reactiva un producto dado de baja, limpiando los campos de baja y
    /// registrando la auditoría de la reactivación.
    /// </summary>
    Task<bool> ReactivarAsync(
        int idProducto,
        int? usuario,
        CancellationToken ct = default);
}
