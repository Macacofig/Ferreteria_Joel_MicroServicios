using System.Text;
using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Dominio.Puertos;
using Npgsql;

namespace MicroServicioClientes.Infraestructura.Persistencia;

/// <summary>
/// Adaptador de salida (patrón Adapter + Repository): traduce el puerto
/// <see cref="IClienteRepositorio"/> a consultas ADO.NET sobre PostgreSQL.
/// </summary>
public sealed class PostgresClienteRepositorio : IClienteRepositorio
{
    private const string Columnas = @"
        c.id_cliente, c.nombre, c.documento, c.correo, c.telefono, c.direccion,
        c.id_tipo_cliente, t.nombre, t.descuento_porcentaje, c.activo,
        c.fecha_creacion, c.usuario_creacion, c.fecha_modificacion, c.usuario_modificacion";

    private const string Origen = @"
        FROM cliente c
        INNER JOIN tipo_cliente t ON t.id_tipo_cliente = c.id_tipo_cliente";

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
            condiciones.Add("c.activo = TRUE");
        }

        if (filtro.IdTipoCliente is int idTipo)
        {
            condiciones.Add("c.id_tipo_cliente = @tipo");
            comando.Parameters.AddWithValue("tipo", idTipo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            condiciones.Add(
                "(c.nombre ILIKE @patron OR c.documento ILIKE @patron OR COALESCE(c.correo, '') ILIKE @patron)");
            comando.Parameters.AddWithValue("patron", $"%{EscaparComodines(filtro.Buscar.Trim())}%");
        }

        string where = condiciones.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", condiciones);

        // Solo se interpolan constantes de una lista blanca: nunca texto del usuario.
        string columnaOrden = filtro.OrdenarPor?.ToLowerInvariant() switch
        {
            "documento" => "c.documento",
            "tipo" => "t.nombre",
            "fecha" => "c.fecha_creacion",
            _ => "c.nombre"
        };

        string sentido = filtro.Descendente ? "DESC" : "ASC";

        comando.CommandText =
            $"SELECT {Columnas} {Origen} {where} ORDER BY {columnaOrden} {sentido}, c.id_cliente ASC";

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
            $"SELECT {Columnas} {Origen} WHERE c.id_cliente = @id", conexion);
        comando.Parameters.AddWithValue("id", idCliente);

        await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(cancelacion);

        return await lector.ReadAsync(cancelacion) ? Leer(lector) : null;
    }

    public async Task<bool> ExisteDocumentoAsync(
        string documento,
        int? excluirIdCliente,
        CancellationToken cancelacion)
    {
        var sql = new StringBuilder(
            "SELECT EXISTS (SELECT 1 FROM cliente WHERE LOWER(documento) = LOWER(@documento)");

        if (excluirIdCliente.HasValue)
        {
            sql.Append(" AND id_cliente <> @excluir");
        }

        sql.Append(')');

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql.ToString(), conexion);
        comando.Parameters.AddWithValue("documento", documento);

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
                (nombre, documento, correo, telefono, direccion, id_tipo_cliente,
                 activo, fecha_creacion, usuario_creacion)
            VALUES
                (@nombre, @documento, @correo, @telefono, @direccion, @tipo,
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
               SET nombre = @nombre,
                   documento = @documento,
                   correo = @correo,
                   telefono = @telefono,
                   direccion = @direccion,
                   id_tipo_cliente = @tipo,
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

    public async Task<bool> DarDeBajaAsync(int idCliente, string usuario, CancellationToken cancelacion)
    {
        const string sql = @"
            UPDATE cliente
               SET activo = FALSE,
                   fecha_modificacion = NOW(),
                   usuario_modificacion = @usuario
             WHERE id_cliente = @id AND activo = TRUE";

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("usuario", usuario);
        comando.Parameters.AddWithValue("id", idCliente);

        return await comando.ExecuteNonQueryAsync(cancelacion) > 0;
    }

    private static void AgregarDatos(NpgsqlCommand comando, Cliente cliente)
    {
        comando.Parameters.AddWithValue("nombre", cliente.Nombre);
        comando.Parameters.AddWithValue("documento", cliente.Documento);
        comando.Parameters.AddWithValue("correo", (object?)cliente.Correo ?? DBNull.Value);
        comando.Parameters.AddWithValue("telefono", cliente.Telefono);
        comando.Parameters.AddWithValue("direccion", (object?)cliente.Direccion ?? DBNull.Value);
        comando.Parameters.AddWithValue("tipo", cliente.IdTipoCliente);
    }

    private static Cliente Leer(NpgsqlDataReader lector) => new()
    {
        IdCliente = lector.GetInt32(0),
        Nombre = lector.GetString(1),
        Documento = lector.GetString(2),
        Correo = lector.IsDBNull(3) ? null : lector.GetString(3),
        Telefono = lector.GetString(4),
        Direccion = lector.IsDBNull(5) ? null : lector.GetString(5),
        IdTipoCliente = lector.GetInt32(6),
        TipoClienteNombre = lector.GetString(7),
        DescuentoPorcentaje = lector.GetDecimal(8),
        Activo = lector.GetBoolean(9),
        FechaCreacion = lector.GetDateTime(10),
        UsuarioCreacion = lector.GetString(11),
        FechaModificacion = lector.IsDBNull(12) ? null : lector.GetDateTime(12),
        UsuarioModificacion = lector.IsDBNull(13) ? null : lector.GetString(13)
    };

    private static string EscaparComodines(string texto) =>
        texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
