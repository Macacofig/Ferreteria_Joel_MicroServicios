using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Aplicacion.Modelos;

public sealed record CrearProductoRequest(
    short IdCategoria,
    short IdMarca,
    string? Codigo,
    string? Nombre,
    string? Descripcion,
    string? UnidadMedida,
    decimal PrecioVenta,
    int CantidadStock);

public sealed record ActualizarProductoRequest(
    short IdCategoria,
    short IdMarca,
    string? Codigo,
    string? Nombre,
    string? Descripcion,
    string? UnidadMedida,
    decimal PrecioVenta,
    int CantidadStock);

public sealed record ProductoResponse(
    int IdProducto,
    short IdCategoria,
    short IdMarca,
    string Codigo,
    string Nombre,
    string? Descripcion,
    string UnidadMedida,
    decimal PrecioVenta,
    int CantidadStock,
    byte Estado,
    string CategoriaNombre,
    string MarcaNombre,
    DateTime FechaRegistro,
    DateTime? FechaActualizacion,
    int? UsuarioCreacion,
    DateTime? FechaCreacion,
    int? UsuarioModificacion,
    DateTime? FechaModificacion,
    int? UsuarioBaja,
    DateTime? FechaBaja,
    int? IdEmpleadoResponsable);

// ===================== Catálogos auxiliares =====================

public sealed record CrearCategoriaRequest(
    string? Codigo,
    string? Nombre,
    string? Descripcion);

public sealed record ActualizarCategoriaRequest(
    string? Codigo,
    string? Nombre,
    string? Descripcion);

public sealed record CrearMarcaRequest(
    string? Nombre,
    string? PaisOrigen);

public sealed record ActualizarMarcaRequest(
    string? Nombre,
    string? PaisOrigen);

public sealed record CategoriaResponse(
    short IdCategoria,
    string Codigo,
    string Nombre,
    string? Descripcion,
    byte Estado,
    DateTime FechaRegistro,
    DateTime? FechaActualizacion,
    int? UsuarioCreacion,
    DateTime? FechaCreacion,
    int? UsuarioModificacion,
    DateTime? FechaModificacion,
    int? UsuarioBaja,
    DateTime? FechaBaja,
    int? IdEmpleadoResponsable);

public sealed record MarcaResponse(
    short IdMarca,
    string Nombre,
    string? PaisOrigen,
    byte Estado,
    DateTime FechaRegistro,
    DateTime? FechaActualizacion,
    int? UsuarioCreacion,
    DateTime? FechaCreacion,
    int? UsuarioModificacion,
    DateTime? FechaModificacion,
    int? UsuarioBaja,
    DateTime? FechaBaja,
    int? IdEmpleadoResponsable);

public sealed record HistoricoProductoResponse(
    int IdHistorico,
    int IdProducto,
    string Accion,
    decimal? PrecioAnterior,
    decimal? PrecioNuevo,
    int? StockAnterior,
    int? StockNuevo,
    string? Detalle,
    DateTime FechaCambio,
    int? IdEmpleadoResponsable);

/// <summary>Traducción entre entidades del dominio y DTOs de la API.</summary>
public static class MapeosProducto
{
    public static ProductoResponse AResponse(Producto producto) => new(
        producto.IdProducto,
        producto.IdCategoria,
        producto.IdMarca,
        producto.Codigo,
        producto.Nombre,
        producto.Descripcion,
        producto.UnidadMedida,
        producto.PrecioVenta,
        producto.CantidadStock,
        producto.Estado,
        producto.CategoriaNombre,
        producto.MarcaNombre,
        producto.FechaRegistro,
        producto.FechaActualizacion,
        producto.UsuarioCreacion,
        producto.FechaCreacion,
        producto.UsuarioModificacion,
        producto.FechaModificacion,
        producto.UsuarioBaja,
        producto.FechaBaja,
        producto.IdEmpleadoResponsable);

    public static CategoriaResponse AResponse(Categoria categoria) => new(
        categoria.IdCategoria,
        categoria.Codigo,
        categoria.Nombre,
        categoria.Descripcion,
        categoria.Estado,
        categoria.FechaRegistro,
        categoria.FechaActualizacion,
        categoria.UsuarioCreacion,
        categoria.FechaCreacion,
        categoria.UsuarioModificacion,
        categoria.FechaModificacion,
        categoria.UsuarioBaja,
        categoria.FechaBaja,
        categoria.IdEmpleadoResponsable);

    public static MarcaResponse AResponse(Marca marca) => new(
        marca.IdMarca,
        marca.Nombre,
        marca.PaisOrigen,
        marca.Estado,
        marca.FechaRegistro,
        marca.FechaActualizacion,
        marca.UsuarioCreacion,
        marca.FechaCreacion,
        marca.UsuarioModificacion,
        marca.FechaModificacion,
        marca.UsuarioBaja,
        marca.FechaBaja,
        marca.IdEmpleadoResponsable);

    public static HistoricoProductoResponse AResponse(HistoricoProducto registro) => new(
        registro.IdHistorico,
        registro.IdProducto,
        registro.Accion,
        registro.PrecioAnterior,
        registro.PrecioNuevo,
        registro.StockAnterior,
        registro.StockNuevo,
        registro.Detalle,
        registro.FechaCambio,
        registro.IdEmpleadoResponsable);
}
