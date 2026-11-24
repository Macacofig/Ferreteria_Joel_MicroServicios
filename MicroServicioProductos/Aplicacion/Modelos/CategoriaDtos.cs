using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Aplicacion.Modelos;

public sealed record CrearCategoriaRequest(
    string? Codigo,
    string? Nombre,
    string? Descripcion);

public sealed record ActualizarCategoriaRequest(
    string? Codigo,
    string? Nombre,
    string? Descripcion);

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

/// <summary>Traducción entre <see cref="Categoria"/> y su DTO de respuesta.</summary>
public static class MapeosCategoria
{
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
}
