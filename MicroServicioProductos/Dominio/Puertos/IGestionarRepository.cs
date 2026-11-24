namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>
/// Producto genérico del Factory Method para entidades con ciclo de vida
/// gestionable: además del contrato base permite actualizar, cambiar el
/// estado y consultar solo los registros activos.
/// </summary>
public interface IGestionarRepository<T> : IRepository<T>
{
    Task ActualizarAsync(T entidad);

    Task CambiarEstadoAsync(int id, int idEmpleadoResponsable);

    Task<List<T>> MostrarActivosAsync();
}
