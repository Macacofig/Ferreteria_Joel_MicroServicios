using MicroServicioProveedores.Dominio.Puertos;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

/// <summary>
/// Caso de uso de baja lógica de proveedores. El documento se conserva en
/// MongoDB y solo cambia su estado junto con los datos de auditoría.
/// </summary>
public class EliminarProveedorCasoDeUso
{
    private readonly IRepositorioProveedores _repositorio;

    public EliminarProveedorCasoDeUso(RepositoryCreatorBase<IRepositorioProveedores> creadorRepositorio)
    {
        _repositorio = creadorRepositorio.CreateRepository();
    }

    public async Task<Result> DarDeBajaAsync(int id, string usuario, CancellationToken ct = default)
    {
        var proveedor = await _repositorio.ObtenerPorIdAsync(id, ct);
        if (proveedor is null)
        {
            return Result.Fallo(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
        }

        if (!proveedor.Estado)
        {
            return Result.Fallo(ErrorResultado.Conflicto("El proveedor ya está dado de baja."));
        }

        bool dadoDeBaja = await _repositorio.DarDeBajaAsync(id, usuario, ct);
        return dadoDeBaja
            ? Result.OkConMensaje("Proveedor dado de baja correctamente.")
            : Result.Fallo(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
    }

    public async Task<Result> ReactivarAsync(int id, string usuario, CancellationToken ct = default)
    {
        var proveedor = await _repositorio.ObtenerPorIdAsync(id, ct);
        if (proveedor is null)
        {
            return Result.Fallo(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
        }

        if (proveedor.Estado)
        {
            return Result.Fallo(ErrorResultado.Conflicto("El proveedor ya está activo."));
        }

        bool reactivado = await _repositorio.ReactivarAsync(id, usuario, ct);
        return reactivado
            ? Result.OkConMensaje("Proveedor reactivado correctamente.")
            : Result.Fallo(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
    }
}
