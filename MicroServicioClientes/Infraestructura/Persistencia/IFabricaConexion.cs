using Npgsql;

namespace MicroServicioClientes.Infraestructura.Persistencia;

/// <summary>Patrón Factory: único punto donde se crean conexiones a PostgreSQL.</summary>
public interface IFabricaConexion
{
    NpgsqlConnection Crear();
}
