using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Repositorios;

/// <summary>
/// Repositorio de productos sobre MySQL (bdproductos). Todas las sentencias
/// son parametrizadas; el ORDER BY se arma con una lista blanca para evitar
/// inyección SQL.
/// </summary>
public sealed class MySqlProductoRepository : IRepositoryProducto
{
    private const string ColumnasProducto = @"
        p.IdProducto,
        p.IdCategoria,
        p.IdMarca,
        p.Codigo,
        p.Nombre,
        p.Descripcion,
        p.UnidadMedida,
        p.PrecioVenta,
        p.CantidadStock,
        p.Estado,
        c.Nombre AS CategoriaNombre,
        m.Nombre AS MarcaNombre,
        p.FechaRegistro,
        p.FechaActualizacion,
        p.UsuarioCreacion,
        p.FechaCreacion,
        p.UsuarioModificacion,
        p.FechaModificacion,
        p.UsuarioBaja,
        p.FechaBaja,
        p.IdEmpleadoResponsable";

    private const string FromJoins = @"
        FROM producto p
        INNER JOIN categoria c ON c.IdCategoria = p.IdCategoria
        INNER JOIN marca m ON m.IdMarca = p.IdMarca";

    private readonly IMySqlUnitOfWork _unidad;

    public MySqlProductoRepository(IMySqlUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    /// <summary>
    /// Crea un comando en la conexión compartida y lo inscribe en la
    /// transacción activa, si la hay. Así producto e historial quedan atómicos.
    /// </summary>
    private MySqlCommand CrearComando(string sql)
    {
        MySqlCommand command = _unidad.Connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _unidad.Transaction;
        return command;
    }

    public async Task<IReadOnlyList<Producto>> ListarAsync(
        string? ordenarPor,
        string? direccion,
        bool incluirInactivos,
        CancellationToken ct = default)
    {
        string columna = (ordenarPor?.Trim().ToLowerInvariant()) switch
        {
            "codigo" => "p.Codigo",
            "nombre" => "p.Nombre",
            "precio" => "p.PrecioVenta",
            "stock" => "p.CantidadStock",
            "categoria" => "c.Nombre",
            "marca" => "m.Nombre",
            _ => "p.IdProducto"
        };

        string sentido = string.Equals(
            direccion, "DESC", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        string orden = columna == "p.IdProducto"
            ? $"{columna} {sentido}"
            : $"{columna} {sentido}, p.IdProducto ASC";

        string query = $@"
            SELECT {ColumnasProducto}
            {FromJoins}
            WHERE (@incluirInactivos = 1 OR p.Estado = 1)
            ORDER BY {orden}";

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue(
            "@incluirInactivos", incluirInactivos ? 1 : 0);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<Producto> productos = new();

        while (await reader.ReadAsync(ct))
        {
            productos.Add(MapearProducto(reader));
        }

        return productos;
    }

    public async Task<Producto?> ObtenerPorIdAsync(
        int idProducto,
        CancellationToken ct = default)
    {
        string query = $@"
            SELECT {ColumnasProducto}
            {FromJoins}
            WHERE p.IdProducto = @id
            LIMIT 1";

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue("@id", idProducto);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        return await reader.ReadAsync(ct)
            ? MapearProducto(reader)
            : null;
    }

    public async Task<bool> ExisteCodigoAsync(
        string codigo,
        int? excluirIdProducto = null,
        CancellationToken ct = default)
    {
        const string query = @"
            SELECT 1
            FROM producto
            WHERE Codigo = @codigo
              AND (@excluirId IS NULL OR IdProducto <> @excluirId)
            LIMIT 1";

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue("@codigo", codigo);
        command.Parameters.AddWithValue(
            "@excluirId",
            (object?)excluirIdProducto ?? DBNull.Value);

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<string> ObtenerSiguienteCodigoAsync(
        CancellationToken ct = default)
    {
        // Se consultan todos los productos (activos e inactivos) para no
        // reutilizar códigos de bajas lógicas. El mayor IdProducto se usa como
        // respaldo si algún registro tiene un código con otro formato.
        const string query = @"
            SELECT
                COALESCE(
                    MAX(CASE
                        WHEN p.Codigo REGEXP '^PROD-[0-9]{3}$'
                        THEN CAST(SUBSTRING(p.Codigo, 6) AS UNSIGNED)
                    END),
                    0
                ) AS UltimoCorrelativo,
                COALESCE(MAX(p.IdProducto), 0) AS UltimoId
            FROM producto p";

        using MySqlCommand command = CrearComando(query);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            return "PROD-001";
        }

        int ultimoCorrelativo = Convert.ToInt32(reader["UltimoCorrelativo"]);
        int ultimoId = Convert.ToInt32(reader["UltimoId"]);
        int siguiente = Math.Max(ultimoCorrelativo, ultimoId) + 1;

        return $"PROD-{siguiente:D3}";
    }

    public async Task<int> InsertarAsync(
        Producto producto,
        CancellationToken ct = default)
    {
        const string query = @"
            INSERT INTO producto
            (
                IdCategoria,
                IdMarca,
                Codigo,
                Nombre,
                Descripcion,
                UnidadMedida,
                PrecioVenta,
                CantidadStock,
                Estado,
                UsuarioCreacion,
                FechaCreacion,
                IdEmpleadoResponsable
            )
            VALUES
            (
                @idCategoria,
                @idMarca,
                @codigo,
                @nombre,
                @descripcion,
                @unidadMedida,
                @precioVenta,
                @cantidadStock,
                @estado,
                @usuarioCreacion,
                @fechaCreacion,
                @idEmpleadoResponsable
            )";

        DateTime ahora = DateTime.Now;
        producto.UsuarioCreacion ??= producto.IdEmpleadoResponsable;
        producto.FechaCreacion ??= ahora;
        producto.FechaRegistro = ahora;

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue("@idCategoria", producto.IdCategoria);
        command.Parameters.AddWithValue("@idMarca", producto.IdMarca);
        command.Parameters.AddWithValue("@codigo", producto.Codigo);
        command.Parameters.AddWithValue("@nombre", producto.Nombre);
        command.Parameters.AddWithValue(
            "@descripcion",
            (object?)producto.Descripcion ?? DBNull.Value);
        command.Parameters.AddWithValue("@unidadMedida", producto.UnidadMedida);
        command.Parameters.AddWithValue("@precioVenta", producto.PrecioVenta);
        command.Parameters.AddWithValue("@cantidadStock", producto.CantidadStock);
        command.Parameters.AddWithValue("@estado", producto.Estado);
        command.Parameters.AddWithValue(
            "@usuarioCreacion",
            (object?)producto.UsuarioCreacion ?? DBNull.Value);
        command.Parameters.AddWithValue("@fechaCreacion", ahora);
        command.Parameters.AddWithValue(
            "@idEmpleadoResponsable",
            (object?)producto.IdEmpleadoResponsable ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(ct);

        producto.IdProducto = Convert.ToInt32(command.LastInsertedId);

        return producto.IdProducto;
    }

    public async Task<bool> ActualizarAsync(
        Producto producto,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE producto
            SET IdCategoria = @idCategoria,
                IdMarca = @idMarca,
                Codigo = @codigo,
                Nombre = @nombre,
                Descripcion = @descripcion,
                UnidadMedida = @unidadMedida,
                PrecioVenta = @precioVenta,
                CantidadStock = @cantidadStock,
                Estado = @estado,
                FechaActualizacion = @fechaActualizacion,
                UsuarioModificacion = @usuarioModificacion,
                FechaModificacion = @fechaModificacion,
                IdEmpleadoResponsable = @idEmpleadoResponsable
            WHERE IdProducto = @id";

        DateTime ahora = DateTime.Now;
        producto.FechaActualizacion = ahora;
        producto.UsuarioModificacion ??= producto.IdEmpleadoResponsable;

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue("@id", producto.IdProducto);
        command.Parameters.AddWithValue("@idCategoria", producto.IdCategoria);
        command.Parameters.AddWithValue("@idMarca", producto.IdMarca);
        command.Parameters.AddWithValue("@codigo", producto.Codigo);
        command.Parameters.AddWithValue("@nombre", producto.Nombre);
        command.Parameters.AddWithValue(
            "@descripcion",
            (object?)producto.Descripcion ?? DBNull.Value);
        command.Parameters.AddWithValue("@unidadMedida", producto.UnidadMedida);
        command.Parameters.AddWithValue("@precioVenta", producto.PrecioVenta);
        command.Parameters.AddWithValue("@cantidadStock", producto.CantidadStock);
        command.Parameters.AddWithValue("@estado", producto.Estado);
        command.Parameters.AddWithValue("@fechaActualizacion", ahora);
        command.Parameters.AddWithValue(
            "@usuarioModificacion",
            (object?)producto.UsuarioModificacion ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@fechaModificacion",
            (object?)producto.FechaModificacion ?? ahora);
        command.Parameters.AddWithValue(
            "@idEmpleadoResponsable",
            (object?)producto.IdEmpleadoResponsable ?? DBNull.Value);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> DarDeBajaAsync(
        int idProducto,
        int? usuarioBaja,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE producto
            SET Estado = 0,
                UsuarioBaja = @usuarioBaja,
                FechaBaja = @fechaBaja,
                FechaActualizacion = @fechaActualizacion,
                UsuarioModificacion = @usuarioBaja,
                FechaModificacion = @fechaBaja,
                IdEmpleadoResponsable = @idEmpleadoResponsable
            WHERE IdProducto = @id
              AND Estado = 1";

        DateTime ahora = DateTime.Now;

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue("@id", idProducto);
        command.Parameters.AddWithValue(
            "@usuarioBaja",
            (object?)usuarioBaja ?? DBNull.Value);
        command.Parameters.AddWithValue("@fechaBaja", ahora);
        command.Parameters.AddWithValue("@fechaActualizacion", ahora);
        command.Parameters.AddWithValue(
            "@idEmpleadoResponsable",
            (object?)usuarioBaja ?? DBNull.Value);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    /// <summary>
    /// Reactiva un producto dado de baja sin borrar la fila, limpiando los
    /// campos de baja y registrando quién y cuándo lo reactivó.
    /// </summary>
    public async Task<bool> ReactivarAsync(
        int idProducto,
        int? usuario,
        CancellationToken ct = default)
    {
        const string query = @"
            UPDATE producto
            SET Estado = 1,
                UsuarioBaja = NULL,
                FechaBaja = NULL,
                FechaActualizacion = @ahora,
                UsuarioModificacion = @usuario,
                FechaModificacion = @ahora,
                IdEmpleadoResponsable = @usuario
            WHERE IdProducto = @id
              AND Estado = 0";

        DateTime ahora = DateTime.Now;

        using MySqlCommand command = CrearComando(query);

        command.Parameters.AddWithValue("@id", idProducto);
        command.Parameters.AddWithValue("@ahora", ahora);
        command.Parameters.AddWithValue("@usuario", (object?)usuario ?? DBNull.Value);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    // ================== Producto genérico del Factory Method ==================

    /// <summary>
    /// Adaptador de <see cref="IGestionarRepository{T}"/>: "todos" incluye
    /// los productos dados de baja; el listado específico decide si los incluye.
    /// </summary>
    public async Task<List<Producto>> ObtenerTodosAsync() =>
        (await ListarAsync(null, null, incluirInactivos: true, CancellationToken.None)).ToList();

    public async Task<List<Producto>> MostrarActivosAsync() =>
        (await ListarAsync(null, null, incluirInactivos: false, CancellationToken.None)).ToList();

    public Task<int> InsertarAsync(Producto entidad) =>
        InsertarAsync(entidad, CancellationToken.None);

    public Task ActualizarAsync(Producto entidad) =>
        ActualizarAsync(entidad, CancellationToken.None);

    /// <summary>Invierte el estado actual del producto.</summary>
    public async Task CambiarEstadoAsync(int id, int idEmpleadoResponsable)
    {
        Producto? producto = await ObtenerPorIdAsync(id, CancellationToken.None);
        if (producto is null)
        {
            return;
        }

        if (producto.Estado == 1)
        {
            await DarDeBajaAsync(id, idEmpleadoResponsable, CancellationToken.None);
        }
        else
        {
            await ReactivarAsync(id, idEmpleadoResponsable, CancellationToken.None);
        }
    }

    private static Producto MapearProducto(MySqlDataReader reader) => new()
    {
        IdProducto = Convert.ToInt32(reader["IdProducto"]),
        IdCategoria = Convert.ToInt16(reader["IdCategoria"]),
        IdMarca = Convert.ToInt16(reader["IdMarca"]),
        Codigo = Texto(reader, "Codigo"),
        Nombre = Texto(reader, "Nombre"),
        Descripcion = TextoNulo(reader, "Descripcion"),
        UnidadMedida = Texto(reader, "UnidadMedida"),
        PrecioVenta = Convert.ToDecimal(reader["PrecioVenta"]),
        CantidadStock = Convert.ToInt32(reader["CantidadStock"]),
        Estado = Convert.ToByte(reader["Estado"]),
        CategoriaNombre = Texto(reader, "CategoriaNombre"),
        MarcaNombre = Texto(reader, "MarcaNombre"),
        FechaRegistro = Convert.ToDateTime(reader["FechaRegistro"]),
        FechaActualizacion = FechaNula(reader, "FechaActualizacion"),
        UsuarioCreacion = EnteroNulo(reader, "UsuarioCreacion"),
        FechaCreacion = FechaNula(reader, "FechaCreacion"),
        UsuarioModificacion = EnteroNulo(reader, "UsuarioModificacion"),
        FechaModificacion = FechaNula(reader, "FechaModificacion"),
        UsuarioBaja = EnteroNulo(reader, "UsuarioBaja"),
        FechaBaja = FechaNula(reader, "FechaBaja"),
        IdEmpleadoResponsable = EnteroNulo(reader, "IdEmpleadoResponsable")
    };

    private static string Texto(MySqlDataReader reader, string nombre) =>
        reader[nombre] as string ?? string.Empty;

    private static string? TextoNulo(MySqlDataReader reader, string nombre) =>
        reader[nombre] as string;

    private static DateTime? FechaNula(MySqlDataReader reader, string nombre) =>
        reader[nombre] is DateTime fecha ? fecha : null;

    private static int? EnteroNulo(MySqlDataReader reader, string nombre) =>
        reader[nombre] is DBNull ? null : Convert.ToInt32(reader[nombre]);
}
