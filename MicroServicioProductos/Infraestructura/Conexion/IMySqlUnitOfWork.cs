using MicroServicioProductos.Dominio.Puertos;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Conexion;

/// <summary>
/// Extensión MySQL de <see cref="IUnitOfWork"/>. Los repositorios leen la
/// conexión y la transacción actuales para que todas sus sentencias queden
/// dentro del mismo contexto transaccional. Aplicación no ve estos tipos.
/// </summary>
public interface IMySqlUnitOfWork : IUnitOfWork
{
    MySqlConnection Connection { get; }

    MySqlTransaction? Transaction { get; }
}
