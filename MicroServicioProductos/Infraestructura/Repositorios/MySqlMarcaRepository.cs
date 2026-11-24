using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Repositorios;

/// <summary>Repositorio de marcas sobre bdproductos (tabla marca).</summary>
public sealed class MySqlMarcaRepository : IRepositoryMarca
{
    private const string ColumnasMarca = @"
        IdMarca, Nombre, PaisOrigen, Estado,
        FechaRegistro, FechaActualizacion,
        UsuarioCreacion, FechaCreacion,
        UsuarioModificacion, FechaModificacion,
        UsuarioBaja, FechaBaja, IdEmpleadoResponsable";

    private readonly IMySqlUnitOfWork _unidad;

    public MySqlMarcaRepository(IMySqlUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    private MySqlCommand CrearComando(string sql)
    {
        MySqlCommand command = _unidad.Connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _unidad.Transaction;
        return command;
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

        using MySqlCommand command = CrearComando(query);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        List<Marca> marcas = new();

        while (await reader.ReadAsync(ct))
        {
            marcas.Add(MapearMarca(reader));
        }

        return marcas;
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

        using MySqlCommand command = CrearComando(query);

        AgregarParametro(command, "@id", idMarca);

        using MySqlDataReader reader =
            (MySqlDataReader)await command.ExecuteReaderAsync(ct);

        return await reader.ReadAsync(ct) ? MapearMarca(reader) : null;
    }

    public async Task<bool> ExisteMarcaActivaAsync(
        short idMarca,
        CancellationToken ct = default) =>
        await ExisteActivoAsync("marca", "IdMarca", idMarca, ct);

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

        using MySqlCommand command = CrearComando(query);

        AgregarParametro(command, "@nombre", nombre);

        if (excluirIdMarca.HasValue)
        {
            AgregarParametro(command, "@excluir", excluirIdMarca.Value);
        }

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<bool> TieneProductosActivosMarcaAsync(
        short idMarca,
        CancellationToken ct = default) =>
        await TieneProductosActivosAsync("IdMarca", idMarca, ct);

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

        using MySqlCommand command = CrearComando(query);

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

        using MySqlCommand command = CrearComando(query);

        AgregarParametro(command, "@id", marca.IdMarca);
        AgregarParametro(command, "@nombre", marca.Nombre);
        AgregarParametro(command, "@paisOrigen", marca.PaisOrigen);
        AgregarParametro(command, "@estado", marca.Estado);
        AgregarParametro(command, "@fechaActualizacion", marca.FechaActualizacion);
        AgregarParametro(command, "@usuarioModificacion", marca.UsuarioModificacion);
        AgregarParametro(command, "@fechaModificacion", marca.FechaModificacion);
        AgregarParametro(
            command, "@idEmpleadoResponsable", marca.IdEmpleadoResponsable);

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

        using MySqlCommand command = CrearComando(query);

        AgregarParametro(command, "@id", idMarca);
        AgregarParametro(command, "@ahora", DateTime.Now);
        AgregarParametro(command, "@usuario", idEmpleadoResponsable);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    /// <summary>
    /// Reactiva una marca dada de baja sin borrar la fila y limpiando
    /// los campos de baja, igual que el patrÃ³n de Proveedores.
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

        using MySqlCommand command = CrearComando(query);

        AgregarParametro(command, "@id", idMarca);
        AgregarParametro(command, "@ahora", DateTime.Now);
        AgregarParametro(command, "@usuario", idEmpleadoResponsable);

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    // ================== Producto genérico del Factory Method ==================

    public async Task<List<Marca>> ObtenerTodosAsync() =>
        (await ListarMarcasAsync(
            incluirInactivos: true, CancellationToken.None)).ToList();

    public async Task<List<Marca>> MostrarActivosAsync() =>
        (await ListarMarcasAsync(
            incluirInactivos: false, CancellationToken.None)).ToList();

    public async Task<int> InsertarAsync(Marca entidad) =>
        await InsertarMarcaAsync(entidad, CancellationToken.None);

    public async Task ActualizarAsync(Marca entidad) =>
        await ActualizarMarcaAsync(entidad, CancellationToken.None);

    /// <summary>Invierte el estado actual; el id de empleado se usa como auditoría.</summary>
    public async Task CambiarEstadoAsync(int id, int idEmpleadoResponsable)
    {
        if (id is < 1 or > short.MaxValue)
        {
            return;
        }

        short idMarca = (short)id;
        Marca? marca = await ObtenerMarcaPorIdAsync(
            idMarca, incluirInactivos: true, CancellationToken.None);

        if (marca is null)
        {
            return;
        }

        if (marca.Estado == 1)
        {
            await DarDeBajaMarcaAsync(
                idMarca, idEmpleadoResponsable, CancellationToken.None);
        }
        else
        {
            await ReactivarMarcaAsync(
                idMarca, idEmpleadoResponsable, CancellationToken.None);
        }
    }

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

        using MySqlCommand command = CrearComando(query);

        AgregarParametro(command, "@id", id);

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

        using MySqlCommand command = CrearComando(query);

        AgregarParametro(command, "@id", id);

        return await command.ExecuteScalarAsync(ct) is not null;
    }

    private static void AgregarParametro(
        MySqlCommand command,
        string nombre,
        object? valor) =>
        command.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);

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
