using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Repositorios;

/// <summary>
/// Consultas y mantenimiento de categorías y marcas sobre bdproductos.
/// Todas las sentencias son ADO.NET parametrizado y usan las columnas reales
/// de Scripts/02-productos.sql, incluidas las de auditoría.
/// </summary>
public sealed class MySqlCatalogoAuxiliarRepository : IRepositoryCatalogoAuxiliar
{
    private const string ColumnasCategoria = @"
        IdCategoria, Codigo, Nombre, Descripcion, Estado,
        FechaRegistro, FechaActualizacion,
        UsuarioCreacion, FechaCreacion,
        UsuarioModificacion, FechaModificacion,
        UsuarioBaja, FechaBaja, IdEmpleadoResponsable";

    private const string ColumnasMarca = @"
        IdMarca, Nombre, PaisOrigen, Estado,
        FechaRegistro, FechaActualizacion,
        UsuarioCreacion, FechaCreacion,
        UsuarioModificacion, FechaModificacion,
        UsuarioBaja, FechaBaja, IdEmpleadoResponsable";

    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlCatalogoAuxiliarRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // ===================== Consultas =====================

    public async Task<IReadOnlyList<Categoria>> ListarCategoriasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        string query = $@"
            SELECT {ColumnasCategoria}
            FROM categoria
            {(incluirInactivos ? string.Empty : "WHERE Estado = 1")}
            ORDER BY Nombre ASC";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        await connection.OpenAsync(ct);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<Categoria> categorias = new();

        while (await reader.ReadAsync(ct))
        {
            categorias.Add(MapearCategoria(reader));
        }

