using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;

namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>Casos de uso exclusivos de la tabla categoría.</summary>
public interface IServicioCategorias
{
    Task<Result<IReadOnlyList<Categoria>>> ListarAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<Result<string>> ObtenerSiguienteCodigoAsync(
        CancellationToken ct = default);

    Task<Result<Categoria>> ObtenerPorIdAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<Result<short>> CrearAsync(
        CrearCategoriaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> ActualizarAsync(
        short idCategoria,
        ActualizarCategoriaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> DarDeBajaAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<Result> ReactivarAsync(
        short idCategoria,
        CancellationToken ct = default);
}
