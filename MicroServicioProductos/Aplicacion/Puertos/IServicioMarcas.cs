using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;

namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>Casos de uso del catálogo de marcas.</summary>
public interface IServicioMarcas
{
    Task<Result<IReadOnlyList<Marca>>> ListarMarcasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<Result<Marca>> ObtenerMarcaPorIdAsync(
        short idMarca,
        CancellationToken ct = default);

    Task<Result<short>> CrearMarcaAsync(
        CrearMarcaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> ActualizarMarcaAsync(
        short idMarca,
        ActualizarMarcaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> DarDeBajaMarcaAsync(
        short idMarca,
        CancellationToken ct = default);

    Task<Result> ReactivarMarcaAsync(
        short idMarca,
        CancellationToken ct = default);
}
