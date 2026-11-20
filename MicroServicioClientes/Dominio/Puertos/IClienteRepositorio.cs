using MicroServicioClientes.Dominio.Entidades;

namespace MicroServicioClientes.Dominio.Puertos;

/// <summary>Puerto de salida: persistencia de clientes. El dominio no conoce PostgreSQL.</summary>
public interface IClienteRepositorio
{
    Task<IReadOnlyList<Cliente>> ListarAsync(FiltroClientes filtro, CancellationToken cancelacion);

    Task<Cliente?> ObtenerPorIdAsync(int idCliente, CancellationToken cancelacion);

    /// <summary>Indica si el documento ya existe, ignorando opcionalmente un cliente (edición).</summary>
    Task<bool> ExisteDocumentoAsync(string documento, int? excluirIdCliente, CancellationToken cancelacion);

    /// <summary>Inserta y devuelve el identificador generado.</summary>
    Task<int> InsertarAsync(Cliente cliente, string usuario, CancellationToken cancelacion);

    Task<bool> ActualizarAsync(Cliente cliente, string usuario, CancellationToken cancelacion);

    /// <summary>Baja lógica. Devuelve falso si no existía o ya estaba inactivo.</summary>
    Task<bool> DarDeBajaAsync(int idCliente, string usuario, CancellationToken cancelacion);
}