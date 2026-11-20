using MicroServicioProveedores.Dominio.Entidades;

namespace MicroServicioProveedores.Aplicacion.Contratos;

public interface IRepositorioProveedores
{
    Task<IReadOnlyList<Proveedor>> ListarAsync(CancellationToken ct = default);
    Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Proveedor?> ObtenerPorNitNormalizadoAsync(string nitNormalizado, CancellationToken ct = default);
    Task<int> GenerarNuevoIdAsync(CancellationToken ct = default);
    Task CrearAsync(Proveedor proveedor, CancellationToken ct = default);
    Task<bool> ActualizarAsync(Proveedor proveedor, CancellationToken ct = default);
    Task<bool> EliminarAsync(int id, CancellationToken ct = default);
}
