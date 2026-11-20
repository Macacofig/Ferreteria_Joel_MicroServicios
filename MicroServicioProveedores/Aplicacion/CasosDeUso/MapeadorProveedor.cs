using MicroServicioProveedores.Aplicacion.Modelos;
using MicroServicioProveedores.Dominio.Entidades;

namespace MicroServicioProveedores.Aplicacion.CasosDeUso;

public static class MapeadorProveedor
{
    public static ProveedorDto ADto(Proveedor proveedor) => new()
    {
        Id = proveedor.Id,
        RazonSocial = proveedor.RazonSocial,
        Nit = proveedor.Nit,
        Contacto = proveedor.Contacto,
        Telefono = proveedor.Telefono
    };
}
