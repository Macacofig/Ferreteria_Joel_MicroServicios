namespace MicroServicioProveedores.Aplicacion.Modelos;

public class ProveedorDto
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public bool Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string UsuarioCreacion { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
    public DateTime? FechaBaja { get; set; }
    public string? UsuarioBaja { get; set; }
}

public class CrearProveedorSolicitud
{
    public string RazonSocial { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
}

public class ActualizarProveedorSolicitud
{
    public string RazonSocial { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
}
