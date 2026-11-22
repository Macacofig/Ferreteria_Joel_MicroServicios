using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Repositorios;

/// <summary>Consultas de categorías y marcas activas sobre bdproductos.</summary>
public sealed class MySqlCatalogoAuxiliarRepository : IRepositoryCatalogoAuxiliar
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlCatalogoAuxiliarRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Categoria>> ListarCategoriasActivasAsync(
        CancellationToken ct = default)
    {
        const string query = @"
            SELECT IdCategoria, Codigo, Nombre, Descripcion, Estado
            FROM categoria
            WHERE Estado = 1
            ORDER BY Nombre ASC";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        await connection.OpenAsync(ct);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<Categoria> categorias = new();

        while (await reader.ReadAsync(ct))
        {
            categorias.Add(new Categoria
            {
                IdCategoria = Convert.ToInt16(reader["IdCategoria"]),
                Codigo = reader["Codigo"] as string ?? string.Empty,
                Nombre = reader["Nombre"] as string ?? string.Empty,
                Descripcion = reader["Descripcion"] as string,
                Estado = Convert.ToByte(reader["Estado"])
            });
        }

        return categorias;
    }

    public async Task<IReadOnlyList<Marca>> ListarMarcasActivasAsync(
        CancellationToken ct = default)
    {
        const string query = @"
            SELECT IdMarca, Nombre, PaisOrigen, Estado
            FROM marca
            WHERE Estado = 1
            ORDER BY Nombre ASC";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        await connection.OpenAsync(ct);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<Marca> marcas = new();

        while (await reader.ReadAsync(ct))
        {
            marcas.Add(new Marca
            {
                IdMarca = Convert.ToInt16(reader["IdMarca"]),
                Nombre = reader["Nombre"] as string ?? string.Empty,
                PaisOrigen = reader["PaisOrigen"] as string,
                Estado = Convert.ToByte(reader["Estado"])
            });
        }

        return marcas;
    }

    public async Task<bool> ExisteCategoriaActivaAsync(
        short idCategoria,
        CancellationToken ct = default) =>
        await ExisteActivoAsync("categoria", "IdCategoria", idCategoria, ct);

    public async Task<bool> ExisteMarcaActivaAsync(
        short idMarca,
        CancellationToken ct = default) =>
        await ExisteActivoAsync("marca", "IdMarca", idMarca, ct);

    private async Task<bool> ExisteActivoAsync(
        string tabla,
        string columnaId,
        short id,
        CancellationToken ct)
    {
        // tabla y columnaId son literales internos; el identificador va parametrizado.
        string query = $@"
            SELECT 1
            FROM {tabla}
            WHERE {columnaId} = @id
              AND Estado = 1
            LIMIT 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        command.Parameters.AddWithValue("@id", id);

        await connection.OpenAsync(ct);

        return await command.ExecuteScalarAsync(ct) is not null;
    }
}
