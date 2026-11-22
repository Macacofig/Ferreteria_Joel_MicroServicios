using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>Puerto de consulta de catálogos auxiliares (categorías y marcas).</summary>
public interface IRepositoryCatalogoAuxiliar
{
    Task<IReadOnlyList<Categoria>> ListarCategoriasActivasAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Marca>> ListarMarcasActivasAsync(CancellationToken ct = default);

    Task<bool> ExisteCategoriaActivaAsync(short idCategoria, CancellationToken ct = default);

    Task<bool> ExisteMarcaActivaAsync(short idMarca, CancellationToken ct = default);
}
