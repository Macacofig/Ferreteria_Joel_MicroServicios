using MicroServicioClientes.Dominio.Entidades;

namespace MicroServicioClientes.Dominio.Puertos;

/// <summary>Puerto de salida: persistencia de tipos de cliente.</summary>
public interface ITipoClienteRepositorio
{
    Task<IReadOnlyList<TipoCliente>> ListarAsync(bool soloActivos, CancellationToken cancelacion);

    Task<TipoCliente?> ObtenerPorIdAsync(int idTipoCliente, CancellationToken cancelacion);

    Task<bool> ExisteNombreAsync(string nombre, int? excluirIdTipoCliente, CancellationToken cancelacion);

    Task<int> InsertarAsync(TipoCliente tipo, string usuario, CancellationToken cancelacion);

    Task<bool> ActualizarAsync(TipoCliente tipo, string usuario, CancellationToken cancelacion);

    Task<bool> EliminarAsync(int idTipoCliente, CancellationToken cancelacion);

    /// <summary>Cantidad de clientes (activos o no) que usan el tipo.</summary>
    Task<int> ContarClientesAsync(int idTipoCliente, CancellationToken cancelacion);
}