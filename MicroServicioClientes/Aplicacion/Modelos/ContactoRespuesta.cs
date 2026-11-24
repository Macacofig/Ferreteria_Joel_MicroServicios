using MicroServicioClientes.Dominio.Entidades;

namespace MicroServicioClientes.Aplicacion.Modelos;

/// <summary>Contacto tal como lo ve el front, incluida su auditoría.</summary>
public sealed record ContactoRespuesta(
    int IdContacto,
    int IdCliente,
    string Nombre,
    string? Cargo,
    string? Telefono,
    string? Correo,
    bool Activo,
    DateTime FechaCreacion,
    string UsuarioCreacion,
    DateTime? FechaModificacion,
    string? UsuarioModificacion,
    DateTime? FechaBaja,
    string? UsuarioBaja)
{
    public static ContactoRespuesta Desde(ClienteContacto c) => new(
        c.IdContacto,
        c.IdCliente,
        c.Nombre,
        c.Cargo,
        c.Telefono,
        c.Correo,
        c.Activo,
        c.FechaCreacion,
        c.UsuarioCreacion,
        c.FechaModificacion,
        c.UsuarioModificacion,
        c.FechaBaja,
        c.UsuarioBaja);
}
