using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>
/// Puerto de persistencia de marcas. El producto genérico del Factory Method
/// es <see cref="IGestionarRepository{T}"/>, especializado para
/// <see cref="Marca"/> con las búsquedas propias del catálogo.
/// </summary>
public interface IRepositoryMarca : IGestionarRepository<Marca>
{
    Task<IReadOnlyList<Marca>> ListarMarcasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<Marca?> ObtenerMarcaPorIdAsync(
        short idMarca,
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<bool> ExisteMarcaActivaAsync(
        short idMarca,
        CancellationToken ct = default);

    Task<bool> ExisteNombreMarcaAsync(
        string nombre,
        short? excluirIdMarca = null,
        CancellationToken ct = default);

    Task<bool> TieneProductosActivosMarcaAsync(
        short idMarca,
        CancellationToken ct = default);

    Task<short> InsertarMarcaAsync(
        Marca marca,
        CancellationToken ct = default);

    Task<bool> ActualizarMarcaAsync(
        Marca marca,
        CancellationToken ct = default);

    Task<bool> DarDeBajaMarcaAsync(
        short idMarca,
        int? idEmpleadoResponsable,
        CancellationToken ct = default);

    Task<bool> ReactivarMarcaAsync(
        short idMarca,
        int? idEmpleadoResponsable,
        CancellationToken ct = default);
}
