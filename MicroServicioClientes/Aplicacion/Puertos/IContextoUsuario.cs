namespace MicroServicioClientes.Aplicacion.Puertos;

/// <summary>Puerto de salida: quién ejecuta la operación (para los campos de auditoría).</summary>
public interface IContextoUsuario
{
    string NombreUsuario { get; }
}
