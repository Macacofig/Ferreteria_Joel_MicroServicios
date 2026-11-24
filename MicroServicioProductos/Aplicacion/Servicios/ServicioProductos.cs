using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Dominio.Resultados;

namespace MicroServicioProductos.Aplicacion.Servicios;

/// <summary>
/// Casos de uso del catálogo de productos. Las validaciones de negocio se
/// devuelven como <see cref="Result"/>; no se lanzan excepciones para errores
/// esperados. Los prefijos permiten al adaptador HTTP distinguir 404 y 409
/// del resto de validaciones (400) sin inspeccionar excepciones.
/// </summary>
public sealed class ServicioProductos : IServicioProductos
{
    private readonly IRepositoryProducto _productos;
    private readonly IRepositoryCatalogoAuxiliar _catalogo;
    private readonly IRepositoryHistoricoProducto _historico;
    private readonly IContextoUsuario _contextoUsuario;

    public ServicioProductos(
        IRepositoryProducto productos,
        IRepositoryCatalogoAuxiliar catalogo,
        IRepositoryHistoricoProducto historico,
        IContextoUsuario contextoUsuario)
    {
        _productos = productos;
        _catalogo = catalogo;
        _historico = historico;
        _contextoUsuario = contextoUsuario;
    }

    // ===================== Consultas =====================

    public async Task<Result<IReadOnlyList<Producto>>> ListarAsync(
        string? ordenarPor,
        string? direccion,
        bool incluirInactivos,
        CancellationToken ct = default)
    {
        IReadOnlyList<Producto> productos = await _productos.ListarAsync(
            ordenarPor, direccion, incluirInactivos, ct);

        return Result.Ok(productos);
    }

    public async Task<Result<Producto>> ObtenerPorIdAsync(
        int idProducto,
        CancellationToken ct = default)
    {
        Producto? producto = await _productos.ObtenerPorIdAsync(idProducto, ct);

        return producto is null
            ? Result.Fallo<Producto>(
                PrefijosResultado.NoEncontrado + "El producto no existe.")
            : Result.Ok(producto);
    }

    /// <summary>
    /// Código autogenerado que debe mostrarse al dar de alta un producto.
    /// </summary>
    public async Task<Result<string>> ObtenerSiguienteCodigoAsync(
        CancellationToken ct = default)
    {
        string codigo = await _productos.ObtenerSiguienteCodigoAsync(ct);

        return Result.Ok(codigo);
    }

    /// <summary>
    /// Código autogenerado que debe mostrarse al dar de alta una categoría.
    /// </summary>
    public async Task<Result<string>> ObtenerSiguienteCodigoCategoriaAsync(
        CancellationToken ct = default)
    {
        string codigo =
            await _catalogo.ObtenerSiguienteCodigoCategoriaAsync(ct);

        return Result.Ok(codigo);
    }

    public async Task<Result<IReadOnlyList<Categoria>>> ListarCategoriasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        IReadOnlyList<Categoria> categorias =
            await _catalogo.ListarCategoriasAsync(incluirInactivos, ct);

