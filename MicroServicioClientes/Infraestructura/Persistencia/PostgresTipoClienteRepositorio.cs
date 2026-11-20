using System.Text;
using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Dominio.Puertos;
using Npgsql;

namespace MicroServicioClientes.Infraestructura.Persistencia;

/// <summary>Adaptador de salida de <see cref="ITipoClienteRepositorio"/> sobre PostgreSQL.</summary>
public sealed class PostgresTipoClienteRepositorio : ITipoClienteRepositorio
{
    private const string Columnas = @"
        id_tipo_cliente, nombre, descuento_porcentaje, descripcion, activo,
        fecha_creacion, usuario_creacion, fecha_modificacion, usuario_modificacion";

    private readonly IFabricaConexion _fabrica;

    public PostgresTipoClienteRepositorio(IFabricaConexion fabrica)
    {
        _fabrica = fabrica;
    }

    public async Task<IReadOnlyList<TipoCliente>> ListarAsync(bool soloActivos, CancellationToken cancelacion)
    {
        string where = soloActivos ? "WHERE activo = TRUE" : string.Empty;

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(
            $"SELECT {Columnas} FROM tipo_cliente {where} ORDER BY nombre ASC", conexion);

        var tipos = new List<TipoCliente>();

        await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(cancelacion);
        while (await lector.ReadAsync(cancelacion))
        {
            tipos.Add(Leer(lector));
        }

        return tipos;
    }

    public async Task<TipoCliente?> ObtenerPorIdAsync(int idTipoCliente, CancellationToken cancelacion)
    {
        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(
            $"SELECT {Columnas} FROM tipo_cliente WHERE id_tipo_cliente = @id", conexion);
        comando.Parameters.AddWithValue("id", idTipoCliente);

        await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(cancelacion);

        return await lector.ReadAsync(cancelacion) ? Leer(lector) : null;
    }

    public async Task<bool> ExisteNombreAsync(
        string nombre,
        int? excluirIdTipoCliente,
        CancellationToken cancelacion)
    {
        var sql = new StringBuilder(
            "SELECT EXISTS (SELECT 1 FROM tipo_cliente WHERE LOWER(nombre) = LOWER(@nombre)");

        if (excluirIdTipoCliente.HasValue)
        {
            sql.Append(" AND id_tipo_cliente <> @excluir");
        }

        sql.Append(')');

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql.ToString(), conexion);
        comando.Parameters.AddWithValue("nombre", nombre);

        if (excluirIdTipoCliente.HasValue)
        {
            comando.Parameters.AddWithValue("excluir", excluirIdTipoCliente.Value);
        }

        return (bool)(await comando.ExecuteScalarAsync(cancelacion))!;
    }

    public async Task<int> InsertarAsync(TipoCliente tipo, string usuario, CancellationToken cancelacion)
    {
        const string sql = @"
            INSERT INTO tipo_cliente
                (nombre, descuento_porcentaje, descripcion, activo, fecha_creacion, usuario_creacion)
            VALUES
                (@nombre, @descuento, @descripcion, @activo, NOW(), @usuario)
            RETURNING id_tipo_cliente";

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        AgregarDatos(comando, tipo);
        comando.Parameters.AddWithValue("usuario", usuario);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancelacion));
    }

    public async Task<bool> ActualizarAsync(TipoCliente tipo, string usuario, CancellationToken cancelacion)
    {
        const string sql = @"
            UPDATE tipo_cliente
               SET nombre = @nombre,
                   descuento_porcentaje = @descuento,
                   descripcion = @descripcion,
                   activo = @activo,
                   fecha_modificacion = NOW(),
                   usuario_modificacion = @usuario
             WHERE id_tipo_cliente = @id";

        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(sql, conexion);
        AgregarDatos(comando, tipo);
        comando.Parameters.AddWithValue("usuario", usuario);
        comando.Parameters.AddWithValue("id", tipo.IdTipoCliente);

        return await comando.ExecuteNonQueryAsync(cancelacion) > 0;
    }

    public async Task<bool> EliminarAsync(int idTipoCliente, CancellationToken cancelacion)
    {
        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(
            "DELETE FROM tipo_cliente WHERE id_tipo_cliente = @id", conexion);
        comando.Parameters.AddWithValue("id", idTipoCliente);

        return await comando.ExecuteNonQueryAsync(cancelacion) > 0;
    }

    public async Task<int> ContarClientesAsync(int idTipoCliente, CancellationToken cancelacion)
    {
        await using NpgsqlConnection conexion = _fabrica.Crear();
        await conexion.OpenAsync(cancelacion);

        await using var comando = new NpgsqlCommand(
            "SELECT COUNT(*) FROM cliente WHERE id_tipo_cliente = @id", conexion);
        comando.Parameters.AddWithValue("id", idTipoCliente);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancelacion));
    }

    private static void AgregarDatos(NpgsqlCommand comando, TipoCliente tipo)
    {
        comando.Parameters.AddWithValue("nombre", tipo.Nombre);
        comando.Parameters.AddWithValue("descuento", tipo.DescuentoPorcentaje);
        comando.Parameters.AddWithValue("descripcion", (object?)tipo.Descripcion ?? DBNull.Value);
        comando.Parameters.AddWithValue("activo", tipo.Activo);
    }

    private static TipoCliente Leer(NpgsqlDataReader lector) => new()
    {
        IdTipoCliente = lector.GetInt32(0),
        Nombre = lector.GetString(1),
        DescuentoPorcentaje = lector.GetDecimal(2),
        Descripcion = lector.IsDBNull(3) ? null : lector.GetString(3),
        Activo = lector.GetBoolean(4),
        FechaCreacion = lector.GetDateTime(5),
        UsuarioCreacion = lector.GetString(6),
        FechaModificacion = lector.IsDBNull(7) ? null : lector.GetDateTime(7),
        UsuarioModificacion = lector.IsDBNull(8) ? null : lector.GetString(8)
    };
}