using MicroServicioClientes.Dominio.Entidades;

namespace MicroServicioClientes.Aplicacion.Modelos;

/// <summary>Cliente tal como lo ve el front.</summary>
public sealed record ClienteRespuesta(
    int IdCliente,
    string? Ci,
    string Nombre,
    string Apellido,
    string? Correo,
    string? Telefono,
    string? Direccion,
    bool Activo,
    DateTime FechaCreacion,
    string UsuarioCreacion,
    DateTime? FechaModificacion,
    string? UsuarioModificacion,
    DateTime? FechaBaja,
    string? UsuarioBaja)
{
    public static ClienteRespuesta Desde(Cliente c) => new(
        c.IdCliente,
        c.Ci,
        c.Nombre,
        c.Apellido,
        c.Correo,
        c.Telefono,
        c.Direccion,
        c.Activo,
        c.FechaCreacion,
        c.UsuarioCreacion,
        c.FechaModificacion,
        c.UsuarioModificacion,
        c.FechaBaja,
        c.UsuarioBaja);
}

