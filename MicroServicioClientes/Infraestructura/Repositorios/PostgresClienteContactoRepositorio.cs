using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Dominio.Puertos;
using MicroServicioClientes.Infraestructura.Conexion;
using Npgsql;

namespace MicroServicioClientes.Infraestructura.Repositorios;

/// <summary>
/// Adaptador de salida (patrón Adapter + Repository): traduce el puerto
/// <see cref="IClienteContactoRepositorio"/> a consultas ADO.NET sobre PostgreSQL.
/// </summary>
public sealed class PostgresClienteContactoRepositorio : IClienteContactoRepositorio
{
    private const string Columnas = @"
        id_contacto, id_cliente, nombre, cargo, telefono, correo, activo,
        fecha_creacion, usuario_creacion, fecha_modificacion, usuario_modificacion,
        fecha_baja, usuario_baja";

    /// <summary>Usuario usado por las operaciones del contrato genérico.</summary>
    private const string UsuarioGenerico = "sistema";

    private readonly IFabricaConexion _fabrica;

    public PostgresClienteContactoRepositorio(IFabricaConexion fabrica)
    {
        _fabrica = fabrica;
    }

    public async Task<IReadOnlyList<ClienteContacto>> ListarPorClienteAsync(
        int idCliente, bool soloActivos, CancellationToken cancelacion)
    {
        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        string where = soloActivos
            ? "WHERE id_cliente = @id AND activo = TRUE"
            : "WHERE id_cliente = @id";

        await using var comando = new NpgsqlCommand(
            $"SELECT {Columnas} FROM cliente_contacto {where} ORDER BY nombre ASC, id_contacto ASC",
            conexion);
        comando.Parameters.AddWithValue("id", idCliente);

        var contactos = new List<ClienteContacto>();
        await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(cancelacion);
        while (await lector.ReadAsync(cancelacion))
        {
            contactos.Add(Leer(lector));
        }

        return contactos;
    }

    public async Task<ClienteContacto?> ObtenerPorIdAsync(
        int idContacto, CancellationToken cancelacion)
    {
        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(
            $"SELECT {Columnas} FROM cliente_contacto WHERE id_contacto = @id", conexion);
        comando.Parameters.AddWithValue("id", idContacto);

        await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(cancelacion);
        return await lector.ReadAsync(cancelacion) ? Leer(lector) : null;
    }

