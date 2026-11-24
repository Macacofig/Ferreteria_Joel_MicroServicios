using MicroServicioProveedores.Aplicacion.Modelos;
using MicroServicioProveedores.Dominio.Puertos;
using MicroServicioProveedores.Dominio.Entidades;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

public class ActualizarProveedorCasoDeUso
{
    private readonly IRepositorioProveedores _repositorio;

    public ActualizarProveedorCasoDeUso(RepositoryCreatorBase<IRepositorioProveedores> creadorRepositorio)
    {
        _repositorio = creadorRepositorio.CreateRepository();
    }

    public async Task<Result<ProveedorDto>> EjecutarAsync(int id, ActualizarProveedorSolicitud solicitud, string usuario, CancellationToken ct = default)
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

        var resultadoActualizacion = proveedor.Actualizar(
            solicitud.RazonSocial,
            solicitud.Nit,
            solicitud.Contacto,
            solicitud.Telefono,
            solicitud.NombreResponsable,
            usuario);
        if (resultadoActualizacion.EsFallo)
        {
            return Result.Fallo<ProveedorDto>(resultadoActualizacion.Error!);
        }

        Result actualizado = await _repositorio.ActualizarAsync(proveedor, ct);
        if (actualizado.EsFallo)
        {
            return Result.Fallo<ProveedorDto>(actualizado.Error!);
        }

        return Result.OkConMensaje(
            MapeadorProveedor.ADto(proveedor),
            "Proveedor actualizado correctamente.");
    }
}
