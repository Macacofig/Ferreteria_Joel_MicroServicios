using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Repositorios;

/// <summary>Persistencia del historial de cambios de productos.</summary>
public sealed class MySqlHistoricoProductoRepository : IRepositoryHistoricoProducto
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlHistoricoProductoRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

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

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

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

        await connection.OpenAsync(ct);

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

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        command.Parameters.AddWithValue("@idProducto", idProducto);

        await connection.OpenAsync(ct);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<HistoricoProducto> registros = new();

        while (await reader.ReadAsync(ct))
        {
            registros.Add(new HistoricoProducto
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
            });
        }

        return registros;
    }

    private static decimal? DecimalNulo(MySqlDataReader reader, string nombre) =>
        reader[nombre] is DBNull ? null : Convert.ToDecimal(reader[nombre]);

    private static int? EnteroNulo(MySqlDataReader reader, string nombre) =>
        reader[nombre] is DBNull ? null : Convert.ToInt32(reader[nombre]);
}
