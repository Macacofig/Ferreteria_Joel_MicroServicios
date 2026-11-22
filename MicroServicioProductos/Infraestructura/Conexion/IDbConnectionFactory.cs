using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Conexion;

/// <summary>Fábrica de conexiones MySQL del microservicio de productos.</summary>
public interface IDbConnectionFactory
{
    MySqlConnection CreateConnection();
}
