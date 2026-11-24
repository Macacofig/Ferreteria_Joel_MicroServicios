namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Cliente tal como lo devuelve MicroServicioClientes. Es una copia
    /// deliberada del contrato HTTP: el front solo comparte JSON con la API.
    /// </summary>
    public class ClienteApi
    {
        public int IdCliente { get; set; }

        public string? Ci { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string? Correo { get; set; }

        public string? Telefono { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public string UsuarioCreacion { get; set; } = string.Empty;

        public DateTime? FechaModificacion { get; set; }

        public string? UsuarioModificacion { get; set; }
    }

    /// <summary>Datos del formulario de alta y edición de un cliente.</summary>
    public class ClienteSolicitudApi
    {
        public string? Ci { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string? Correo { get; set; }

        public string? Telefono { get; set; }

        public string? Direccion { get; set; }
    }

    /// <summary>
    /// Persona de contacto de un cliente (segunda tabla de
    /// MicroServicioClientes), incluida su auditoría.
    /// </summary>
    public class ContactoApi
    {
        public int IdContacto { get; set; }

        public int IdCliente { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Cargo { get; set; }

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public string UsuarioCreacion { get; set; } = string.Empty;

        public DateTime? FechaModificacion { get; set; }

        public string? UsuarioModificacion { get; set; }

        public DateTime? FechaBaja { get; set; }

        public string? UsuarioBaja { get; set; }
    }

    /// <summary>Datos del formulario de alta y edición de un contacto.</summary>
    public class ContactoSolicitudApi
    {
        public string Nombre { get; set; } = string.Empty;

        public string? Cargo { get; set; }

        public string? Telefono { get; set; }

        public string? Correo { get; set; }
    }
}

