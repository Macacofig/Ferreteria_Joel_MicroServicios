using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;

namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>Casos de uso exclusivos de la tabla marca.</summary>
public interface IServicioMarcas
{
    Task<Result<IReadOnlyList<Marca>>> ListarAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<Result<Marca>> ObtenerPorIdAsync(
        short idMarca,
        CancellationToken ct = default);

    Task<Result<short>> CrearAsync(
        CrearMarcaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> ActualizarAsync(
        short idMarca,
        ActualizarMarcaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> DarDeBajaAsync(
        short idMarca,
        CancellationToken ct = default);

    Task<Result> ReactivarAsync(
        short idMarca,
        CancellationToken ct = default);
}
