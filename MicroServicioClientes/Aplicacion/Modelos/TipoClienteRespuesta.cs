using MicroServicioClientes.Dominio.Entidades;

namespace MicroServicioClientes.Aplicacion.Modelos;

public sealed record TipoClienteRespuesta(
    int IdTipoCliente,
    string Nombre,
    decimal DescuentoPorcentaje,
    string? Descripcion,
    bool Activo,
    DateTime FechaCreacion,
    string UsuarioCreacion,
    DateTime? FechaModificacion,
    string? UsuarioModificacion)
{
    public static TipoClienteRespuesta Desde(TipoCliente t) => new(
        t.IdTipoCliente,
        t.Nombre,
        t.DescuentoPorcentaje,
        t.Descripcion,
        t.Activo,
        t.FechaCreacion,
        t.UsuarioCreacion,
        t.FechaModificacion,
        t.UsuarioModificacion);
}
