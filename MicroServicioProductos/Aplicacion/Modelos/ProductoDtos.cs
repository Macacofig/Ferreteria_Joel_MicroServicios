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

/// <summary>Traducción entre <see cref="Producto"/> y su DTO de respuesta.</summary>
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
}
