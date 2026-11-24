using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Dominio.Comunes;

namespace MicroServicioClientes.Aplicacion.Puertos;

/// <summary>Puerto de entrada: casos de uso de la segunda tabla del microservicio.</summary>
public interface IServicioContactos
{
    Task<Resultado<IReadOnlyList<ContactoRespuesta>>> ListarAsync(
        int idCliente, bool soloActivos, CancellationToken cancelacion);

    Task<Resultado<ContactoRespuesta>> ObtenerAsync(int idContacto, CancellationToken cancelacion);

    Task<Resultado<ContactoRespuesta>> CrearAsync(
        int idCliente, ContactoSolicitud solicitud, CancellationToken cancelacion);

    Task<Resultado<ContactoRespuesta>> ActualizarAsync(
        int idContacto, ContactoSolicitud solicitud, CancellationToken cancelacion);

    Task<Resultado> DarDeBajaAsync(int idContacto, CancellationToken cancelacion);

    Task<Resultado> ReactivarAsync(int idContacto, CancellationToken cancelacion);
}
