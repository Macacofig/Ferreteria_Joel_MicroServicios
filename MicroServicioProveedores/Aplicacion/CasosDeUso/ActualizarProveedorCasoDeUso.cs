using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Aplicacion.Modelos;
using MicroServicioProveedores.Dominio.Entidades;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

public class ActualizarProveedorCasoDeUso
{
    private readonly IRepositorioProveedores _repositorio;

    public ActualizarProveedorCasoDeUso(IRepositorioProveedores repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Result<ProveedorDto>> EjecutarAsync(int id, ActualizarProveedorSolicitud solicitud, CancellationToken ct = default)
    {
        var proveedor = await _repositorio.ObtenerPorIdAsync(id, ct);
        if (proveedor is null)
        {
            return Result.Fallo<ProveedorDto>(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
        }

        string nitNormalizado = Proveedor.NormalizarNit(solicitud.Nit);
        if (!string.IsNullOrWhiteSpace(nitNormalizado))
        {
            var existenteNit = await _repositorio.ObtenerPorNitNormalizadoAsync(nitNormalizado, ct);
            if (existenteNit is not null && existenteNit.Id != id)
            {
                return Result.Fallo<ProveedorDto>(ErrorResultado.Conflicto(
                    "Ya existe otro proveedor registrado con el mismo NIT.",
                    new Dictionary<string, string> { ["Nit"] = "El NIT ingresado ya está registrado para otro proveedor." }));
            }
        }

        var resultadoActualizacion = proveedor.Actualizar(solicitud.RazonSocial, solicitud.Nit, solicitud.Contacto, solicitud.Telefono);
        if (resultadoActualizacion.EsFallo)
        {
            return Result.Fallo<ProveedorDto>(resultadoActualizacion.Error!);
        }

        try
        {
            bool actualizado = await _repositorio.ActualizarAsync(proveedor, ct);
            if (!actualizado)
            {
                return Result.Fallo<ProveedorDto>(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
            }
        }
        catch (ConflictoNegocioExcepcion ex)
        {
            var errores = string.IsNullOrWhiteSpace(ex.Campo)
                ? null
                : new Dictionary<string, string> { [ex.Campo] = ex.Message };
            return Result.Fallo<ProveedorDto>(ErrorResultado.Conflicto(ex.Message, errores));
        }

        return Result.Ok(MapeadorProveedor.ADto(proveedor));
    }
}
