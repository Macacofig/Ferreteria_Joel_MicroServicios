using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Aplicacion.Modelos;
using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

public class ListarProveedoresCasoDeUso
{
    private readonly IRepositorioProveedores _repositorio;

    public ListarProveedoresCasoDeUso(RepositoryCreatorBase<IRepositorioProveedores> creadorRepositorio)
    {
        _repositorio = creadorRepositorio.CreateRepository();
    }

    public async Task<Result<IReadOnlyList<ProveedorDto>>> EjecutarAsync(string? orden, CancellationToken ct = default)
    {
        var proveedores = await _repositorio.ListarAsync(ct);
        bool descendente = string.Equals(orden, "desc", StringComparison.OrdinalIgnoreCase);

        var ordenados = descendente
            ? proveedores.OrderByDescending(p => p.RazonSocial, StringComparer.CurrentCultureIgnoreCase).ToList()
            : proveedores.OrderBy(p => p.RazonSocial, StringComparer.CurrentCultureIgnoreCase).ToList();

        IReadOnlyList<ProveedorDto> datos =
            ordenados.Select(MapeadorProveedor.ADto).ToList();
        string mensaje = datos.Count == 0
            ? "No se encontraron proveedores registrados."
            : $"Se encontraron {datos.Count} proveedor(es).";

        return Result.OkConMensaje(datos, mensaje);
    }
}
