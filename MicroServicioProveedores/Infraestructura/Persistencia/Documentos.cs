using MongoDB.Bson.Serialization.Attributes;

namespace MicroServicioProveedores.Infraestructura.Persistencia;

internal class ProveedorDocumento
{
    [BsonId]
    public int Id { get; set; }

    [BsonElement("razonSocial")]
    public string RazonSocial { get; set; } = string.Empty;

    [BsonElement("nit")]
    public string Nit { get; set; } = string.Empty;

    [BsonElement("contacto")]
    public string Contacto { get; set; } = string.Empty;

    [BsonElement("telefono")]
    public string Telefono { get; set; } = string.Empty;

    [BsonElement("nombreResponsable")]
    public string NombreResponsable { get; set; } = string.Empty;

    [BsonElement("estado")]
    public bool Estado { get; set; } = true;

    [BsonElement("fechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    [BsonElement("usuarioCreacion")]
    public string UsuarioCreacion { get; set; } = string.Empty;

    [BsonElement("fechaModificacion")]
    public DateTime? FechaModificacion { get; set; }

    [BsonElement("usuarioModificacion")]
    public string? UsuarioModificacion { get; set; }

    [BsonElement("fechaBaja")]
    public DateTime? FechaBaja { get; set; }

    [BsonElement("usuarioBaja")]
    public string? UsuarioBaja { get; set; }
}

internal class ContadorDocumento
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("secuencia")]
    public int Secuencia { get; set; }
}
