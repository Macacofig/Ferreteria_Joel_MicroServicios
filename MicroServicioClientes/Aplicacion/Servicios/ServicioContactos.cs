using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Aplicacion.Validaciones;
using MicroServicioClientes.Dominio.Comunes;
using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Dominio.Puertos;

namespace MicroServicioClientes.Aplicacion.Servicios;

/// <summary>
/// Casos de uso de contactos de cliente (segunda tabla del microservicio).
/// Las reglas de negocio se resuelven con Result, sin lanzar excepciones
/// para los casos esperados. Depende solo de puertos y de la Factoría.
/// </summary>
public sealed class ServicioContactos : IServicioContactos
{
    /// <summary>Regla de negocio: un cliente activo puede tener hasta 10 contactos activos.</summary>
    private const int MaximoContactosActivos = 10;

    private readonly IClienteRepositorio _clientes;
    private readonly IClienteContactoRepositorio _contactos;
    private readonly IContextoUsuario _contexto;

    public ServicioContactos(
        IRepositoryCreator<IClienteRepositorio> creadorClientes,
        IRepositoryCreator<IClienteContactoRepositorio> creadorContactos,
        IContextoUsuario contexto)
    {
        _clientes = creadorClientes.CreateRepository();
        _contactos = creadorContactos.CreateRepository();
        _contexto = contexto;
    }

    public async Task<Resultado<IReadOnlyList<ContactoRespuesta>>> ListarAsync(
        int idCliente, bool soloActivos, CancellationToken cancelacion)
    {
        Cliente? cliente = await _clientes.ObtenerPorIdAsync(idCliente, cancelacion);
        if (cliente is null)
        {
            return Resultado<IReadOnlyList<ContactoRespuesta>>.Falla(
                TipoError.NoEncontrado, "El cliente no existe.");
        }

        var contactos = await _contactos.ListarPorClienteAsync(idCliente, soloActivos, cancelacion);
        return Resultado<IReadOnlyList<ContactoRespuesta>>.Exito(
            contactos.Select(ContactoRespuesta.Desde).ToList());
    }

    public async Task<Resultado<ContactoRespuesta>> ObtenerAsync(
        int idContacto, CancellationToken cancelacion)
    {
        ClienteContacto? contacto = await _contactos.ObtenerPorIdAsync(idContacto, cancelacion);
        return contacto is null
            ? NoEncontrado()
            : Resultado<ContactoRespuesta>.Exito(ContactoRespuesta.Desde(contacto));
    }

    public async Task<Resultado<ContactoRespuesta>> CrearAsync(
        int idCliente, ContactoSolicitud solicitud, CancellationToken cancelacion)
    {
        Cliente? cliente = await _clientes.ObtenerPorIdAsync(idCliente, cancelacion);
        if (cliente is null)
        {
            return Resultado<ContactoRespuesta>.Falla(
                TipoError.NoEncontrado, "El cliente no existe.");
        }

        if (!cliente.Activo)
        {
            return Resultado<ContactoRespuesta>.Falla(
                TipoError.Conflicto, "No se pueden agregar contactos a un cliente dado de baja.");
        }

        ContactoSolicitud datos = solicitud.Normalizada();
        Dictionary<string, string> errores = ValidadorContacto.Validar(datos);
        if (errores.Count > 0)
        {
            return Resultado<ContactoRespuesta>.Falla(
                TipoError.Validacion, "Revise los datos del contacto.", errores);
        }

        int activos = await _contactos.ContarActivosAsync(idCliente, cancelacion);
        if (activos >= MaximoContactosActivos)
        {
            return Resultado<ContactoRespuesta>.Falla(
                TipoError.Conflicto, "El cliente ya tiene el máximo de 10 contactos activos.");
        }

        var contacto = new ClienteContacto { IdCliente = idCliente };
        Mapear(datos, contacto);

        int id = await _contactos.InsertarAsync(contacto, _contexto.NombreUsuario, cancelacion);
        ClienteContacto creado = (await _contactos.ObtenerPorIdAsync(id, cancelacion))!;

        return Resultado<ContactoRespuesta>.Exito(
            ContactoRespuesta.Desde(creado), "Contacto registrado correctamente.");
    }

