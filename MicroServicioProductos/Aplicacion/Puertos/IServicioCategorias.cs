using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;

namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>
/// Casos de uso del catálogo de categorías, incluido el código
/// autogenerado <c>CAT-XXX</c>.
/// </summary>
public interface IServicioCategorias
{
    Task<Result<string>> ObtenerSiguienteCodigoCategoriaAsync(
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<Categoria>>> ListarCategoriasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<Result<Categoria>> ObtenerCategoriaPorIdAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<Result<short>> CrearCategoriaAsync(
        CrearCategoriaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> ActualizarCategoriaAsync(
        short idCategoria,
        ActualizarCategoriaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> DarDeBajaCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<Result> ReactivarCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default);
}
