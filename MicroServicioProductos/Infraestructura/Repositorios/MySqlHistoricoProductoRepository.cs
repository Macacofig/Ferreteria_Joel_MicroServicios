using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Repositorios;

/// <summary>Persistencia del historial de cambios de productos.</summary>
public sealed class MySqlHistoricoProductoRepository : IRepositoryHistoricoProducto
{
    private readonly IMySqlUnitOfWork _unidad;

    public MySqlHistoricoProductoRepository(IMySqlUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    /// <summary>
    /// Comparte la conexión y la transacción del producto que se está
    /// modificando: el historial se graba en el mismo commit.
    /// </summary>
    private MySqlCommand CrearComando(string sql)
    {
        MySqlCommand command = _unidad.Connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _unidad.Transaction;
        return command;
    }

    // ================== Producto genérico del Factory Method ==================

    /// <summary>
    /// Adaptador de <see cref="IRepository{T}"/>: el puerto específico solo
    /// consulta por producto; aquí se listan todos los registros.
    /// </summary>
    public Task<List<HistoricoProducto>> ObtenerTodosAsync() =>
        ObtenerTodosAsync(CancellationToken.None);

    private async Task<List<HistoricoProducto>> ObtenerTodosAsync(
        CancellationToken ct)
    {
        const string query = @"
            SELECT IdHistorico, IdProducto, Accion, PrecioAnterior, PrecioNuevo,
                   StockAnterior, StockNuevo, Detalle, FechaCambio,
                   IdEmpleadoResponsable
            FROM historico_producto
            ORDER BY FechaCambio DESC, IdHistorico DESC";

        using MySqlCommand command = CrearComando(query);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<HistoricoProducto> registros = new();
        while (await reader.ReadAsync(ct))
        {
            registros.Add(Mapear(reader));
        }

        return registros;
    }

    /// <summary>
    /// El puerto específico registra sin devolver identificador; el contrato
    /// genérico exige uno y se reporta 0 porque no se consulta LAST_INSERT_ID().
    /// </summary>
    public async Task<int> InsertarAsync(HistoricoProducto entidad)
    {
        await RegistrarAsync(entidad, CancellationToken.None);
        return 0;
    }

    /// <summary>
    /// Inscribe el INSERT en la transacción activa de la unidad de trabajo.
    /// Cuando lo invoca ServicioProductos, la UPDATE del producto y este INSERT
    /// se confirman o se revierten juntos.
    /// </summary>
    public async Task RegistrarAsync(
        HistoricoProducto registro,
        CancellationToken ct = default)
    {
        const string query = @"
            INSERT INTO historico_producto
            (
                IdProducto,
                Accion,
                PrecioAnterior,
                PrecioNuevo,
                StockAnterior,
                StockNuevo,
                Detalle,
                FechaCambio,
                IdEmpleadoResponsable
            )
            VALUES
            (
                @idProducto,
                @accion,
                @precioAnterior,
                @precioNuevo,
                @stockAnterior,
                @stockNuevo,
                @detalle,
                @fechaCambio,
                @idEmpleadoResponsable
            )";

        if (registro.FechaCambio == default)
        {
            registro.FechaCambio = DateTime.Now;
        }

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue("@idProducto", registro.IdProducto);
        command.Parameters.AddWithValue("@accion", registro.Accion);
        command.Parameters.AddWithValue(
            "@precioAnterior",
            (object?)registro.PrecioAnterior ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@precioNuevo",
            (object?)registro.PrecioNuevo ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@stockAnterior",
            (object?)registro.StockAnterior ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@stockNuevo",
            (object?)registro.StockNuevo ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@detalle",
            (object?)registro.Detalle ?? DBNull.Value);
        command.Parameters.AddWithValue("@fechaCambio", registro.FechaCambio);
        command.Parameters.AddWithValue(
            "@idEmpleadoResponsable",
            (object?)registro.IdEmpleadoResponsable ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<HistoricoProducto>> ListarPorProductoAsync(
        int idProducto,
        CancellationToken ct = default)
    {
        const string query = @"
            SELECT IdHistorico, IdProducto, Accion, PrecioAnterior, PrecioNuevo,
                   StockAnterior, StockNuevo, Detalle, FechaCambio,
                   IdEmpleadoResponsable
            FROM historico_producto
            WHERE IdProducto = @idProducto
            ORDER BY FechaCambio DESC, IdHistorico DESC";

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue("@idProducto", idProducto);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<HistoricoProducto> registros = new();

        while (await reader.ReadAsync(ct))
        {
            registros.Add(Mapear(reader));
        }

        return registros;
    }

    private static HistoricoProducto Mapear(MySqlDataReader reader) => new()
    {
        IdHistorico = Convert.ToInt32(reader["IdHistorico"]),
        IdProducto = Convert.ToInt32(reader["IdProducto"]),
        Accion = reader["Accion"] as string ?? string.Empty,
        PrecioAnterior = DecimalNulo(reader, "PrecioAnterior"),
        PrecioNuevo = DecimalNulo(reader, "PrecioNuevo"),
        StockAnterior = EnteroNulo(reader, "StockAnterior"),
        StockNuevo = EnteroNulo(reader, "StockNuevo"),
        Detalle = reader["Detalle"] as string,
        FechaCambio = Convert.ToDateTime(reader["FechaCambio"]),
        IdEmpleadoResponsable = EnteroNulo(reader, "IdEmpleadoResponsable")
    };

    private static decimal? DecimalNulo(MySqlDataReader reader, string nombre) =>
        reader[nombre] is DBNull ? null : Convert.ToDecimal(reader[nombre]);

    private static int? EnteroNulo(MySqlDataReader reader, string nombre) =>
        reader[nombre] is DBNull ? null : Convert.ToInt32(reader[nombre]);
}
