using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Aplicacion.Modelos;
using MicroServicioProveedores.Dominio.Entidades;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

public class CrearProveedorCasoDeUso
{
    private readonly IRepositorioProveedores _repositorio;

    public CrearProveedorCasoDeUso(IRepositorioProveedores repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Result<ProveedorDto>> EjecutarAsync(CrearProveedorSolicitud solicitud, CancellationToken ct = default)
    {
        string nitNormalizado = Proveedor.NormalizarNit(solicitud.Nit);
        if (!string.IsNullOrWhiteSpace(nitNormalizado))
        {
            var existenteNit = await _repositorio.ObtenerPorNitNormalizadoAsync(nitNormalizado, ct);
            if (existenteNit is not null)
            {
                return Result.Fallo<ProveedorDto>(ErrorResultado.Conflicto(
                    "Ya existe un proveedor registrado con el mismo NIT.",
                    new Dictionary<string, string> { ["Nit"] = "El NIT ingresado ya está registrado para otro proveedor." }));
            }
        }

        // Se genera el identificador entero de forma atómica en persistencia para evitar colisiones bajo concurrencia.
        int nuevoId = await _repositorio.GenerarNuevoIdAsync(ct);
        var resultadoCreacion = Proveedor.CrearNuevo(nuevoId, solicitud.RazonSocial, solicitud.Nit, solicitud.Contacto, solicitud.Telefono, solicitud.NombreResponsable);
        if (resultadoCreacion.EsFallo)
        {
            return Result.Fallo<ProveedorDto>(resultadoCreacion.Error!);
        }

        try
        {
            await _repositorio.CrearAsync(resultadoCreacion.Valor!, ct);
        }
        catch (ConflictoNegocioExcepcion ex)
        {
            var errores = string.IsNullOrWhiteSpace(ex.Campo)
                ? null
                : new Dictionary<string, string> { [ex.Campo] = ex.Message };
            return Result.Fallo<ProveedorDto>(ErrorResultado.Conflicto(ex.Message, errores));
        }

        return Result.Ok(MapeadorProveedor.ADto(resultadoCreacion.Valor!));
    }
}