        return Result.Ok(categorias);
    }

    public async Task<Result<IReadOnlyList<Marca>>> ListarMarcasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        IReadOnlyList<Marca> marcas =
            await _catalogo.ListarMarcasAsync(incluirInactivos, ct);

        return Result.Ok(marcas);
    }

    public async Task<Result<IReadOnlyList<HistoricoProducto>>> ListarHistoricoAsync(
        int idProducto,
        CancellationToken ct = default)
    {
        Producto? producto = await _productos.ObtenerPorIdAsync(idProducto, ct);

        if (producto is null)
        {
            return Result.Fallo<IReadOnlyList<HistoricoProducto>>(
                PrefijosResultado.NoEncontrado + "El producto no existe.");
        }

        IReadOnlyList<HistoricoProducto> historial =
            await _historico.ListarPorProductoAsync(idProducto, ct);

        return Result.Ok(historial);
    }

    // ===================== Altas, cambios y bajas =====================

    public async Task<Result<int>> CrearAsync(
        CrearProductoRequest solicitud,
        CancellationToken ct = default)
    {
        if (solicitud is null)
        {
            return Result.Fallo<int>("Los datos del producto son obligatorios.");
        }

        // El código es autogenerado: se asigna cuando viene vacío o cuando ya
        // fue tomado por otra petición (concurrencia) antes de validar.
        string codigo = string.IsNullOrWhiteSpace(solicitud.Codigo)
            ? await GenerarCodigoDisponibleAsync(ct)
            : solicitud.Codigo.Trim().ToUpperInvariant();

        if (await _productos.ExisteCodigoAsync(codigo, null, ct))
        {
            codigo = await GenerarCodigoDisponibleAsync(ct);
        }

        Result<DatosValidados> validacion = await ValidarAsync(
            new DatosValidados(
                solicitud.IdCategoria,
                solicitud.IdMarca,
                codigo,
                solicitud.Nombre ?? string.Empty,
                solicitud.Descripcion,
                solicitud.UnidadMedida ?? string.Empty,
                solicitud.PrecioVenta,
                solicitud.CantidadStock),
            null,
            ct);

        if (validacion.EsFallo)
        {
            return Result.Fallo<int>(validacion.Error);
        }

        DatosValidados datos = validacion.ValorRequerido;

        // Última comprobación antes de insertar: otra petición pudo tomar el
        // mismo correlativo mientras se ejecutaban las validaciones.
        if (await _productos.ExisteCodigoAsync(datos.Codigo, null, ct))
        {
            string codigoAlterno = await GenerarCodigoDisponibleAsync(ct);

            if (codigoAlterno == datos.Codigo
                || await _productos.ExisteCodigoAsync(codigoAlterno, null, ct))
            {
                return Result.Fallo<int>(
                    PrefijosResultado.Conflicto +
                    "No se pudo asignar un código único. Inténtalo nuevamente.");
            }

            datos = datos with { Codigo = codigoAlterno };
        }
        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        string usuario = _contextoUsuario.NombreUsuarioActual;
        DateTime ahora = DateTime.Now;

        Producto producto = new()
        {
            IdCategoria = datos.IdCategoria,
            IdMarca = datos.IdMarca,
            Codigo = datos.Codigo,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            UnidadMedida = datos.UnidadMedida,
            PrecioVenta = datos.PrecioVenta,
            CantidadStock = datos.CantidadStock,
            Estado = 1,
            FechaRegistro = ahora,
            FechaCreacion = ahora,
            UsuarioCreacion = idEmpleado,
            IdEmpleadoResponsable = idEmpleado
        };

        int idProducto = await _productos.InsertarAsync(producto, ct);

        await _historico.RegistrarAsync(new HistoricoProducto
        {
            IdProducto = idProducto,
            Accion = "INSERT",
            PrecioNuevo = producto.PrecioVenta,
            StockNuevo = producto.CantidadStock,
            Detalle = $"Alta de producto realizada por {usuario}.",
            FechaCambio = ahora,
            IdEmpleadoResponsable = idEmpleado
        }, ct);

        return Result.Ok(idProducto);
    }

    public async Task<Result> ActualizarAsync(
        int idProducto,
        ActualizarProductoRequest solicitud,
        CancellationToken ct = default)
    {
        if (solicitud is null)
        {
            return Result.Fallo("Los datos del producto son obligatorios.");
        }

        Producto? actual = await _productos.ObtenerPorIdAsync(idProducto, ct);

        if (actual is null || actual.Estado != 1)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "El producto no existe o no está activo.");
        }

        Result<DatosValidados> validacion = await ValidarAsync(
            new DatosValidados(
                solicitud.IdCategoria,
                solicitud.IdMarca,
                solicitud.Codigo ?? string.Empty,
                solicitud.Nombre ?? string.Empty,
                solicitud.Descripcion,
                solicitud.UnidadMedida ?? string.Empty,
                solicitud.PrecioVenta,
                solicitud.CantidadStock),
            idProducto,
            ct);

        if (validacion.EsFallo)
        {
            return validacion;
        }

        DatosValidados datos = validacion.ValorRequerido;
        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        string usuario = _contextoUsuario.NombreUsuarioActual;
        DateTime ahora = DateTime.Now;

        Producto actualizado = new()
        {
            IdProducto = actual.IdProducto,
            IdCategoria = datos.IdCategoria,
            IdMarca = datos.IdMarca,
            Codigo = datos.Codigo,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            UnidadMedida = datos.UnidadMedida,
            PrecioVenta = datos.PrecioVenta,
            CantidadStock = datos.CantidadStock,
            Estado = actual.Estado,
            FechaRegistro = actual.FechaRegistro,
            FechaActualizacion = ahora,
            UsuarioCreacion = actual.UsuarioCreacion,
            FechaCreacion = actual.FechaCreacion,
            UsuarioModificacion = idEmpleado,
            FechaModificacion = ahora,
            IdEmpleadoResponsable = idEmpleado
        };

        bool guardado = await _productos.ActualizarAsync(actualizado, ct);

        if (!guardado)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "El producto no existe o no está activo.");
        }

        await _historico.RegistrarAsync(new HistoricoProducto
        {
            IdProducto = idProducto,
            Accion = "UPDATE",
            PrecioAnterior = actual.PrecioVenta,
            PrecioNuevo = actualizado.PrecioVenta,
            StockAnterior = actual.CantidadStock,
            StockNuevo = actualizado.CantidadStock,
            Detalle = $"Actualización de producto realizada por {usuario}.",
            FechaCambio = ahora,
            IdEmpleadoResponsable = idEmpleado
        }, ct);

        return Result.Ok();
    }

    public async Task<Result> DarDeBajaAsync(
        int idProducto,
        CancellationToken ct = default)
    {
        Producto? actual = await _productos.ObtenerPorIdAsync(idProducto, ct);

        if (actual is null || actual.Estado != 1)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "El producto no existe o no está activo.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        string usuario = _contextoUsuario.NombreUsuarioActual;
        DateTime ahora = DateTime.Now;

        bool dadoDeBaja = await _productos.DarDeBajaAsync(
            idProducto, idEmpleado, ct);

        if (!dadoDeBaja)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "El producto no existe o no está activo.");
        }

        await _historico.RegistrarAsync(new HistoricoProducto
        {
            IdProducto = idProducto,
            Accion = "DELETE",
            PrecioAnterior = actual.PrecioVenta,
            StockAnterior = actual.CantidadStock,
            Detalle = $"Baja lógica de producto realizada por {usuario}.",
            FechaCambio = ahora,
            IdEmpleadoResponsable = idEmpleado
        }, ct);

        return Result.Ok();
    }

    /// <summary>Reactiva un producto dado de baja, conservando su historial.</summary>
    public async Task<Result> ReactivarAsync(
        int idProducto,
        CancellationToken ct = default)
    {
        Producto? actual = await _productos.ObtenerPorIdAsync(idProducto, ct);

        if (actual is null)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "El producto no existe.");
        }

        if (actual.Estado == 1)
        {
            return Result.Fallo(PrefijosResultado.Conflicto + "El producto ya está activo.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        string usuario = _contextoUsuario.NombreUsuarioActual;
        DateTime ahora = DateTime.Now;

        bool reactivado = await _productos.ReactivarAsync(
            idProducto, idEmpleado, ct);

        if (!reactivado)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "El producto no existe o ya está activo.");
        }

        await _historico.RegistrarAsync(new HistoricoProducto
        {
            IdProducto = idProducto,
            Accion = "REACTIVAR",
            PrecioAnterior = actual.PrecioVenta,
            StockAnterior = actual.CantidadStock,
            Detalle = $"Reactivación de producto realizada por {usuario}.",
            FechaCambio = ahora,
            IdEmpleadoResponsable = idEmpleado
        }, ct);

        return Result.Ok();
    }

    // ===================== Categorías =====================

    public async Task<Result<Categoria>> ObtenerCategoriaPorIdAsync(
        short idCategoria,
        CancellationToken ct = default)
    {
        Categoria? categoria =
            await _catalogo.ObtenerCategoriaPorIdAsync(idCategoria, false, ct);

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

        if (await _catalogo.ExisteCodigoCategoriaAsync(codigo, null, ct))
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
        if (await _catalogo.ExisteCodigoCategoriaAsync(datos.Codigo, null, ct))
        {
            string codigoAlterno =
                await GenerarCodigoCategoriaDisponibleAsync(ct);

            if (codigoAlterno == datos.Codigo
                || await _catalogo.ExisteCodigoCategoriaAsync(
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
            await _catalogo.InsertarCategoriaAsync(categoria, ct);

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
            await _catalogo.ObtenerCategoriaPorIdAsync(idCategoria, false, ct);

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
            await _catalogo.ActualizarCategoriaAsync(actualizada, ct);

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
            await _catalogo.ObtenerCategoriaPorIdAsync(idCategoria, false, ct);

        if (actual is null || actual.Estado != 1)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe o no está activa.");
        }

        if (await _catalogo.TieneProductosActivosCategoriaAsync(idCategoria, ct))
        {
            return Result.Fallo(
                PrefijosResultado.Conflicto +
                "No se puede dar de baja la categoría porque tiene productos activos asociados.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;

        bool dadoDeBaja = await _catalogo.DarDeBajaCategoriaAsync(
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
            await _catalogo.ObtenerCategoriaPorIdAsync(
                idCategoria, incluirInactivos: true, ct);

        if (actual is null)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe.");
        }

        if (actual.Estado == 1)
        {
            return Result.Fallo(PrefijosResultado.Conflicto + "La categoría ya está activa.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;

        bool reactivada = await _catalogo.ReactivarCategoriaAsync(
            idCategoria, idEmpleado, ct);

        return reactivada
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La categoría no existe o ya está activa.");
    }

    // ===================== Marcas =====================

    public async Task<Result<Marca>> ObtenerMarcaPorIdAsync(
        short idMarca,
        CancellationToken ct = default)
    {
        Marca? marca = await _catalogo.ObtenerMarcaPorIdAsync(idMarca, false, ct);

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

        short idMarca = await _catalogo.InsertarMarcaAsync(marca, ct);

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

        Marca? actual = await _catalogo.ObtenerMarcaPorIdAsync(idMarca, false, ct);

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

        bool guardado = await _catalogo.ActualizarMarcaAsync(actualizada, ct);

        return guardado
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La marca no existe o no está activa.");
    }

    public async Task<Result> DarDeBajaMarcaAsync(
        short idMarca,
        CancellationToken ct = default)
    {
        Marca? actual = await _catalogo.ObtenerMarcaPorIdAsync(idMarca, false, ct);

        if (actual is null || actual.Estado != 1)
        {
            return Result.Fallo(
                PrefijosResultado.NoEncontrado + "La marca no existe o no está activa.");
        }

        if (await _catalogo.TieneProductosActivosMarcaAsync(idMarca, ct))
        {
            return Result.Fallo(
                PrefijosResultado.Conflicto +
                "No se puede dar de baja la marca porque tiene productos activos asociados.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;

        bool dadoDeBaja =
            await _catalogo.DarDeBajaMarcaAsync(idMarca, idEmpleado, ct);

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
        Marca? actual = await _catalogo.ObtenerMarcaPorIdAsync(
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

        bool reactivada = await _catalogo.ReactivarMarcaAsync(
            idMarca, idEmpleado, ct);

        return reactivada
            ? Result.Ok()
            : Result.Fallo(
                PrefijosResultado.NoEncontrado + "La marca no existe o ya está activa.");
    }

    private async Task<string> GenerarCodigoDisponibleAsync(
        CancellationToken ct)
    {
        string codigo = await _productos.ObtenerSiguienteCodigoAsync(ct);

        // Si otra petición tomó el correlativo entre la consulta y la
        // verificación, se pide uno nuevo (con límite para no bloquear).
        for (int intento = 0;
             intento < 5 && await _productos.ExisteCodigoAsync(codigo, null, ct);
             intento++)
        {
            codigo = await _productos.ObtenerSiguienteCodigoAsync(ct);
        }

        return codigo;
    }

    private async Task<string> GenerarCodigoCategoriaDisponibleAsync(
        CancellationToken ct)
    {
        string codigo =
            await _catalogo.ObtenerSiguienteCodigoCategoriaAsync(ct);

        // Si otra petición tomó el correlativo entre la consulta y la
        // verificación, se pide uno nuevo (con límite para no bloquear).
        for (int intento = 0;
             intento < 5
             && await _catalogo.ExisteCodigoCategoriaAsync(codigo, null, ct);
             intento++)
        {
            codigo =
                await _catalogo.ObtenerSiguienteCodigoCategoriaAsync(ct);
        }

        return codigo;
    }

    // ===================== Validaciones =====================

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

        if (await _catalogo.ExisteCodigoCategoriaAsync(
                codigoNormalizado, excluirIdCategoria, ct))
        {
            return Result.Fallo<DatosCategoria>(
                PrefijosResultado.Conflicto +
                $"El código '{codigoNormalizado}' ya está registrado en otra categoría activa.");
        }

        if (await _catalogo.ExisteNombreCategoriaAsync(
                nombreNormalizado, excluirIdCategoria, ct))
        {
            return Result.Fallo<DatosCategoria>(
                PrefijosResultado.Conflicto +
                $"El nombre '{nombreNormalizado}' ya está registrado en otra categoría activa.");
        }

        return Result.Ok(new DatosCategoria(
            codigoNormalizado, nombreNormalizado, descripcionNormalizada));
    }

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

        if (await _catalogo.ExisteNombreMarcaAsync(
                nombreNormalizado, excluirIdMarca, ct))
        {
            return Result.Fallo<DatosMarca>(
                PrefijosResultado.Conflicto +
                $"El nombre '{nombreNormalizado}' ya está registrado en otra marca activa.");
        }

        return Result.Ok(new DatosMarca(nombreNormalizado, paisNormalizado));
    }

    private async Task<Result<DatosValidados>> ValidarAsync(
        DatosValidados datos,
        int? excluirIdProducto,
        CancellationToken ct)
    {
        List<string> errores = new();

        string codigo = (datos.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        string nombre = (datos.Nombre ?? string.Empty).Trim();
        string? descripcion = string.IsNullOrWhiteSpace(datos.Descripcion)
            ? null
            : datos.Descripcion.Trim();
        string unidadMedida = (datos.UnidadMedida ?? string.Empty).Trim();

        if (codigo.Length == 0)
        {
            errores.Add("El código es obligatorio.");
        }
        else
        {
            if (codigo.Length is < 3 or > 30)
            {
                errores.Add("El código debe tener entre 3 y 30 caracteres.");
            }

            if (codigo.Any(char.IsWhiteSpace))
            {
                errores.Add("El código no debe contener espacios.");
            }
        }

        if (nombre.Length == 0)
        {
            errores.Add("El nombre es obligatorio.");
        }
        else if (nombre.Length is < 3 or > 150)
        {
            errores.Add("El nombre debe tener entre 3 y 150 caracteres.");
        }

        if (descripcion is not null && descripcion.Length > 500)
        {
            errores.Add("La descripción no puede superar los 500 caracteres.");
        }

        if (unidadMedida.Length == 0)
        {
            errores.Add("La unidad de medida es obligatoria.");
        }
        else if (unidadMedida.Length > 30)
        {
            errores.Add("La unidad de medida no puede superar los 30 caracteres.");
        }

        if (datos.PrecioVenta <= 0)
        {
            errores.Add("El precio de venta debe ser mayor a cero.");
        }

        if (datos.CantidadStock < 0)
        {
            errores.Add("La cantidad de stock no puede ser negativa.");
        }

        if (errores.Count > 0)
        {
            return Result.Fallo<DatosValidados>(string.Join(" ", errores));
        }

        if (!await _catalogo.ExisteCategoriaActivaAsync(datos.IdCategoria, ct))
        {
            errores.Add("La categoría seleccionada no existe o no está activa.");
        }

        if (!await _catalogo.ExisteMarcaActivaAsync(datos.IdMarca, ct))
        {
            errores.Add("La marca seleccionada no existe o no está activa.");
        }

        if (await _productos.ExisteCodigoAsync(codigo, excluirIdProducto, ct))
        {
            return Result.Fallo<DatosValidados>(
                PrefijosResultado.Conflicto + $"El código '{codigo}' ya está registrado.");
        }

        if (errores.Count > 0)
        {
            return Result.Fallo<DatosValidados>(string.Join(" ", errores));
        }

        return Result.Ok(new DatosValidados(
            datos.IdCategoria,
            datos.IdMarca,
            codigo,
            nombre,
            descripcion,
            unidadMedida,
            datos.PrecioVenta,
            datos.CantidadStock));
    }

    private sealed record DatosValidados(
        short IdCategoria,
        short IdMarca,
        string Codigo,
        string Nombre,
        string? Descripcion,
        string UnidadMedida,
        decimal PrecioVenta,
        int CantidadStock);

    private sealed record DatosCategoria(
        string Codigo,
        string Nombre,
        string? Descripcion);

    private sealed record DatosMarca(
        string Nombre,
        string? PaisOrigen);
}
