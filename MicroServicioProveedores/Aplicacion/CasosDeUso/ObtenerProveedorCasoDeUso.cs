using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Aplicacion.Modelos;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

public class ObtenerProveedorCasoDeUso
{
    private readonly IRepositorioProveedores _repositorio;

    public ObtenerProveedorCasoDeUso(IRepositoryCreator<IRepositorioProveedores> creadorRepositorio)
    {
        _repositorio = creadorRepositorio.CreateRepository();
    }

    public async Task<Result<ProveedorDto>> EjecutarAsync(int id, CancellationToken ct = default)
    {
        var proveedor = await _repositorio.ObtenerPorIdAsync(id, ct);
        if (proveedor is null)
        {
            return Result.Fallo<ProveedorDto>(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
        }

        return Result.Ok(MapeadorProveedor.ADto(proveedor));
    }
}
