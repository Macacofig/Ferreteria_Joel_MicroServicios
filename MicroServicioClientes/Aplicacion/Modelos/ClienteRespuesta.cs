using MicroServicioClientes.Dominio.Entidades;

namespace MicroServicioClientes.Aplicacion.Modelos;

/// <summary>Cliente tal como lo ve el front.</summary>
public sealed record ClienteRespuesta(
    int IdCliente,
    string Nombre,
    string Documento,
    string? Correo,
    string Telefono,
    string? Direccion,
    int IdTipoCliente,
    string TipoCliente,
    decimal DescuentoPorcentaje,
    bool Activo,
    DateTime FechaCreacion,
    string UsuarioCreacion,
    DateTime? FechaModificacion,
    string? UsuarioModificacion)
{
    public static ClienteRespuesta Desde(Cliente c) => new(
        c.IdCliente,
        c.Nombre,
        c.Documento,
        c.Correo,
        c.Telefono,
        c.Direccion,
        c.IdTipoCliente,
        c.TipoClienteNombre,
        c.DescuentoPorcentaje,
        c.Activo,
        c.FechaCreacion,
        c.UsuarioCreacion,
        c.FechaModificacion,
        c.UsuarioModificacion);
}
