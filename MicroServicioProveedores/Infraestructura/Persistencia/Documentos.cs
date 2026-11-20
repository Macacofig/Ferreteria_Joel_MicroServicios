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
}

internal class ContadorDocumento
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("secuencia")]
    public int Secuencia { get; set; }
}
