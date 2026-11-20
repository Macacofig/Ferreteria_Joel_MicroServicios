using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Dominio.Comunes;
using MicroServicioClientes.Dominio.Entidades;

namespace MicroServicioClientes.Aplicacion.Puertos;

/// <summary>Puerto de entrada: casos de uso de clientes que consume el controlador.</summary>
public interface IServicioClientes
{
    Task<Resultado<IReadOnlyList<ClienteRespuesta>>> ListarAsync(FiltroClientes filtro, CancellationToken cancelacion);

    Task<Resultado<ClienteRespuesta>> ObtenerAsync(int idCliente, CancellationToken cancelacion);

    Task<Resultado<ClienteRespuesta>> CrearAsync(ClienteSolicitud solicitud, CancellationToken cancelacion);

    Task<Resultado<ClienteRespuesta>> ActualizarAsync(int idCliente, ClienteSolicitud solicitud, CancellationToken cancelacion);

    Task<Resultado> DarDeBajaAsync(int idCliente, CancellationToken cancelacion);
}