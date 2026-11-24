using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Aplicacion.Modelos;

public sealed record CrearMarcaRequest(
    string? Nombre,
    string? PaisOrigen);

public sealed record ActualizarMarcaRequest(
    string? Nombre,
    string? PaisOrigen);

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

/// <summary>Traducción entre <see cref="Marca"/> y su DTO de respuesta.</summary>
public static class MapeosMarca
{
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
}
