using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Dominio.Resultados;
using MicroServicioProductos.Infraestructura.Factories;

namespace MicroServicioProductos.Aplicacion.Servicios;

/// <summary>
/// Casos de uso del catálogo de categorías. El repositorio se obtiene mediante
/// el Factory Method inyectado y las reglas de negocio se devuelven como
/// <see cref="Result"/>.
/// </summary>
public sealed class ServicioCategorias : IServicioCategorias
{
    private readonly IRepositoryCategoria _categorias;
    private readonly IContextoUsuario _contextoUsuario;

    public ServicioCategorias(
        RepositoryCreator<IRepositoryCategoria> creadorRepositorio,
        IContextoUsuario contextoUsuario)
    {
        _categorias = creadorRepositorio.CreateRepository();
        _contextoUsuario = contextoUsuario;
    }

    /// <summary>
    /// Código autogenerado que debe mostrarse al dar de alta una categoría.
    /// </summary>
    public async Task<Result<string>> ObtenerSiguienteCodigoCategoriaAsync(
        CancellationToken ct = default)
    {
        string codigo =
            await _categorias.ObtenerSiguienteCodigoCategoriaAsync(ct);

        return Result.Ok(codigo);
    }

    public async Task<Result<IReadOnlyList<Categoria>>> ListarCategoriasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        IReadOnlyList<Categoria> categorias =
            await _categorias.ListarCategoriasAsync(incluirInactivos, ct);

