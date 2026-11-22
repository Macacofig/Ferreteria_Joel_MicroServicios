using MicroServicioProductos.Aplicacion.Modelos;
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
public sealed class ServicioProductos
{
    public const string PrefijoNoEncontrado = "[NO_ENCONTRADO]";
    public const string PrefijoConflicto = "[CONFLICTO]";

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
                PrefijoNoEncontrado + "El producto no existe.")
            : Result.Ok(producto);
    }

    public async Task<Result<IReadOnlyList<Categoria>>> ListarCategoriasAsync(
        CancellationToken ct = default)
    {
        IReadOnlyList<Categoria> categorias =
            await _catalogo.ListarCategoriasActivasAsync(ct);

        return Result.Ok(categorias);
    }

    public async Task<Result<IReadOnlyList<Marca>>> ListarMarcasAsync(
        CancellationToken ct = default)
    {
        IReadOnlyList<Marca> marcas =
            await _catalogo.ListarMarcasActivasAsync(ct);

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
                PrefijoNoEncontrado + "El producto no existe.");
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
            null,
            ct);

        if (validacion.EsFallo)
        {
            return Result.Fallo<int>(validacion.Error);
        }

        DatosValidados datos = validacion.ValorRequerido;
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
                PrefijoNoEncontrado + "El producto no existe o no está activo.");
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
                PrefijoNoEncontrado + "El producto no existe o no está activo.");
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
                PrefijoNoEncontrado + "El producto no existe o no está activo.");
        }

        int? idEmpleado = _contextoUsuario.IdEmpleadoActual;
        string usuario = _contextoUsuario.NombreUsuarioActual;
        DateTime ahora = DateTime.Now;

        bool dadoDeBaja = await _productos.DarDeBajaAsync(
            idProducto, idEmpleado, ct);

        if (!dadoDeBaja)
        {
            return Result.Fallo(
                PrefijoNoEncontrado + "El producto no existe o no está activo.");
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

    // ===================== Validaciones =====================

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
                PrefijoConflicto + $"El código '{codigo}' ya está registrado.");
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
}
