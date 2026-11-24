using System.Text;
using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Dominio.Puertos;
using MicroServicioClientes.Infraestructura.Conexion;
using Npgsql;

namespace MicroServicioClientes.Infraestructura.Repositorios;

/// <summary>
/// Adaptador de salida (patrón Adapter + Repository): traduce el puerto
/// <see cref="IClienteRepositorio"/> a consultas ADO.NET sobre PostgreSQL.
/// </summary>
public sealed class PostgresClienteRepositorio : IClienteRepositorio
{
    private const string Columnas = @"
        id_cliente, ci, nombre, apellido, correo, telefono, direccion, activo,
        fecha_creacion, usuario_creacion, fecha_modificacion, usuario_modificacion,
        fecha_baja, usuario_baja";

    /// <summary>
    /// Usuario usado por las operaciones del contrato genérico, que no recibe
    /// el autor de la acción. Las operaciones específicas sí lo reciben.
    /// </summary>
    private const string UsuarioGenerico = "sistema";

    private readonly IFabricaConexion _fabrica;

    public PostgresClienteRepositorio(IFabricaConexion fabrica)
    {
        _fabrica = fabrica;
    }

    public async Task<IReadOnlyList<Cliente>> ListarAsync(FiltroClientes filtro, CancellationToken cancelacion)
    {
        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using NpgsqlCommand comando = conexion.CreateCommand();

        var condiciones = new List<string>();

        if (filtro.SoloActivos)
        {
            condiciones.Add("activo = TRUE");
        }

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            condiciones.Add(
                "(COALESCE(ci, '') ILIKE @patron OR nombre ILIKE @patron OR apellido ILIKE @patron " +
                "OR COALESCE(correo, '') ILIKE @patron OR (nombre || ' ' || apellido) ILIKE @patron)");
            comando.Parameters.AddWithValue("patron", $"%{EscaparComodines(filtro.Buscar.Trim())}%");
        }

        string where = condiciones.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", condiciones);

