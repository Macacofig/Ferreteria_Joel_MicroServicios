using MicroServicioProveedores.Aplicacion.Modelos;
using MicroServicioProveedores.Dominio.Puertos;
using MicroServicioProveedores.Dominio.Entidades;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

public class CrearProveedorCasoDeUso
{
    private readonly IRepositorioProveedores _repositorio;

    public CrearProveedorCasoDeUso(RepositoryCreatorBase<IRepositorioProveedores> creadorRepositorio)
    {
        _repositorio = creadorRepositorio.CreateRepository();
    }

    public async Task<Result<ProveedorDto>> EjecutarAsync(CrearProveedorSolicitud solicitud, string usuario, CancellationToken ct = default)
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
        var resultadoCreacion = Proveedor.CrearNuevo(
            nuevoId,
            solicitud.RazonSocial,
            solicitud.Nit,
            solicitud.Contacto,
            solicitud.Telefono,
            solicitud.NombreResponsable,
            usuario);
        if (resultadoCreacion.EsFallo)
        {
            return Result.Fallo<ProveedorDto>(resultadoCreacion.Error!);
        }

        Result guardado = await _repositorio.CrearAsync(resultadoCreacion.Valor!, ct);
        if (guardado.EsFallo)
        {
            return Result.Fallo<ProveedorDto>(guardado.Error!);
        }

        return Result.OkConMensaje(
            MapeadorProveedor.ADto(resultadoCreacion.Valor!),
            "Proveedor registrado correctamente.");
    }
}
