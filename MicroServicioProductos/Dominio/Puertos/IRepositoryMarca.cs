using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>Puerto de persistencia del catálogo de marcas.</summary>
public interface IRepositoryMarca
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
