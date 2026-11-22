namespace MicroServicioClientes.Dominio.Entidades;

/// <summary>Criterios de consulta del listado de clientes.</summary>
/// <param name="Buscar">Texto a buscar en CI, nombre, apellido o correo.</param>
/// <param name="SoloActivos">Si es verdadero, excluye los clientes dados de baja.</param>
/// <param name="OrdenarPor">nombre (por defecto), apellido, ci o fecha.</param>
/// <param name="Descendente">Invierte el sentido del orden.</param>
public sealed record FiltroClientes(
    string? Buscar,
    bool SoloActivos,
    string? OrdenarPor,
    bool Descendente);
