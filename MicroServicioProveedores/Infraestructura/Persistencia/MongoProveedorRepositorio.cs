using MicroServicioProveedores.Dominio.Entidades;
using MicroServicioProveedores.Dominio.Puertos;
using MicroServicioProveedores.Dominio.Resultados;
using MongoDB.Driver;

namespace MicroServicioProveedores.Infraestructura.Persistencia;

public class MongoProveedorRepositorio : IRepositorioProveedores
{
    private const string ColeccionProveedores = "proveedores";
    private const string ColeccionContadores = "contadores";
    private const string IdContadorProveedores = "proveedores";

    private readonly IMongoCollection<ProveedorDocumento> _proveedores;
    private readonly IMongoCollection<ContadorDocumento> _contadores;

    public MongoProveedorRepositorio(IMongoDatabase database)
    {
        _proveedores = database.GetCollection<ProveedorDocumento>(ColeccionProveedores);
        _contadores = database.GetCollection<ContadorDocumento>(ColeccionContadores);
    }

    public async Task<IReadOnlyList<Proveedor>> ListarAsync(CancellationToken ct = default)
    {
        var documentos = await _proveedores.Find(FilterDefinition<ProveedorDocumento>.Empty).ToListAsync(ct);
        return documentos.Select(MapearADominio).ToList();
    }

