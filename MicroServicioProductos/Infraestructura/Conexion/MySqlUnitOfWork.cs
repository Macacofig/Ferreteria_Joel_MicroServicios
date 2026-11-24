using System.Data;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Conexion;

/// <summary>
/// Unidad de trabajo con una sola conexión por petición (scoped). Permite que
/// la actualización de un producto y el registro de su historial compartan la
/// misma transacción: si falla el segundo, se revierte el primero.
/// </summary>
public sealed class MySqlUnitOfWork : IMySqlUnitOfWork, IAsyncDisposable
{
    private readonly IDbConnectionFactory _connectionFactory;
    private MySqlConnection? _connection;
    private MySqlTransaction? _transaction;

    public MySqlUnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Conexión compartida, abierta de forma diferida. Se mantiene abierta
    /// durante toda la petición para que la transacción pueda reutilizarse.
    /// </summary>
    public MySqlConnection Connection
    {
        get
        {
            _connection ??= _connectionFactory.CreateConnection();

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            return _connection;
        }
    }

    public MySqlTransaction? Transaction => _transaction;

    public async Task IniciarAsync(CancellationToken ct = default)
    {
        // Si ya hay una transacción activa, se reutiliza. No se anidan.
        if (_transaction is not null)
        {
            return;
        }

        _transaction = await Connection.BeginTransactionAsync(ct);
    }

    public async Task ConfirmarAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync(ct);
        _transaction.Dispose();
        _transaction = null;
    }

    public async Task RevertirAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        // Se ignora el token de la petición: aunque el cliente haya cancelado,
        // el rollback debe completarse para no dejar bloqueos en MySQL.
        await _transaction.RollbackAsync(CancellationToken.None);
        _transaction.Dispose();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await RevertirAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
