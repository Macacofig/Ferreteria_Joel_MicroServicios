namespace MicroServicioClientes.Dominio.Entidades;

/// <summary>Criterios de consulta del listado de clientes.</summary>
/// <param name="Buscar">Texto a buscar en nombre, documento o correo.</param>
/// <param name="IdTipoCliente">Limita el listado a un tipo de cliente.</param>
/// <param name="SoloActivos">Si es verdadero, excluye los clientes dados de baja.</param>
/// <param name="OrdenarPor">nombre (por defecto), documento, tipo o fecha.</param>
/// <param name="Descendente">Invierte el sentido del orden.</param>
public sealed record FiltroClientes(
    string? Buscar,
    int? IdTipoCliente,
    bool SoloActivos,
    string? OrdenarPor,
    bool Descendente);