    public async Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var documento = await _proveedores.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
        return documento is null ? null : MapearADominio(documento);
    }

    public async Task<Proveedor?> ObtenerPorNitNormalizadoAsync(string nitNormalizado, CancellationToken ct = default)
    {
        var documento = await _proveedores.Find(p => p.Nit == nitNormalizado).FirstOrDefaultAsync(ct);
        return documento is null ? null : MapearADominio(documento);
    }

    public async Task<int> GenerarNuevoIdAsync(CancellationToken ct = default)
    {
        var filtro = Builders<ContadorDocumento>.Filter.Eq(c => c.Id, IdContadorProveedores);
        var actualizacion = Builders<ContadorDocumento>.Update.Inc(c => c.Secuencia, 1);
        var opciones = new FindOneAndUpdateOptions<ContadorDocumento>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var contador = await _contadores.FindOneAndUpdateAsync(filtro, actualizacion, opciones, ct);
        return contador.Secuencia;
    }

    public async Task<Result> CrearAsync(Proveedor proveedor, CancellationToken ct = default)
    {
        try
        {
            await _proveedores.InsertOneAsync(MapearADocumento(proveedor), cancellationToken: ct);
            return Result.Ok();
        }
        // Duplicados son errores esperados de negocio: se convierten en Result.
        // El resto de excepciones Mongo (conexión, configuración) siguen propagándose.
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result.Fallo(TraducirDuplicado(ex));
        }
    }

    public async Task<Result> ActualizarAsync(Proveedor proveedor, CancellationToken ct = default)
    {
        try
        {
            var resultado = await _proveedores.ReplaceOneAsync(
                p => p.Id == proveedor.Id,
                MapearADocumento(proveedor),
                cancellationToken: ct);

            return resultado.MatchedCount > 0
                ? Result.Ok()
                : Result.Fallo(ErrorResultado.NoEncontrado("Proveedor no encontrado."));
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result.Fallo(TraducirDuplicado(ex));
        }
    }

    // ================== Baja lógica ==================

    /// <summary>Marca el proveedor como inactivo sin borrar el documento.</summary>
    public async Task<bool> DarDeBajaAsync(int id, string usuario, CancellationToken ct = default)
    {
        DateTime ahora = DateTime.Now;
        var filtro = Builders<ProveedorDocumento>.Filter.And(
            Builders<ProveedorDocumento>.Filter.Eq(p => p.Id, id),
            Builders<ProveedorDocumento>.Filter.Eq(p => p.Estado, true));

        var actualizacion = Builders<ProveedorDocumento>.Update
            .Set(p => p.Estado, false)
            .Set(p => p.FechaBaja, ahora)
            .Set(p => p.UsuarioBaja, usuario)
            .Set(p => p.FechaModificacion, ahora)
            .Set(p => p.UsuarioModificacion, usuario);

        var resultado = await _proveedores.UpdateOneAsync(filtro, actualizacion, cancellationToken: ct);
        return resultado.ModifiedCount > 0;
    }

    /// <summary>Reactiva un proveedor dado de baja y limpia los campos de baja.</summary>
    public async Task<bool> ReactivarAsync(int id, string usuario, CancellationToken ct = default)
    {
        DateTime ahora = DateTime.Now;
        var filtro = Builders<ProveedorDocumento>.Filter.And(
            Builders<ProveedorDocumento>.Filter.Eq(p => p.Id, id),
            Builders<ProveedorDocumento>.Filter.Eq(p => p.Estado, false));

        var actualizacion = Builders<ProveedorDocumento>.Update
            .Set(p => p.Estado, true)
            .Set(p => p.FechaBaja, null)
            .Set(p => p.UsuarioBaja, null)
            .Set(p => p.FechaModificacion, ahora)
            .Set(p => p.UsuarioModificacion, usuario);

        var resultado = await _proveedores.UpdateOneAsync(filtro, actualizacion, cancellationToken: ct);
        return resultado.ModifiedCount > 0;
    }

    // ================== Producto genérico del Factory Method ==================

    /// <summary>
    /// Adaptador de <see cref="IGestionarRepository{T}"/>: delega en el listado
    /// del puerto específico y materializa el resultado en <see cref="List{T}"/>.
    /// </summary>
    public async Task<List<Proveedor>> ObtenerTodosAsync() =>
        (await ListarAsync(CancellationToken.None)).ToList();

    /// <summary>
    /// El identificador lo asigna el caso de uso con GenerarNuevoIdAsync antes
    /// de crear; por eso se devuelve el que ya trae la entidad. Ante un fallo
    /// de escritura se reporta 0 para no inventar un identificador válido.
    /// </summary>
    public async Task<int> InsertarAsync(Proveedor entidad)
    {
        Result resultado = await CrearAsync(entidad, CancellationToken.None);
        return resultado.EsFallo ? 0 : entidad.Id;
    }

    public Task ActualizarAsync(Proveedor entidad) =>
        ActualizarAsync(entidad, CancellationToken.None);

    /// <summary>Invierte el estado actual; el id de empleado se usa como auditoría.</summary>
    public async Task CambiarEstadoAsync(int id, int idEmpleadoResponsable)
    {
        Proveedor? proveedor = await ObtenerPorIdAsync(id, CancellationToken.None);
        if (proveedor is null)
        {
            return;
        }

        string usuario = idEmpleadoResponsable.ToString();

        if (proveedor.Estado)
        {
            await DarDeBajaAsync(id, usuario, CancellationToken.None);
        }
        else
        {
            await ReactivarAsync(id, usuario, CancellationToken.None);
        }
    }

    public async Task<List<Proveedor>> MostrarActivosAsync()
    {
        var documentos = await _proveedores
            .Find(p => p.Estado)
            .ToListAsync(CancellationToken.None);

        return documentos.Select(MapearADominio).ToList();
    }

    private static ErrorResultado TraducirDuplicado(MongoWriteException ex)
    {
        string mensaje = ex.WriteError?.Message ?? string.Empty;
        if (mensaje.Contains("nit", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorResultado.Conflicto(
                "Ya existe un proveedor registrado con el mismo NIT.",
                new Dictionary<string, string>
                {
                    ["Nit"] = "El NIT ingresado ya está registrado para otro proveedor."
                });
        }

        if (mensaje.Contains("_id", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorResultado.Conflicto(
                "Ya existe un proveedor con el mismo identificador.",
                new Dictionary<string, string> { ["Id"] = "El identificador ya está registrado." });
        }

        return ErrorResultado.Conflicto(
            "El proveedor entra en conflicto con un registro existente.");
    }

    private static ProveedorDocumento MapearADocumento(Proveedor proveedor) => new()
    {
        Id = proveedor.Id,
        RazonSocial = proveedor.RazonSocial,
        Nit = proveedor.Nit,
        Contacto = proveedor.Contacto,
        Telefono = proveedor.Telefono,
        NombreResponsable = proveedor.NombreResponsable,
        Estado = proveedor.Estado,
        FechaCreacion = proveedor.FechaCreacion,
        UsuarioCreacion = proveedor.UsuarioCreacion,
        FechaModificacion = proveedor.FechaModificacion,
        UsuarioModificacion = proveedor.UsuarioModificacion,
        FechaBaja = proveedor.FechaBaja,
        UsuarioBaja = proveedor.UsuarioBaja
    };

    private static Proveedor MapearADominio(ProveedorDocumento d)
        => new(
            d.Id,
            d.RazonSocial,
            d.Nit,
            d.Contacto,
            d.Telefono,
            d.NombreResponsable ?? string.Empty,
            d.Estado,
            d.FechaCreacion,
            d.UsuarioCreacion ?? string.Empty,
            d.FechaModificacion,
            d.UsuarioModificacion,
            d.FechaBaja,
            d.UsuarioBaja);
}
