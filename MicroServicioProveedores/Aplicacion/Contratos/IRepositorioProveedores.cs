using MicroServicioProveedores.Dominio.Entidades;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.Contratos;

public interface IRepositorioProveedores
{
    Task<IReadOnlyList<Proveedor>> ListarAsync(CancellationToken ct = default);
    Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Proveedor?> ObtenerPorNitNormalizadoAsync(string nitNormalizado, CancellationToken ct = default);
    Task<int> GenerarNuevoIdAsync(CancellationToken ct = default);

    /// <summary>
    /// Devuelve <see cref="Result"/> en vez de lanzar para conflictos de
    /// negocio (por ejemplo, NIT duplicado).
    /// </summary>
    Task<Result> CrearAsync(Proveedor proveedor, CancellationToken ct = default);

    /// <summary>Devuelve <see cref="Result"/> en vez de lanzar por duplicados.</summary>
    Task<Result> ActualizarAsync(Proveedor proveedor, CancellationToken ct = default);

    Task<bool> DarDeBajaAsync(int id, string usuario, CancellationToken ct = default);
    Task<bool> ReactivarAsync(int id, string usuario, CancellationToken ct = default);
}
