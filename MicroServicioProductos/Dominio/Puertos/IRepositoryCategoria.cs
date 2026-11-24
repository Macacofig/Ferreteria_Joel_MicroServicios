using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>
/// Puerto de persistencia de categorías. El producto genérico del Factory
/// Method es <see cref="IGestionarRepository{T}"/>, especializado para
/// <see cref="Categoria"/> con las búsquedas propias del catálogo.
/// </summary>
public interface IRepositoryCategoria : IGestionarRepository<Categoria>
{
    Task<IReadOnlyList<Categoria>> ListarCategoriasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<Categoria?> ObtenerCategoriaPorIdAsync(
        short idCategoria,
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<bool> ExisteCategoriaActivaAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<bool> ExisteCodigoCategoriaAsync(
        string codigo,
        short? excluirIdCategoria = null,
        CancellationToken ct = default);

    /// <summary>Devuelve el siguiente código <c>CAT-XXX</c> disponible.</summary>
    Task<string> ObtenerSiguienteCodigoCategoriaAsync(
        CancellationToken ct = default);

    Task<bool> ExisteNombreCategoriaAsync(
        string nombre,
        short? excluirIdCategoria = null,
        CancellationToken ct = default);

    Task<bool> TieneProductosActivosCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<short> InsertarCategoriaAsync(
        Categoria categoria,
        CancellationToken ct = default);

    Task<bool> ActualizarCategoriaAsync(
        Categoria categoria,
        CancellationToken ct = default);

    Task<bool> DarDeBajaCategoriaAsync(
        short idCategoria,
        int? idEmpleadoResponsable,
        CancellationToken ct = default);

    Task<bool> ReactivarCategoriaAsync(
        short idCategoria,
        int? idEmpleadoResponsable,
        CancellationToken ct = default);
}
