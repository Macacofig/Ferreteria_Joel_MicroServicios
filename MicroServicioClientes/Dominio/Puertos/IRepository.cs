namespace MicroServicioClientes.Dominio.Puertos;

/// <summary>
/// Contrato base de persistencia. Todas las operaciones son asíncronas
/// para no bloquear el hilo durante el acceso a la base de datos.
/// <para>
/// Es el <em>Producto</em> genérico del Factory Method: los creadores
/// devuelven repositorios que cumplen este contrato, aunque los casos de uso
/// trabajen con interfaces más específicas que lo extienden.
/// </para>
/// </summary>
public interface IRepository<T>
{
    Task<List<T>> ObtenerTodosAsync();

    Task<int> InsertarAsync(T entidad);
}