    public async Task<Resultado<ContactoRespuesta>> ActualizarAsync(
        int idContacto, ContactoSolicitud solicitud, CancellationToken cancelacion)
    {
        ClienteContacto? existente = await _contactos.ObtenerPorIdAsync(idContacto, cancelacion);
        if (existente is null)
        {
            return NoEncontrado();
        }

        if (!existente.Activo)
        {
            return Resultado<ContactoRespuesta>.Falla(
                TipoError.Conflicto, "No se puede modificar un contacto dado de baja. Reactívelo primero.");
        }

        ContactoSolicitud datos = solicitud.Normalizada();
        Dictionary<string, string> errores = ValidadorContacto.Validar(datos);
        if (errores.Count > 0)
        {
            return Resultado<ContactoRespuesta>.Falla(
                TipoError.Validacion, "Revise los datos del contacto.", errores);
        }

        Mapear(datos, existente);
        await _contactos.ActualizarAsync(existente, _contexto.NombreUsuario, cancelacion);

        ClienteContacto actualizado = (await _contactos.ObtenerPorIdAsync(idContacto, cancelacion))!;
        return Resultado<ContactoRespuesta>.Exito(
            ContactoRespuesta.Desde(actualizado), "Contacto actualizado correctamente.");
    }

    public async Task<Resultado> DarDeBajaAsync(int idContacto, CancellationToken cancelacion)
    {
        ClienteContacto? existente = await _contactos.ObtenerPorIdAsync(idContacto, cancelacion);
        if (existente is null)
        {
            return Resultado.Falla(TipoError.NoEncontrado, "El contacto no existe.");
        }

        if (!existente.Activo)
        {
            return Resultado.Falla(TipoError.Conflicto, "El contacto ya está dado de baja.");
        }

        await _contactos.DarDeBajaAsync(idContacto, _contexto.NombreUsuario, cancelacion);
        return Resultado.Exito("Contacto dado de baja correctamente.");
    }

    public async Task<Resultado> ReactivarAsync(int idContacto, CancellationToken cancelacion)
    {
        ClienteContacto? existente = await _contactos.ObtenerPorIdAsync(idContacto, cancelacion);
        if (existente is null)
        {
            return Resultado.Falla(TipoError.NoEncontrado, "El contacto no existe.");
        }

        if (existente.Activo)
        {
            return Resultado.Falla(TipoError.Conflicto, "El contacto ya está activo.");
        }

        Cliente? cliente = await _clientes.ObtenerPorIdAsync(existente.IdCliente, cancelacion);
        if (cliente is not null && !cliente.Activo)
        {
            return Resultado.Falla(
                TipoError.Conflicto, "No se puede reactivar el contacto porque el cliente está dado de baja.");
        }

        int activos = await _contactos.ContarActivosAsync(existente.IdCliente, cancelacion);
        if (activos >= MaximoContactosActivos)
        {
            return Resultado.Falla(
                TipoError.Conflicto, "El cliente ya tiene el máximo de 10 contactos activos.");
        }

        await _contactos.ReactivarAsync(idContacto, _contexto.NombreUsuario, cancelacion);
        return Resultado.Exito("Contacto reactivado correctamente.");
    }

    private static void Mapear(ContactoSolicitud datos, ClienteContacto destino)
    {
        destino.Nombre = datos.Nombre;
        destino.Cargo = datos.Cargo;
        destino.Telefono = datos.Telefono;
        destino.Correo = datos.Correo;
    }

    private static Resultado<ContactoRespuesta> NoEncontrado() =>
        Resultado<ContactoRespuesta>.Falla(TipoError.NoEncontrado, "El contacto no existe.");
}
