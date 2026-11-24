using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Puertos;
using MicroServicioProductos.Dominio.Resultados;
using MicroServicioProductos.Infraestructura.Factories;

namespace MicroServicioProductos.Aplicacion.Servicios;

/// <summary>
/// Casos de uso del catálogo de productos y su histórico. Las reglas de
/// negocio se devuelven como <see cref="Result"/>; los repositorios se obtienen
/// mediante el Factory Method inyectado, nunca por su clase concreta.
/// </summary>
public sealed class ServicioProductos : IServicioProductos
{
    private readonly IRepositoryProducto _productos;
    private readonly IRepositoryHistoricoProducto _historico;
    private readonly IRepositoryCategoria _categorias;
    private readonly IRepositoryMarca _marcas;
    private readonly IContextoUsuario _contextoUsuario;

    public ServicioProductos(
        RepositoryCreator<IRepositoryProducto> creadorProductos,
        RepositoryCreator<IRepositoryHistoricoProducto> creadorHistorico,
        RepositoryCreator<IRepositoryCategoria> creadorCategorias,
        RepositoryCreator<IRepositoryMarca> creadorMarcas,
        IContextoUsuario contextoUsuario)
    {
        _productos = creadorProductos.CreateRepository();
        _historico = creadorHistorico.CreateRepository();
        _categorias = creadorCategorias.CreateRepository();
        _marcas = creadorMarcas.CreateRepository();
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
            return Result.Fallo(
                PrefijosResultado.Conflicto + "El producto ya está activo.");
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

    // ===================== Auxiliares =====================

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

        if (!await _categorias.ExisteCategoriaActivaAsync(datos.IdCategoria, ct))
        {
            errores.Add("La categoría seleccionada no existe o no está activa.");
        }

        if (!await _marcas.ExisteMarcaActivaAsync(datos.IdMarca, ct))
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
}
