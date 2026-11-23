using System.Security.Claims;
using MicroServicioProductos.Dominio.Puertos;

namespace MicroServicioProductos.Servicios;

/// <summary>
/// Adaptador del puerto <see cref="IContextoUsuario"/>. Resuelve la identidad
/// desde el JWT ya validado por Keycloak, igual que MicroServicioUsuarios.
/// </summary>
public sealed class ContextoUsuarioJwt : IContextoUsuario
{
    private readonly IHttpContextAccessor _accesor;

    public ContextoUsuarioJwt(IHttpContextAccessor accesor)
    {
        _accesor = accesor;
    }

    private ClaimsPrincipal? Usuario => _accesor.HttpContext?.User;

    public int? IdEmpleadoActual =>
        int.TryParse(
            Usuario?.FindFirstValue("id_empleado"),
            out int id)
            ? id
            : null;

    public string NombreUsuarioActual =>
        Usuario?.FindFirstValue(ClaimTypes.Name)
        ?? Usuario?.Identity?.Name
        ?? Usuario?.FindFirstValue("preferred_username")
        ?? string.Empty;
}
