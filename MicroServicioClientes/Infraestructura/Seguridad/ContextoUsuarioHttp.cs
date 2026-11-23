using MicroServicioClientes.Aplicacion.Puertos;

namespace MicroServicioClientes.Infraestructura.Seguridad;

/// <summary>Adaptador de <see cref="IContextoUsuario"/>: toma el usuario del JWT validado.</summary>
public sealed class ContextoUsuarioHttp : IContextoUsuario
{
    private readonly IHttpContextAccessor _accesor;

    public ContextoUsuarioHttp(IHttpContextAccessor accesor)
    {
        _accesor = accesor;
    }

    public string NombreUsuario =>
        _accesor.HttpContext?.User.Identity?.Name is { Length: > 0 } nombre ? nombre : "sistema";
}