        // Solo se interpolan constantes de una lista blanca: nunca texto del usuario.
        string columnaOrden = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "apellido" => "apellido",
            "ci" => "ci",
            "fecha" => "fecha_creacion",
            _ => "nombre"
        };

        string sentido = filtro.Descendente ? "DESC" : "ASC";

        comando.CommandText =
            $"SELECT {Columnas} FROM cliente {where} ORDER BY {columnaOrden} {sentido}, id_cliente ASC";

        var clientes = new List<Cliente>();

        await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(cancelacion);
        while (await lector.ReadAsync(cancelacion))
        {
            clientes.Add(Leer(lector));
        }

        return clientes;
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int idCliente, CancellationToken cancelacion)
    {
        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(
            $"SELECT {Columnas} FROM cliente WHERE id_cliente = @id", conexion);
        comando.Parameters.AddWithValue("id", idCliente);

        await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(cancelacion);

        return await lector.ReadAsync(cancelacion) ? Leer(lector) : null;
    }

    public async Task<bool> ExisteCiAsync(string ci, int? excluirIdCliente, CancellationToken cancelacion)
    {
        var sql = new StringBuilder(
            "SELECT EXISTS (SELECT 1 FROM cliente WHERE LOWER(ci) = LOWER(@ci)");

        if (excluirIdCliente.HasValue)
        {
            sql.Append(" AND id_cliente <> @excluir");
        }

        sql.Append(')');

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql.ToString(), conexion);
        comando.Parameters.AddWithValue("ci", ci);

        if (excluirIdCliente.HasValue)
        {
            comando.Parameters.AddWithValue("excluir", excluirIdCliente.Value);
        }

        return (bool)(await comando.ExecuteScalarAsync(cancelacion))!;
    }

    public async Task<int> InsertarAsync(Cliente cliente, string usuario, CancellationToken cancelacion)
    {
        const string sql = @"
            INSERT INTO cliente
                (ci, nombre, apellido, correo, telefono, direccion,
                 activo, fecha_creacion, usuario_creacion)
            VALUES
                (@ci, @nombre, @apellido, @correo, @telefono, @direccion,
                 TRUE, NOW(), @usuario)
            RETURNING id_cliente";

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        AgregarDatos(comando, cliente);
        comando.Parameters.AddWithValue("usuario", usuario);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancelacion));
    }

    public async Task<bool> ActualizarAsync(Cliente cliente, string usuario, CancellationToken cancelacion)
    {
        const string sql = @"
            UPDATE cliente
               SET ci = @ci,
                   nombre = @nombre,
                   apellido = @apellido,
                   correo = @correo,
                   telefono = @telefono,
                   direccion = @direccion,
                   fecha_modificacion = NOW(),
                   usuario_modificacion = @usuario
             WHERE id_cliente = @id";

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        AgregarDatos(comando, cliente);
        comando.Parameters.AddWithValue("usuario", usuario);
        comando.Parameters.AddWithValue("id", cliente.IdCliente);

        return await comando.ExecuteNonQueryAsync(cancelacion) > 0;
    }

    public Task<bool> DarDeBajaAsync(int idCliente, string usuario, CancellationToken cancelacion) =>
        CambiarEstadoAsync(idCliente, usuario, nuevoEstado: false, cancelacion);

    public Task<bool> ReactivarAsync(int idCliente, string usuario, CancellationToken cancelacion) =>
        CambiarEstadoAsync(idCliente, usuario, nuevoEstado: true, cancelacion);

    // ================== Producto genérico del Factory Method ==================

    /// <summary>
    /// Adaptador de <see cref="IGestionarRepository{T}"/>: delega en el
    /// listado con un filtro neutro y sin cancelación, según el contrato.
    /// </summary>
    public async Task<List<Cliente>> ObtenerTodosAsync() =>
        (await ListarAsync(
            new FiltroClientes(null, false, null, false),
            CancellationToken.None)).ToList();

    public Task<int> InsertarAsync(Cliente entidad) =>
        InsertarAsync(entidad, UsuarioGenerico, CancellationToken.None);

    public Task ActualizarAsync(Cliente entidad) =>
        ActualizarAsync(entidad, UsuarioGenerico, CancellationToken.None);

    /// <summary>Invierte el estado actual; el id de empleado se usa como auditoría.</summary>
    public async Task CambiarEstadoAsync(int id, int idEmpleadoResponsable)
    {
        Cliente? cliente = await ObtenerPorIdAsync(id, CancellationToken.None);
        if (cliente is null)
        {
            return;
        }

        string usuario = idEmpleadoResponsable.ToString();

        if (cliente.Activo)
        {
            await DarDeBajaAsync(id, usuario, CancellationToken.None);
        }
        else
        {
            await ReactivarAsync(id, usuario, CancellationToken.None);
        }
    }

    public async Task<List<Cliente>> MostrarActivosAsync() =>
        (await ListarAsync(
            new FiltroClientes(null, true, null, false),
            CancellationToken.None)).ToList();

    private async Task<bool> CambiarEstadoAsync(
        int idCliente,
        string usuario,
        bool nuevoEstado,
        CancellationToken cancelacion)
    {
        // La baja lógica registra quién y cuándo; la reactivación limpia esos
        // campos para que el estado y la auditoría nunca se contradigan.
        string sql = nuevoEstado
            ? @"
            UPDATE cliente
               SET activo = @estado,
                   fecha_modificacion = NOW(),
                   usuario_modificacion = @usuario,
                   fecha_baja = NULL,
                   usuario_baja = NULL
             WHERE id_cliente = @id AND activo <> @estado"
            : @"
            UPDATE cliente
               SET activo = @estado,
                   fecha_modificacion = NOW(),
                   usuario_modificacion = @usuario,
                   fecha_baja = NOW(),
                   usuario_baja = @usuario
             WHERE id_cliente = @id AND activo <> @estado";

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("estado", nuevoEstado);
        comando.Parameters.AddWithValue("usuario", usuario);
        comando.Parameters.AddWithValue("id", idCliente);

        return await comando.ExecuteNonQueryAsync(cancelacion) > 0;
    }

    private static void AgregarDatos(NpgsqlCommand comando, Cliente cliente)
    {
        comando.Parameters.AddWithValue("ci", (object?)cliente.Ci ?? DBNull.Value);
        comando.Parameters.AddWithValue("nombre", cliente.Nombre);
        comando.Parameters.AddWithValue("apellido", cliente.Apellido);
        comando.Parameters.AddWithValue("correo", (object?)cliente.Correo ?? DBNull.Value);
        comando.Parameters.AddWithValue("telefono", (object?)cliente.Telefono ?? DBNull.Value);
        comando.Parameters.AddWithValue("direccion", (object?)cliente.Direccion ?? DBNull.Value);
    }

    private static Cliente Leer(NpgsqlDataReader lector) => new()
    {
        IdCliente = lector.GetInt32(0),
        Ci = lector.IsDBNull(1) ? null : lector.GetString(1),
        Nombre = lector.GetString(2),
        Apellido = lector.GetString(3),
        Correo = lector.IsDBNull(4) ? null : lector.GetString(4),
        Telefono = lector.IsDBNull(5) ? null : lector.GetString(5),
        Direccion = lector.IsDBNull(6) ? null : lector.GetString(6),
        Activo = lector.GetBoolean(7),
        FechaCreacion = lector.GetDateTime(8),
        UsuarioCreacion = lector.GetString(9),
        FechaModificacion = lector.IsDBNull(10) ? null : lector.GetDateTime(10),
        UsuarioModificacion = lector.IsDBNull(11) ? null : lector.GetString(11),
        FechaBaja = lector.IsDBNull(12) ? null : lector.GetDateTime(12),
        UsuarioBaja = lector.IsDBNull(13) ? null : lector.GetString(13)
    };

    private static string EscaparComodines(string texto) =>
        texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
