using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Dominio.Comunes;

namespace MicroServicioClientes.Aplicacion.Puertos;

/// <summary>Puerto de entrada: casos de uso de tipos de cliente.</summary>
public interface IServicioTiposCliente
{
    Task<Resultado<IReadOnlyList<TipoClienteRespuesta>>> ListarAsync(bool soloActivos, CancellationToken cancelacion);

    Task<Resultado<TipoClienteRespuesta>> ObtenerAsync(int idTipoCliente, CancellationToken cancelacion);

    Task<Resultado<TipoClienteRespuesta>> CrearAsync(TipoClienteSolicitud solicitud, CancellationToken cancelacion);

    Task<Resultado<TipoClienteRespuesta>> ActualizarAsync(int idTipoCliente, TipoClienteSolicitud solicitud, CancellationToken cancelacion);

    Task<Resultado> EliminarAsync(int idTipoCliente, CancellationToken cancelacion);
}
