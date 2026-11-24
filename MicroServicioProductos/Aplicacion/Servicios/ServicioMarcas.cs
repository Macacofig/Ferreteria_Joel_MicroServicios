using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Dominio.Resultados;
using MicroServicioProductos.Infraestructura.Factories;

namespace MicroServicioProductos.Aplicacion.Servicios;

/// <summary>
/// Casos de uso del catálogo de marcas. El repositorio se obtiene mediante el
/// Factory Method inyectado y las reglas de negocio se devuelven como
/// <see cref="Result"/>.
/// </summary>
public sealed class ServicioMarcas : IServicioMarcas
{
    private readonly IRepositoryMarca _marcas;
    private readonly IContextoUsuario _contextoUsuario;

    public ServicioMarcas(
        RepositoryCreator<IRepositoryMarca> creadorRepositorio,
        IContextoUsuario contextoUsuario)
    {
        _marcas = creadorRepositorio.CreateRepository();
        _contextoUsuario = contextoUsuario;
    }

    public async Task<Result<IReadOnlyList<Marca>>> ListarMarcasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        IReadOnlyList<Marca> marcas =
            await _marcas.ListarMarcasAsync(incluirInactivos, ct);

        return Result.Ok(marcas);
    }

    public async Task<Result<Marca>> ObtenerMarcaPorIdAsync(
        short idMarca,
        CancellationToken ct = default)
    {
        Marca? marca = await _marcas.ObtenerMarcaPorIdAsync(idMarca, false, ct);

        return marca is null
            ? Result.Fallo<Marca>(
                PrefijosResultado.NoEncontrado + "La marca no existe o no está activa.")
            : Result.Ok(marca);
    }

    public async Task<Result<short>> CrearMarcaAsync(
        CrearMarcaRequest solicitud,
        CancellationToken ct = default)
    {
        if (solicitud is null)
        {
            return Result.Fallo<short>("Los datos de la marca son obligatorios.");
        }

        Result<DatosMarca> validacion = await ValidarMarcaAsync(
            solicitud.Nombre,
            solicitud.PaisOrigen,
            null,
            ct);

        if (validacion.EsFallo)
        {
            return Result.Fallo<short>(validacion.Error);
        }

        DatosMarca datos = validacion.ValorRequerido;
        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        DateTime ahora = DateTime.Now;

        Marca marca = new()
        {
            Nombre = datos.Nombre,
            PaisOrigen = datos.PaisOrigen,
            Estado = 1,
            FechaRegistro = ahora,
            FechaCreacion = ahora,
            UsuarioCreacion = idEmpleado,
            IdEmpleadoResponsable = idEmpleado
        };

        short idMarca = await _marcas.InsertarMarcaAsync(marca, ct);

        return Result.Ok(idMarca);
    }

    public async Task<Result> ActualizarMarcaAsync(
        short idMarca,
        ActualizarMarcaRequest solicitud,
        CancellationToken ct = default)
    {
        if (solicitud is null)
        {
            return Result.Fallo("Los datos de la marca son obligatorios.");
        }

        Marca? actual = await _marcas.ObtenerMarcaPorIdAsync(idMarca, false, ct);

        if (actual is null || actual.Estado != 1)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "La marca no existe o no está activa.");
        }

        Result<DatosMarca> validacion = await ValidarMarcaAsync(
            solicitud.Nombre,
            solicitud.PaisOrigen,
            idMarca,
            ct);

        if (validacion.EsFallo)
        {
            return validacion;
        }

        DatosMarca datos = validacion.ValorRequerido;
        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        DateTime ahora = DateTime.Now;

        Marca actualizada = new()
        {
            IdMarca = actual.IdMarca,
            Nombre = datos.Nombre,
            PaisOrigen = datos.PaisOrigen,
            Estado = actual.Estado,
            FechaRegistro = actual.FechaRegistro,
            FechaActualizacion = ahora,
            UsuarioCreacion = actual.UsuarioCreacion,
            FechaCreacion = actual.FechaCreacion,
            UsuarioModificacion = idEmpleado,
            FechaModificacion = ahora,
            UsuarioBaja = actual.UsuarioBaja,
            FechaBaja = actual.FechaBaja,
            IdEmpleadoResponsable = idEmpleado
        };

        bool guardado = await _marcas.ActualizarMarcaAsync(actualizada, ct);

        return guardado
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La marca no existe o no está activa.");
    }

    public async Task<Result> DarDeBajaMarcaAsync(
        short idMarca,
        CancellationToken ct = default)
    {
        Marca? actual = await _marcas.ObtenerMarcaPorIdAsync(idMarca, false, ct);

        if (actual is null || actual.Estado != 1)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "La marca no existe o no está activa.");
        }

        if (await _marcas.TieneProductosActivosMarcaAsync(idMarca, ct))
        {
            return Result.Fallo(
                PrefijosResultado.Conflicto +
                "No se puede dar de baja la marca porque tiene productos activos asociados.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;

        bool dadoDeBaja =
            await _marcas.DarDeBajaMarcaAsync(idMarca, idEmpleado, ct);

        return dadoDeBaja
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La marca no existe o no está activa.");
    }

    /// <summary>Reactiva una marca dada de baja.</summary>
    public async Task<Result> ReactivarMarcaAsync(
        short idMarca,
        CancellationToken ct = default)
    {
        Marca? actual = await _marcas.ObtenerMarcaPorIdAsync(
            idMarca, incluirInactivos: true, ct);

        if (actual is null)
        {
            return Result.Fallo(PrefijosResultado.NoEncontrado + "La marca no existe.");
        }

        if (actual.Estado == 1)
        {
            return Result.Fallo(PrefijosResultado.Conflicto + "La marca ya está activa.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;

        bool reactivada = await _marcas.ReactivarMarcaAsync(
            idMarca, idEmpleado, ct);

        return reactivada
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La marca no existe o ya está activa.");
    }

    // ===================== Auxiliares =====================

    private async Task<Result<DatosMarca>> ValidarMarcaAsync(
        string? nombre,
        string? paisOrigen,
        short? excluirIdMarca,
        CancellationToken ct)
    {
        List<string> errores = new();

        string nombreNormalizado = (nombre ?? string.Empty).Trim();
        string? paisNormalizado = string.IsNullOrWhiteSpace(paisOrigen)
            ? null
            : paisOrigen.Trim();

        if (nombreNormalizado.Length == 0)
        {
            errores.Add("El nombre es obligatorio.");
        }
        else if (nombreNormalizado.Length > 100)
        {
            errores.Add("El nombre no puede superar los 100 caracteres.");
        }

        if (paisNormalizado is not null && paisNormalizado.Length > 80)
        {
            errores.Add("El país de origen no puede superar los 80 caracteres.");
        }

        if (errores.Count > 0)
        {
            return Result.Fallo<DatosMarca>(string.Join(" ", errores));
        }

        if (await _marcas.ExisteNombreMarcaAsync(
                nombreNormalizado, excluirIdMarca, ct))
        {
            return Result.Fallo<DatosMarca>(
                PrefijosResultado.Conflicto +
                $"El nombre '{nombreNormalizado}' ya está registrado en otra marca activa.");
        }

        return Result.Ok(new DatosMarca(nombreNormalizado, paisNormalizado));
    }

    private sealed record DatosMarca(
        string Nombre,
        string? PaisOrigen);
}
