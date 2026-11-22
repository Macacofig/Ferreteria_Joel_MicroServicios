using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>
/// Puerto de consulta y mantenimiento de los catálogos auxiliares
/// (categorías y marcas) sobre bdproductos.
/// </summary>
public interface IRepositoryCatalogoAuxiliar
{
    // ===================== Consultas =====================

    Task<IReadOnlyList<Categoria>> ListarCategoriasActivasAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Marca>> ListarMarcasActivasAsync(CancellationToken ct = default);

    Task<Categoria?> ObtenerCategoriaPorIdAsync(short idCategoria, CancellationToken ct = default);

    Task<Marca?> ObtenerMarcaPorIdAsync(short idMarca, CancellationToken ct = default);

    Task<bool> ExisteCategoriaActivaAsync(short idCategoria, CancellationToken ct = default);

    Task<bool> ExisteMarcaActivaAsync(short idMarca, CancellationToken ct = default);

    Task<bool> ExisteCodigoCategoriaAsync(
        string codigo,
        short? excluirIdCategoria = null,
        CancellationToken ct = default);

    Task<bool> ExisteNombreCategoriaAsync(
        string nombre,
        short? excluirIdCategoria = null,
        CancellationToken ct = default);

    Task<bool> ExisteNombreMarcaAsync(
        string nombre,
        short? excluirIdMarca = null,
        CancellationToken ct = default);

    Task<bool> TieneProductosActivosCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<bool> TieneProductosActivosMarcaAsync(
        short idMarca,
        CancellationToken ct = default);

    // ===================== Categorías =====================

    Task<short> InsertarCategoriaAsync(Categoria categoria, CancellationToken ct = default);

    Task<bool> ActualizarCategoriaAsync(Categoria categoria, CancellationToken ct = default);

    Task<bool> DarDeBajaCategoriaAsync(
        short idCategoria,
        int? idEmpleadoResponsable,
        CancellationToken ct = default);

    // ===================== Marcas =====================

    Task<short> InsertarMarcaAsync(Marca marca, CancellationToken ct = default);

    Task<bool> ActualizarMarcaAsync(Marca marca, CancellationToken ct = default);

    Task<bool> DarDeBajaMarcaAsync(
        short idMarca,
        int? idEmpleadoResponsable,
        CancellationToken ct = default);
}