        return categorias;
    }

    public async Task<IReadOnlyList<Marca>> ListarMarcasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        string query = $@"
            SELECT {ColumnasMarca}
            FROM marca
            {(incluirInactivos ? string.Empty : "WHERE Estado = 1")}
            ORDER BY Nombre ASC";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        await connection.OpenAsync(ct);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<Marca> marcas = new();

        while (await reader.ReadAsync(ct))
        {
            marcas.Add(MapearMarca(reader));
        }

        return marcas;
    }

    public async Task<Categoria?> ObtenerCategoriaPorIdAsync(
        short idCategoria,
        bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        string condicion = incluirInactivos
            ? "IdCategoria = @id"
            : "IdCategoria = @id AND Estado = 1";
        string query = $@"
            SELECT {ColumnasCategoria}
            FROM categoria
            WHERE {condicion}
            LIMIT 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", idCategoria);

        await connection.OpenAsync(ct);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        return await reader.ReadAsync(ct) ? MapearCategoria(reader) : null;
    }

    public async Task<Marca?> ObtenerMarcaPorIdAsync(
        short idMarca,
        bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        string condicion = incluirInactivos
            ? "IdMarca = @id"
            : "IdMarca = @id AND Estado = 1";
        string query = $@"
            SELECT {ColumnasMarca}
            FROM marca
            WHERE {condicion}
            LIMIT 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", idMarca);

        await connection.OpenAsync(ct);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        return await reader.ReadAsync(ct) ? MapearMarca(reader) : null;
    }

    public async Task<bool> ExisteCategoriaActivaAsync(
        short idCategoria,
        CancellationToken ct = default) =>
        await ExisteActivoAsync("categoria", "IdCategoria", idCategoria, ct);

    public async Task<bool> ExisteMarcaActivaAsync(
        short idMarca,
        CancellationToken ct = default) =>
        await ExisteActivoAsync("marca", "IdMarca", idMarca, ct);

    public async Task<bool> ExisteCodigoCategoriaAsync(
        string codigo,
        short? excluirIdCategoria = null,
        CancellationToken ct = default)
    {
        string exclusion = excluirIdCategoria.HasValue
            ? " AND IdCategoria <> @excluir"
            : string.Empty;

        string query = $@"
            SELECT 1
            FROM categoria
            WHERE Codigo = @codigo AND Estado = 1{exclusion}
            LIMIT 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@codigo", codigo);

        if (excluirIdCategoria.HasValue)
        {
            AgregarParametro(command, "@excluir", excluirIdCategoria.Value);
        }

        await connection.OpenAsync(ct);

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<string> ObtenerSiguienteCodigoCategoriaAsync(
        CancellationToken ct = default)
    {
        // Se consultan todas las categorías (activas e inactivas) para no
        // reutilizar códigos de bajas lógicas. El mayor IdCategoria se usa como
        // respaldo si algún registro tiene un código con otro formato.
        const string query = @"
            SELECT
                COALESCE(
                    MAX(CASE
                        WHEN c.Codigo REGEXP '^CAT-[0-9]{3}$'
                        THEN CAST(SUBSTRING(c.Codigo, 6) AS UNSIGNED)
                    END),
                    0
                ) AS UltimoCorrelativo,
                COALESCE(MAX(c.IdCategoria), 0) AS UltimoId
            FROM categoria c";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        await connection.OpenAsync(ct);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            return "CAT-001";
        }

        int ultimoCorrelativo = Convert.ToInt32(reader["UltimoCorrelativo"]);
        int ultimoId = Convert.ToInt32(reader["UltimoId"]);
        int siguiente = Math.Max(ultimoCorrelativo, ultimoId) + 1;

        return $"CAT-{siguiente:D3}";
    }

    public async Task<bool> ExisteNombreCategoriaAsync(
        string nombre,
        short? excluirIdCategoria = null,
        CancellationToken ct = default)
    {
        string exclusion = excluirIdCategoria.HasValue
            ? " AND IdCategoria <> @excluir"
            : string.Empty;

        string query = $@"
            SELECT 1
            FROM categoria
            WHERE Nombre = @nombre AND Estado = 1{exclusion}
            LIMIT 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@nombre", nombre);

        if (excluirIdCategoria.HasValue)
        {
            AgregarParametro(command, "@excluir", excluirIdCategoria.Value);
        }

        await connection.OpenAsync(ct);

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<bool> ExisteNombreMarcaAsync(
        string nombre,
        short? excluirIdMarca = null,
        CancellationToken ct = default)
    {
        string exclusion = excluirIdMarca.HasValue
            ? " AND IdMarca <> @excluir"
            : string.Empty;

        string query = $@"
            SELECT 1
            FROM marca
            WHERE Nombre = @nombre AND Estado = 1{exclusion}
            LIMIT 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@nombre", nombre);

        if (excluirIdMarca.HasValue)
        {
            AgregarParametro(command, "@excluir", excluirIdMarca.Value);
        }

        await connection.OpenAsync(ct);

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<bool> TieneProductosActivosCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default) =>
        await TieneProductosActivosAsync("IdCategoria", idCategoria, ct);

    public async Task<bool> TieneProductosActivosMarcaAsync(
        short idMarca,
        CancellationToken ct = default) =>
        await TieneProductosActivosAsync("IdMarca", idMarca, ct);

    // ===================== Categorías =====================

    public async Task<short> InsertarCategoriaAsync(
        Categoria categoria,
        CancellationToken ct = default)
    {
        const string query = @"
            INSERT INTO categoria
                (Codigo, Nombre, Descripcion, Estado,
                 FechaRegistro, FechaActualizacion,
                 UsuarioCreacion, FechaCreacion,
                 UsuarioModificacion, FechaModificacion,
                 UsuarioBaja, FechaBaja, IdEmpleadoResponsable)
            VALUES
                (@codigo, @nombre, @descripcion, @estado,
                 @fechaRegistro, @fechaActualizacion,
                 @usuarioCreacion, @fechaCreacion,
                 @usuarioModificacion, @fechaModificacion,
                 @usuarioBaja, @fechaBaja, @idEmpleadoResponsable);
            SELECT LAST_INSERT_ID();";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@codigo", categoria.Codigo);
        AgregarParametro(command, "@nombre", categoria.Nombre);
        AgregarParametro(command, "@descripcion", categoria.Descripcion);
        AgregarParametro(command, "@estado", categoria.Estado);
        AgregarParametro(command, "@fechaRegistro", categoria.FechaRegistro);
        AgregarParametro(command, "@fechaActualizacion", categoria.FechaActualizacion);
        AgregarParametro(command, "@usuarioCreacion", categoria.UsuarioCreacion);
        AgregarParametro(command, "@fechaCreacion", categoria.FechaCreacion);
        AgregarParametro(command, "@usuarioModificacion", categoria.UsuarioModificacion);
        AgregarParametro(command, "@fechaModificacion", categoria.FechaModificacion);
        AgregarParametro(command, "@usuarioBaja", categoria.UsuarioBaja);
        AgregarParametro(command, "@fechaBaja", categoria.FechaBaja);
        AgregarParametro(
            command, "@idEmpleadoResponsable", categoria.IdEmpleadoResponsable);

        await connection.OpenAsync(ct);

        object? id = await command.ExecuteScalarAsync(ct);

        return Convert.ToInt16(id);
    }

    public async Task<bool> ActualizarCategoriaAsync(
        Categoria categoria,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE categoria
            SET Codigo = @codigo,
                Nombre = @nombre,
                Descripcion = @descripcion,
                Estado = @estado,
                FechaActualizacion = @fechaActualizacion,
                UsuarioModificacion = @usuarioModificacion,
                FechaModificacion = @fechaModificacion,
                IdEmpleadoResponsable = @idEmpleadoResponsable
            WHERE IdCategoria = @id AND Estado = 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", categoria.IdCategoria);
        AgregarParametro(command, "@codigo", categoria.Codigo);
        AgregarParametro(command, "@nombre", categoria.Nombre);
        AgregarParametro(command, "@descripcion", categoria.Descripcion);
        AgregarParametro(command, "@estado", categoria.Estado);
        AgregarParametro(command, "@fechaActualizacion", categoria.FechaActualizacion);
        AgregarParametro(command, "@usuarioModificacion", categoria.UsuarioModificacion);
        AgregarParametro(command, "@fechaModificacion", categoria.FechaModificacion);
        AgregarParametro(
            command, "@idEmpleadoResponsable", categoria.IdEmpleadoResponsable);

        await connection.OpenAsync(ct);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> DarDeBajaCategoriaAsync(
        short idCategoria,
        int? idEmpleadoResponsable,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE categoria
            SET Estado = 0,
                FechaActualizacion = @ahora,
                UsuarioModificacion = @usuario,
                FechaModificacion = @ahora,
                UsuarioBaja = @usuario,
                FechaBaja = @ahora,
                IdEmpleadoResponsable = @usuario
            WHERE IdCategoria = @id AND Estado = 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", idCategoria);
        AgregarParametro(command, "@ahora", DateTime.Now);
        AgregarParametro(command, "@usuario", idEmpleadoResponsable);

        await connection.OpenAsync(ct);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    /// <summary>
    /// Reactiva una categoría dada de baja sin borrar la fila y limpiando
    /// los campos de baja, igual que el patrón de Proveedores.
    /// </summary>
    public async Task<bool> ReactivarCategoriaAsync(
        short idCategoria,
        int? idEmpleadoResponsable,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE categoria
            SET Estado = 1,
                FechaActualizacion = @ahora,
                UsuarioModificacion = @usuario,
                FechaModificacion = @ahora,
                UsuarioBaja = NULL,
                FechaBaja = NULL,
                IdEmpleadoResponsable = @usuario
            WHERE IdCategoria = @id AND Estado = 0";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", idCategoria);
        AgregarParametro(command, "@ahora", DateTime.Now);
        AgregarParametro(command, "@usuario", idEmpleadoResponsable);

        await connection.OpenAsync(ct);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    // ===================== Marcas =====================

    public async Task<short> InsertarMarcaAsync(
        Marca marca,
        CancellationToken ct = default)
    {
        const string query = @"
            INSERT INTO marca
                (Nombre, PaisOrigen, Estado,
                 FechaRegistro, FechaActualizacion,
                 UsuarioCreacion, FechaCreacion,
                 UsuarioModificacion, FechaModificacion,
                 UsuarioBaja, FechaBaja, IdEmpleadoResponsable)
            VALUES
                (@nombre, @paisOrigen, @estado,
                 @fechaRegistro, @fechaActualizacion,
                 @usuarioCreacion, @fechaCreacion,
                 @usuarioModificacion, @fechaModificacion,
                 @usuarioBaja, @fechaBaja, @idEmpleadoResponsable);
            SELECT LAST_INSERT_ID();";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@nombre", marca.Nombre);
        AgregarParametro(command, "@paisOrigen", marca.PaisOrigen);
        AgregarParametro(command, "@estado", marca.Estado);
        AgregarParametro(command, "@fechaRegistro", marca.FechaRegistro);
        AgregarParametro(command, "@fechaActualizacion", marca.FechaActualizacion);
        AgregarParametro(command, "@usuarioCreacion", marca.UsuarioCreacion);
        AgregarParametro(command, "@fechaCreacion", marca.FechaCreacion);
        AgregarParametro(command, "@usuarioModificacion", marca.UsuarioModificacion);
        AgregarParametro(command, "@fechaModificacion", marca.FechaModificacion);
        AgregarParametro(command, "@usuarioBaja", marca.UsuarioBaja);
        AgregarParametro(command, "@fechaBaja", marca.FechaBaja);
        AgregarParametro(
            command, "@idEmpleadoResponsable", marca.IdEmpleadoResponsable);

        await connection.OpenAsync(ct);

        object? id = await command.ExecuteScalarAsync(ct);

        return Convert.ToInt16(id);
    }

    public async Task<bool> ActualizarMarcaAsync(
        Marca marca,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE marca
            SET Nombre = @nombre,
                PaisOrigen = @paisOrigen,
                Estado = @estado,
                FechaActualizacion = @fechaActualizacion,
                UsuarioModificacion = @usuarioModificacion,
                FechaModificacion = @fechaModificacion,
                IdEmpleadoResponsable = @idEmpleadoResponsable
            WHERE IdMarca = @id AND Estado = 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", marca.IdMarca);
        AgregarParametro(command, "@nombre", marca.Nombre);
        AgregarParametro(command, "@paisOrigen", marca.PaisOrigen);
        AgregarParametro(command, "@estado", marca.Estado);
        AgregarParametro(command, "@fechaActualizacion", marca.FechaActualizacion);
        AgregarParametro(command, "@usuarioModificacion", marca.UsuarioModificacion);
        AgregarParametro(command, "@fechaModificacion", marca.FechaModificacion);
        AgregarParametro(
            command, "@idEmpleadoResponsable", marca.IdEmpleadoResponsable);

        await connection.OpenAsync(ct);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> DarDeBajaMarcaAsync(
        short idMarca,
        int? idEmpleadoResponsable,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE marca
            SET Estado = 0,
                FechaActualizacion = @ahora,
                UsuarioModificacion = @usuario,
                FechaModificacion = @ahora,
                UsuarioBaja = @usuario,
                FechaBaja = @ahora,
                IdEmpleadoResponsable = @usuario
            WHERE IdMarca = @id AND Estado = 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", idMarca);
        AgregarParametro(command, "@ahora", DateTime.Now);
        AgregarParametro(command, "@usuario", idEmpleadoResponsable);

        await connection.OpenAsync(ct);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    /// <summary>
    /// Reactiva una marca dada de baja sin borrar la fila y limpiando
    /// los campos de baja, igual que el patrón de Proveedores.
    /// </summary>
    public async Task<bool> ReactivarMarcaAsync(
        short idMarca,
        int? idEmpleadoResponsable,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE marca
            SET Estado = 1,
                FechaActualizacion = @ahora,
                UsuarioModificacion = @usuario,
                FechaModificacion = @ahora,
                UsuarioBaja = NULL,
                FechaBaja = NULL,
                IdEmpleadoResponsable = @usuario
            WHERE IdMarca = @id AND Estado = 0";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", idMarca);
        AgregarParametro(command, "@ahora", DateTime.Now);
        AgregarParametro(command, "@usuario", idEmpleadoResponsable);

        await connection.OpenAsync(ct);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    // ===================== Auxiliares =====================

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

        AgregarParametro(command, "@id", id);

        await connection.OpenAsync(ct);

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    private async Task<bool> TieneProductosActivosAsync(
        string columnaId,
        short id,
        CancellationToken ct)
    {
        // columnaId es un literal interno; el valor va parametrizado.
        string query = $@"
            SELECT 1
            FROM producto
            WHERE {columnaId} = @id
              AND Estado = 1
            LIMIT 1";

        using MySqlConnection connection = _connectionFactory.CreateConnection();
        using MySqlCommand command = new(query, connection);

        AgregarParametro(command, "@id", id);

        await connection.OpenAsync(ct);

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    private static void AgregarParametro(
        MySqlCommand command,
        string nombre,
        object? valor) =>
        command.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);

    private static Categoria MapearCategoria(MySqlDataReader reader) => new()
    {
        IdCategoria = Convert.ToInt16(reader["IdCategoria"]),
        Codigo = reader["Codigo"] as string ?? string.Empty,
        Nombre = reader["Nombre"] as string ?? string.Empty,
        Descripcion = reader["Descripcion"] as string,
        Estado = Convert.ToByte(reader["Estado"]),
        FechaRegistro = LeerFecha(reader, "FechaRegistro") ?? DateTime.MinValue,
        FechaActualizacion = LeerFecha(reader, "FechaActualizacion"),
        UsuarioCreacion = LeerEntero(reader, "UsuarioCreacion"),
        FechaCreacion = LeerFecha(reader, "FechaCreacion"),
        UsuarioModificacion = LeerEntero(reader, "UsuarioModificacion"),
        FechaModificacion = LeerFecha(reader, "FechaModificacion"),
        UsuarioBaja = LeerEntero(reader, "UsuarioBaja"),
        FechaBaja = LeerFecha(reader, "FechaBaja"),
        IdEmpleadoResponsable = LeerEntero(reader, "IdEmpleadoResponsable")
    };

    private static Marca MapearMarca(MySqlDataReader reader) => new()
    {
        IdMarca = Convert.ToInt16(reader["IdMarca"]),
        Nombre = reader["Nombre"] as string ?? string.Empty,
        PaisOrigen = reader["PaisOrigen"] as string,
        Estado = Convert.ToByte(reader["Estado"]),
        FechaRegistro = LeerFecha(reader, "FechaRegistro") ?? DateTime.MinValue,
        FechaActualizacion = LeerFecha(reader, "FechaActualizacion"),
        UsuarioCreacion = LeerEntero(reader, "UsuarioCreacion"),
        FechaCreacion = LeerFecha(reader, "FechaCreacion"),
        UsuarioModificacion = LeerEntero(reader, "UsuarioModificacion"),
        FechaModificacion = LeerFecha(reader, "FechaModificacion"),
        UsuarioBaja = LeerEntero(reader, "UsuarioBaja"),
        FechaBaja = LeerFecha(reader, "FechaBaja"),
        IdEmpleadoResponsable = LeerEntero(reader, "IdEmpleadoResponsable")
    };

    private static int? LeerEntero(MySqlDataReader reader, string columna) =>
        reader[columna] is DBNull
            ? null
            : Convert.ToInt32(reader[columna]);

    private static DateTime? LeerFecha(MySqlDataReader reader, string columna) =>
        reader[columna] is DBNull
            ? null
            : Convert.ToDateTime(reader[columna]);
}
