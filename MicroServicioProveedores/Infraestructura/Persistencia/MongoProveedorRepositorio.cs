using MicroServicioProveedores.Aplicacion.Contratos;
using MicroServicioProveedores.Dominio.Entidades;
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

    public async Task CrearAsync(Proveedor proveedor, CancellationToken ct = default)
    {
        try
        {
            await _proveedores.InsertOneAsync(MapearADocumento(proveedor), cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw TraducirDuplicado(ex);
        }
    }

    public async Task<bool> ActualizarAsync(Proveedor proveedor, CancellationToken ct = default)
    {
        try
        {
            var resultado = await _proveedores.ReplaceOneAsync(
                p => p.Id == proveedor.Id,
                MapearADocumento(proveedor),
                cancellationToken: ct);
            return resultado.MatchedCount > 0;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw TraducirDuplicado(ex);
        }
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken ct = default)
    {
        var resultado = await _proveedores.DeleteOneAsync(p => p.Id == id, ct);
        return resultado.DeletedCount > 0;
    }

    private static ConflictoNegocioExcepcion TraducirDuplicado(MongoWriteException ex)
    {
        string mensaje = ex.WriteError?.Message ?? string.Empty;
        if (mensaje.Contains("nit", StringComparison.OrdinalIgnoreCase))
        {
            return new ConflictoNegocioExcepcion("Ya existe un proveedor registrado con el mismo NIT.", "Nit");
        }

        if (mensaje.Contains("_id", StringComparison.OrdinalIgnoreCase))
        {
            return new ConflictoNegocioExcepcion("Ya existe un proveedor con el mismo identificador.", "Id");
        }

        return new ConflictoNegocioExcepcion("El proveedor entra en conflicto con un registro existente.");
    }

    private static ProveedorDocumento MapearADocumento(Proveedor proveedor) => new()
    {
        Id = proveedor.Id,
        RazonSocial = proveedor.RazonSocial,
        Nit = proveedor.Nit,
        Contacto = proveedor.Contacto,
        Telefono = proveedor.Telefono
    };

    private static Proveedor MapearADominio(ProveedorDocumento d)
        => new(d.Id, d.RazonSocial, d.Nit, d.Contacto, d.Telefono);
}