        return Result.Ok(categorias);
    }

    public async Task<Result<Categoria>> ObtenerCategoriaPorIdAsync(
        short idCategoria,
        CancellationToken ct = default)
    {
        Categoria? categoria =
            await _categorias.ObtenerCategoriaPorIdAsync(idCategoria, false, ct);

        return categoria is null
            ? Result.Fallo<Categoria>(
                PrefijosResultado.NoEncontrado + "La categoría no existe o no está activa.")
            : Result.Ok(categoria);
    }

    public async Task<Result<short>> CrearCategoriaAsync(
        CrearCategoriaRequest solicitud,
        CancellationToken ct = default)
    {
        if (solicitud is null)
        {
            return Result.Fallo<short>("Los datos de la categoría son obligatorios.");
        }

        // El código es autogenerado: se asigna cuando viene vacío o cuando ya
        // fue tomado por otra petición (concurrencia) antes de validar.
        string codigo = string.IsNullOrWhiteSpace(solicitud.Codigo)
            ? await GenerarCodigoCategoriaDisponibleAsync(ct)
            : solicitud.Codigo.Trim().ToUpperInvariant();

        if (await _categorias.ExisteCodigoCategoriaAsync(codigo, null, ct))
        {
            codigo = await GenerarCodigoCategoriaDisponibleAsync(ct);
        }

        Result<DatosCategoria> validacion = await ValidarCategoriaAsync(
            codigo,
            solicitud.Nombre,
            solicitud.Descripcion,
            null,
            ct);

        if (validacion.EsFallo)
        {
            return Result.Fallo<short>(validacion.Error);
        }

        DatosCategoria datos = validacion.ValorRequerido;

        // Última comprobación antes de insertar: otra petición pudo tomar el
        // mismo correlativo mientras se ejecutaban las validaciones.
        if (await _categorias.ExisteCodigoCategoriaAsync(datos.Codigo, null, ct))
        {
            string codigoAlterno =
                await GenerarCodigoCategoriaDisponibleAsync(ct);

            if (codigoAlterno == datos.Codigo
                || await _categorias.ExisteCodigoCategoriaAsync(
                    codigoAlterno, null, ct))
            {
                return Result.Fallo<short>(
                    PrefijosResultado.Conflicto +
                    "No se pudo asignar un código único. Inténtalo nuevamente.");
            }

            datos = datos with { Codigo = codigoAlterno };
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        DateTime ahora = DateTime.Now;

        Categoria categoria = new()
        {
            Codigo = datos.Codigo,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            Estado = 1,
            FechaRegistro = ahora,
            FechaCreacion = ahora,
            UsuarioCreacion = idEmpleado,
            IdEmpleadoResponsable = idEmpleado
        };

        short idCategoria =
            await _categorias.InsertarCategoriaAsync(categoria, ct);

        return Result.Ok(idCategoria);
    }

    public async Task<Result> ActualizarCategoriaAsync(
        short idCategoria,
        ActualizarCategoriaRequest solicitud,
        CancellationToken ct = default)
    {
        if (solicitud is null)
        {
            return Result.Fallo("Los datos de la categoría son obligatorios.");
        }

        Categoria? actual =
            await _categorias.ObtenerCategoriaPorIdAsync(idCategoria, false, ct);

        if (actual is null || actual.Estado != 1)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe o no está activa.");
        }

        // El código es inmutable: se valida y se conserva el que ya está en BD,
        // ignora cualquier valor enviado por el cliente.
        Result<DatosCategoria> validacion = await ValidarCategoriaAsync(
            actual.Codigo,
            solicitud.Nombre,
            solicitud.Descripcion,
            idCategoria,
            ct);

        if (validacion.EsFallo)
        {
            return validacion;
        }

        DatosCategoria datos = validacion.ValorRequerido;
        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        DateTime ahora = DateTime.Now;

        Categoria actualizada = new()
        {
            IdCategoria = actual.IdCategoria,
            Codigo = actual.Codigo,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
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

        bool guardado =
            await _categorias.ActualizarCategoriaAsync(actualizada, ct);

        return guardado
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe o no está activa.");
    }

    public async Task<Result> DarDeBajaCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default)
    {
        Categoria? actual =
            await _categorias.ObtenerCategoriaPorIdAsync(idCategoria, false, ct);

        if (actual is null || actual.Estado != 1)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe o no está activa.");
        }

        if (await _categorias.TieneProductosActivosCategoriaAsync(idCategoria, ct))
        {
            return Result.Fallo(
                PrefijosResultado.Conflicto +
                "No se puede dar de baja la categoría porque tiene productos activos asociados.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;

        bool dadoDeBaja = await _categorias.DarDeBajaCategoriaAsync(
            idCategoria, idEmpleado, ct);

        return dadoDeBaja
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe o no está activa.");
    }

    /// <summary>Reactiva una categoría dada de baja.</summary>
    public async Task<Result> ReactivarCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default)
    {
        Categoria? actual =
            await _categorias.ObtenerCategoriaPorIdAsync(
                idCategoria, incluirInactivos: true, ct);

        if (actual is null)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe.");
        }

        if (actual.Estado == 1)
        {
            return Result.Fallo(
                PrefijosResultado.Conflicto + "La categoría ya está activa.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;

        bool reactivada = await _categorias.ReactivarCategoriaAsync(
            idCategoria, idEmpleado, ct);

        return reactivada
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe o ya está activa.");
    }

    // ===================== Auxiliares =====================

    private async Task<string> GenerarCodigoCategoriaDisponibleAsync(
        CancellationToken ct)
    {
        string codigo =
            await _categorias.ObtenerSiguienteCodigoCategoriaAsync(ct);

        // Si otra petición tomó el correlativo entre la consulta y la
        // verificación, se pide uno nuevo (con límite para no bloquear).
        for (int intento = 0;
             intento < 5
             && await _categorias.ExisteCodigoCategoriaAsync(codigo, null, ct);
             intento++)
        {
            codigo =
                await _categorias.ObtenerSiguienteCodigoCategoriaAsync(ct);
        }

        return codigo;
    }

    private async Task<Result<DatosCategoria>> ValidarCategoriaAsync(
        string? codigo,
        string? nombre,
        string? descripcion,
        short? excluirIdCategoria,
        CancellationToken ct)
    {
        List<string> errores = new();

        string codigoNormalizado =
            (codigo ?? string.Empty).Trim().ToUpperInvariant();
        string nombreNormalizado = (nombre ?? string.Empty).Trim();
        string? descripcionNormalizada = string.IsNullOrWhiteSpace(descripcion)
            ? null
            : descripcion.Trim();

        if (codigoNormalizado.Length == 0)
        {
            errores.Add("El código es obligatorio.");
        }
        else if (codigoNormalizado.Length > 20)
        {
            errores.Add("El código no puede superar los 20 caracteres.");
        }

        if (nombreNormalizado.Length == 0)
        {
            errores.Add("El nombre es obligatorio.");
        }
        else if (nombreNormalizado.Length > 100)
        {
            errores.Add("El nombre no puede superar los 100 caracteres.");
        }

        if (descripcionNormalizada is not null
            && descripcionNormalizada.Length > 255)
        {
            errores.Add("La descripción no puede superar los 255 caracteres.");
        }

        if (errores.Count > 0)
        {
            return Result.Fallo<DatosCategoria>(string.Join(" ", errores));
        }

        if (await _categorias.ExisteCodigoCategoriaAsync(
                codigoNormalizado, excluirIdCategoria, ct))
        {
            return Result.Fallo<DatosCategoria>(
                PrefijosResultado.Conflicto +
                $"El código '{codigoNormalizado}' ya está registrado en otra categoría activa.");
        }

        if (await _categorias.ExisteNombreCategoriaAsync(
                nombreNormalizado, excluirIdCategoria, ct))
        {
            return Result.Fallo<DatosCategoria>(
                PrefijosResultado.Conflicto +
                $"El nombre '{nombreNormalizado}' ya está registrado en otra categoría activa.");
        }

        return Result.Ok(new DatosCategoria(
            codigoNormalizado, nombreNormalizado, descripcionNormalizada));
    }

    private sealed record DatosCategoria(
        string Codigo,
        string Nombre,
        string? Descripcion);
}
