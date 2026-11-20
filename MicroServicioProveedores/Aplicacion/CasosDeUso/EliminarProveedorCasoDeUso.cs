using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

public class EliminarProveedorCasoDeUso
{
    private readonly IRepositorioProveedores _repositorio;

    public EliminarProveedorCasoDeUso(IRepositorioProveedores repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Result> EjecutarAsync(int id, CancellationToken ct = default)
    {
        bool eliminado = await _repositorio.EliminarAsync(id, ct);
        if (!eliminado)
        {
            return Result.Fallo(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
        }

        return Result.Ok();
    }
}