    public async Task<int> ContarActivosAsync(int idCliente, CancellationToken cancelacion)
    {
        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(
            "SELECT COUNT(*) FROM cliente_contacto WHERE id_cliente = @id AND activo = TRUE",
            conexion);
        comando.Parameters.AddWithValue("id", idCliente);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancelacion));
    }

    public async Task<int> InsertarAsync(
        ClienteContacto contacto, string usuario, CancellationToken cancelacion)
    {
        const string sql = @"
            INSERT INTO cliente_contacto
                (id_cliente, nombre, cargo, telefono, correo, activo,
                 fecha_creacion, usuario_creacion)
            VALUES
                (@id_cliente, @nombre, @cargo, @telefono, @correo, TRUE,
                 NOW(), @usuario)
            RETURNING id_contacto";

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        AgregarDatos(comando, contacto);
        comando.Parameters.AddWithValue("usuario", usuario);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancelacion));
    }

    public async Task<bool> ActualizarAsync(
        ClienteContacto contacto, string usuario, CancellationToken cancelacion)
    {
        const string sql = @"
            UPDATE cliente_contacto
               SET nombre = @nombre,
                   cargo = @cargo,
                   telefono = @telefono,
                   correo = @correo,
                   fecha_modificacion = NOW(),
                   usuario_modificacion = @usuario
             WHERE id_contacto = @id AND activo = TRUE";

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        AgregarDatos(comando, contacto);
        comando.Parameters.AddWithValue("usuario", usuario);
        comando.Parameters.AddWithValue("id", contacto.IdContacto);

        return await comando.ExecuteNonQueryAsync(cancelacion) > 0;
    }

    public Task<bool> DarDeBajaAsync(
        int idContacto, string usuario, CancellationToken cancelacion) =>
        CambiarEstadoAsync(idContacto, usuario, nuevoEstado: false, cancelacion);

    public Task<bool> ReactivarAsync(
        int idContacto, string usuario, CancellationToken cancelacion) =>
        CambiarEstadoAsync(idContacto, usuario, nuevoEstado: true, cancelacion);

    // ================== Producto genérico del Factory Method ==================

    /// <summary>
    /// Adaptador de <see cref="IGestionarRepository{T}"/>: el listado
    /// específico exige un cliente, aquí se listan todos los contactos.
    /// </summary>
    public Task<List<ClienteContacto>> ObtenerTodosAsync() =>
        ListarTodosAsync(soloActivos: false, CancellationToken.None);

    public Task<List<ClienteContacto>> MostrarActivosAsync() =>
        ListarTodosAsync(soloActivos: true, CancellationToken.None);

    public Task<int> InsertarAsync(ClienteContacto entidad) =>
        InsertarAsync(entidad, UsuarioGenerico, CancellationToken.None);

    public Task ActualizarAsync(ClienteContacto entidad) =>
        ActualizarAsync(entidad, UsuarioGenerico, CancellationToken.None);

    /// <summary>Invierte el estado actual; el id de empleado se usa como auditoría.</summary>
    public async Task CambiarEstadoAsync(int id, int idEmpleadoResponsable)
    {
        ClienteContacto? contacto = await ObtenerPorIdAsync(id, CancellationToken.None);
        if (contacto is null)
        {
            return;
        }

        string usuario = idEmpleadoResponsable.ToString();

        if (contacto.Activo)
        {
            await DarDeBajaAsync(id, usuario, CancellationToken.None);
        }
        else
        {
            await ReactivarAsync(id, usuario, CancellationToken.None);
        }
    }

    private async Task<List<ClienteContacto>> ListarTodosAsync(
        bool soloActivos, CancellationToken cancelacion)
    {
        string where = soloActivos ? "WHERE activo = TRUE" : string.Empty;

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(
            $"SELECT {Columnas} FROM cliente_contacto {where} " +
            "ORDER BY nombre ASC, id_contacto ASC",
            conexion);

        var contactos = new List<ClienteContacto>();
        await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(cancelacion);
        while (await lector.ReadAsync(cancelacion))
        {
            contactos.Add(Leer(lector));
        }

        return contactos;
    }

    private async Task<bool> CambiarEstadoAsync(
        int idContacto,
        string usuario,
        bool nuevoEstado,
        CancellationToken cancelacion)
    {
        string sql = nuevoEstado
            ? """
              UPDATE cliente_contacto
                 SET activo = TRUE,
                     fecha_baja = NULL,
                     usuario_baja = NULL,
                     fecha_modificacion = NOW(),
                     usuario_modificacion = @usuario
               WHERE id_contacto = @id AND activo = FALSE
              """
            : """
              UPDATE cliente_contacto
                 SET activo = FALSE,
                     fecha_baja = NOW(),
                     usuario_baja = @usuario,
                     fecha_modificacion = NOW(),
                     usuario_modificacion = @usuario
               WHERE id_contacto = @id AND activo = TRUE
              """;

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("usuario", usuario);
        comando.Parameters.AddWithValue("id", idContacto);

        return await comando.ExecuteNonQueryAsync(cancelacion) > 0;
    }

    private static void AgregarDatos(NpgsqlCommand comando, ClienteContacto contacto)
    {
        comando.Parameters.AddWithValue("id_cliente", contacto.IdCliente);
        comando.Parameters.AddWithValue("nombre", contacto.Nombre);
        comando.Parameters.AddWithValue("cargo", (object?)contacto.Cargo ?? DBNull.Value);
        comando.Parameters.AddWithValue("telefono", (object?)contacto.Telefono ?? DBNull.Value);
        comando.Parameters.AddWithValue("correo", (object?)contacto.Correo ?? DBNull.Value);
    }

    private static ClienteContacto Leer(NpgsqlDataReader lector) => new()
    {
        IdContacto = lector.GetInt32(0),
        IdCliente = lector.GetInt32(1),
        Nombre = lector.GetString(2),
        Cargo = lector.IsDBNull(3) ? null : lector.GetString(3),
        Telefono = lector.IsDBNull(4) ? null : lector.GetString(4),
        Correo = lector.IsDBNull(5) ? null : lector.GetString(5),
        Activo = lector.GetBoolean(6),
        FechaCreacion = lector.GetDateTime(7),
        UsuarioCreacion = lector.GetString(8),
        FechaModificacion = lector.IsDBNull(9) ? null : lector.GetDateTime(9),
        UsuarioModificacion = lector.IsDBNull(10) ? null : lector.GetString(10),
        FechaBaja = lector.IsDBNull(11) ? null : lector.GetDateTime(11),
        UsuarioBaja = lector.IsDBNull(12) ? null : lector.GetString(12)
    };
}
