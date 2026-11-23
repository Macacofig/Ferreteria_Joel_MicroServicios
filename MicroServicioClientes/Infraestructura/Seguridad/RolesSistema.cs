namespace MicroServicioClientes.Infraestructura.Seguridad;

/// <summary>Roles del realm de Keycloak (ver Keycloak/ferreteria-realm.json).</summary>
public static class RolesSistema
{
    public const string Administrador = "Administrador";

    public const string EmpleadoVenta = "EmpleadoVenta";

    public const string AdministradorOEmpleado = Administrador + "," + EmpleadoVenta;
}