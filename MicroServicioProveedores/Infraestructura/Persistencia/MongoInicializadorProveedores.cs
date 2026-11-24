using MongoDB.Driver;

namespace MicroServicioProveedores.Infraestructura.Persistencia;

public class MongoInicializadorProveedores
{
    private const string ColeccionProveedores = "proveedores";
    private const string ColeccionContadores = "contadores";
    private const string IdContadorProveedores = "proveedores";

    private readonly IMongoDatabase _database;

    public MongoInicializadorProveedores(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task InicializarAsync(CancellationToken ct = default)
    {
        var proveedores = _database.GetCollection<ProveedorDocumento>(ColeccionProveedores);
        var contadores = _database.GetCollection<ContadorDocumento>(ColeccionContadores);

        // Índice único para NIT normalizado: evita duplicados a nivel de persistencia.
        var indiceNit = new CreateIndexModel<ProveedorDocumento>(
            Builders<ProveedorDocumento>.IndexKeys.Ascending(p => p.Nit),
            new CreateIndexOptions { Unique = true, Name = "ux_proveedores_nit" });

        try
        {
            await proveedores.Indexes.CreateOneAsync(indiceNit, cancellationToken: ct);
        }
        catch (MongoCommandException)
        {
            // Si existen datos previos no normalizados/duplicados, el índice puede fallar;
            // se propaga para que el despliegue detecte la inconsistencia.
            throw;
        }

        // Backfill de auditoría para documentos creados antes de estos campos.
        DateTime ahoraBackfill = DateTime.Now;
        await proveedores.UpdateManyAsync(
            Builders<ProveedorDocumento>.Filter.Exists(p => p.Estado, false),
            Builders<ProveedorDocumento>.Update.Set(p => p.Estado, true),
            cancellationToken: ct);
        await proveedores.UpdateManyAsync(
            Builders<ProveedorDocumento>.Filter.Exists(p => p.FechaCreacion, false),
            Builders<ProveedorDocumento>.Update.Set(p => p.FechaCreacion, ahoraBackfill),
            cancellationToken: ct);
        await proveedores.UpdateManyAsync(
            Builders<ProveedorDocumento>.Filter.Exists(p => p.UsuarioCreacion, false),
            Builders<ProveedorDocumento>.Update.Set(p => p.UsuarioCreacion, "jrios"),
            cancellationToken: ct);

        // Sincroniza el contador con el máximo Id existente para garantizar generación sin colisiones.
        var ultimo = await proveedores.Find(FilterDefinition<ProveedorDocumento>.Empty)
            .SortByDescending(p => p.Id)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

        int maxId = ultimo?.Id ?? 0;

        var filtro = Builders<ContadorDocumento>.Filter.Eq(c => c.Id, IdContadorProveedores);
        var contadorActual = await contadores.Find(filtro).FirstOrDefaultAsync(ct);
        if (contadorActual is null)
        {
            if (maxId == 0)
            {
                await SembrarDatosInicialesAsync(proveedores, contadores, ct);
                return;
            }

            await contadores.InsertOneAsync(new ContadorDocumento { Id = IdContadorProveedores, Secuencia = maxId }, cancellationToken: ct);
            return;
        }

        if (contadorActual.Secuencia < maxId)
        {
            var update = Builders<ContadorDocumento>.Update.Set(c => c.Secuencia, maxId);
            await contadores.UpdateOneAsync(filtro, update, cancellationToken: ct);
        }
    }

    private static async Task SembrarDatosInicialesAsync(
        IMongoCollection<ProveedorDocumento> proveedores,
        IMongoCollection<ContadorDocumento> contadores,
        CancellationToken ct)
    {
        // Datos de ejemplo para facilitar la verificación de listado/ordenación con varios registros.
        DateTime ahora = DateTime.Now;
        var semilla = new[]
        {
            new ProveedorDocumento { Id = 1, RazonSocial = "Ferreteria Central SA", Nit = "100200300", Contacto = "Jorge Vargas", Telefono = "70011111", NombreResponsable = "Joel Ríos", Estado = true, FechaCreacion = ahora, UsuarioCreacion = "jrios" },
            new ProveedorDocumento { Id = 2, RazonSocial = "Importadora Andina Ltda", Nit = "200300400", Contacto = "Lucia Paredes", Telefono = "70022222", NombreResponsable = "Joel Ríos", Estado = true, FechaCreacion = ahora, UsuarioCreacion = "jrios" },
            new ProveedorDocumento { Id = 3, RazonSocial = "Distribuidora El Tornillo", Nit = "300400500", Contacto = "Pedro Chavez", Telefono = "70033333", NombreResponsable = "Joel Ríos", Estado = true, FechaCreacion = ahora, UsuarioCreacion = "jrios" }
        };

        await proveedores.InsertManyAsync(semilla, cancellationToken: ct);
        await contadores.InsertOneAsync(new ContadorDocumento { Id = IdContadorProveedores, Secuencia = semilla.Max(s => s.Id) }, cancellationToken: ct);
    }
}
