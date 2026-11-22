namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>
/// Identidad del usuario autenticado, resuelta por el adaptador Web a partir
/// de las claims. Productos solo consume el dato para la auditoría.
/// </summary>
public interface IContextoUsuario
{
    int? IdEmpleadoActual { get; }

    string NombreUsuarioActual { get; }
}
