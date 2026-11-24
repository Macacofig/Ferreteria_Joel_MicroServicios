using MicroServicioClientes.Dominio.Entidades;

namespace MicroServicioClientes.Dominio.Puertos;

/// <summary>
/// Puerto de salida: persistencia de contactos. El dominio no conoce PostgreSQL.
/// El producto del Factory Method es <see cref="IGestionarRepository{T}"/>,
/// especializado aquí para <see cref="ClienteContacto"/>.
/// </summary>
public interface IClienteContactoRepositorio : IGestionarRepository<ClienteContacto>
{
    Task<IReadOnlyList<ClienteContacto>> ListarPorClienteAsync(
        int idCliente, bool soloActivos, CancellationToken cancelacion);

    Task<ClienteContacto?> ObtenerPorIdAsync(int idContacto, CancellationToken cancelacion);

    Task<int> ContarActivosAsync(int idCliente, CancellationToken cancelacion);

    /// <summary>Inserta y devuelve el identificador generado.</summary>
    Task<int> InsertarAsync(ClienteContacto contacto, string usuario, CancellationToken cancelacion);

    Task<bool> ActualizarAsync(ClienteContacto contacto, string usuario, CancellationToken cancelacion);

    /// <summary>Baja lógica. Devuelve falso si no existía o ya estaba inactivo.</summary>
    Task<bool> DarDeBajaAsync(int idContacto, string usuario, CancellationToken cancelacion);

    /// <summary>Reactiva un contacto dado de baja. Devuelve falso si no existía o ya estaba activo.</summary>
    Task<bool> ReactivarAsync(int idContacto, string usuario, CancellationToken cancelacion);
}
