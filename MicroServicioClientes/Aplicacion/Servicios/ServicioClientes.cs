using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Aplicacion.Validaciones;
using MicroServicioClientes.Dominio.Comunes;
using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Dominio.Puertos;

namespace MicroServicioClientes.Aplicacion.Servicios;

/// <summary>
/// Casos de uso de clientes. Depende solo de puertos (repositorio y contexto
/// de usuario), nunca de PostgreSQL ni de ASP.NET.
/// </summary>
public sealed class ServicioClientes : IServicioClientes
{
    private readonly IClienteRepositorio _clientes;
    private readonly IContextoUsuario _contexto;

    public ServicioClientes(
        IRepositoryCreator<IClienteRepositorio> creadorRepositorio,
        IContextoUsuario contexto)
    {
        _clientes = creadorRepositorio.CreateRepository();
        _contexto = contexto;
    }

    public async Task<Resultado<IReadOnlyList<ClienteRespuesta>>> ListarAsync(
        FiltroClientes filtro,
        CancellationToken cancelacion)
    {
        IReadOnlyList<Cliente> clientes = await _clientes.ListarAsync(filtro, cancelacion);

        IReadOnlyList<ClienteRespuesta> datos = clientes.Select(ClienteRespuesta.Desde).ToList();

        string mensaje = datos.Count == 0
            ? "No se encontraron clientes con los criterios indicados."
            : $"Se encontraron {datos.Count} cliente(s).";

        return Resultado<IReadOnlyList<ClienteRespuesta>>.Exito(datos, mensaje);
    }

    public async Task<Resultado<ClienteRespuesta>> ObtenerAsync(int idCliente, CancellationToken cancelacion)
    {
        Cliente? cliente = await _clientes.ObtenerPorIdAsync(idCliente, cancelacion);

        return cliente is null
            ? NoEncontrado()
            : Resultado<ClienteRespuesta>.Exito(
                ClienteRespuesta.Desde(cliente), "Cliente obtenido correctamente.");
    }

    public async Task<Resultado<ClienteRespuesta>> CrearAsync(
        ClienteSolicitud solicitud,
        CancellationToken cancelacion)
    {
        ClienteSolicitud datos = solicitud.Normalizada();

        Dictionary<string, string> errores = ValidadorCliente.Validar(datos);
        if (errores.Count > 0)
        {
            return Resultado<ClienteRespuesta>.Falla(
                TipoError.Validacion, "Revise los datos del cliente.", errores);
        }

        if (datos.Ci is not null && await _clientes.ExisteCiAsync(datos.Ci, null, cancelacion))
        {
            return CiDuplicada();
        }

        int id = await _clientes.InsertarAsync(Mapear(datos, new Cliente()), _contexto.NombreUsuario, cancelacion);

        Cliente creado = (await _clientes.ObtenerPorIdAsync(id, cancelacion))!;

        return Resultado<ClienteRespuesta>.Exito(
            ClienteRespuesta.Desde(creado), "Cliente registrado correctamente.");
    }

    public async Task<Resultado<ClienteRespuesta>> ActualizarAsync(
        int idCliente,
        ClienteSolicitud solicitud,
        CancellationToken cancelacion)
    {
        Cliente? existente = await _clientes.ObtenerPorIdAsync(idCliente, cancelacion);
        if (existente is null)
        {
            return NoEncontrado();
        }

        if (!existente.Activo)
        {
            return Resultado<ClienteRespuesta>.Falla(
                TipoError.Conflicto, "No se puede modificar un cliente dado de baja. Reactívelo primero.");
        }

        ClienteSolicitud datos = solicitud.Normalizada();

        Dictionary<string, string> errores = ValidadorCliente.Validar(datos);
        if (errores.Count > 0)
        {
            return Resultado<ClienteRespuesta>.Falla(
                TipoError.Validacion, "Revise los datos del cliente.", errores);
        }

        if (datos.Ci is not null && await _clientes.ExisteCiAsync(datos.Ci, idCliente, cancelacion))
        {
            return CiDuplicada();
        }

        await _clientes.ActualizarAsync(Mapear(datos, existente), _contexto.NombreUsuario, cancelacion);

        Cliente actualizado = (await _clientes.ObtenerPorIdAsync(idCliente, cancelacion))!;

        return Resultado<ClienteRespuesta>.Exito(
            ClienteRespuesta.Desde(actualizado), "Cliente actualizado correctamente.");
    }

    public async Task<Resultado> DarDeBajaAsync(int idCliente, CancellationToken cancelacion)
    {
        Cliente? existente = await _clientes.ObtenerPorIdAsync(idCliente, cancelacion);
        if (existente is null)
        {
            return Resultado.Falla(TipoError.NoEncontrado, "El cliente no existe.");
        }

        if (!existente.Activo)
        {
            return Resultado.Falla(TipoError.Conflicto, "El cliente ya está dado de baja.");
        }

        await _clientes.DarDeBajaAsync(idCliente, _contexto.NombreUsuario, cancelacion);

        return Resultado.Exito("Cliente dado de baja correctamente.");
    }

    public async Task<Resultado> ReactivarAsync(int idCliente, CancellationToken cancelacion)
    {
        Cliente? existente = await _clientes.ObtenerPorIdAsync(idCliente, cancelacion);
        if (existente is null)
        {
            return Resultado.Falla(TipoError.NoEncontrado, "El cliente no existe.");
        }

        if (existente.Activo)
        {
            return Resultado.Falla(TipoError.Conflicto, "El cliente ya está activo.");
        }

        await _clientes.ReactivarAsync(idCliente, _contexto.NombreUsuario, cancelacion);

        return Resultado.Exito("Cliente reactivado correctamente.");
    }

    private static Cliente Mapear(ClienteSolicitud datos, Cliente destino)
    {
        destino.Ci = datos.Ci;
        destino.Nombre = datos.Nombre;
        destino.Apellido = datos.Apellido;
        destino.Correo = datos.Correo;
        destino.Telefono = datos.Telefono;
        destino.Direccion = datos.Direccion;
        return destino;
    }

    private static Resultado<ClienteRespuesta> NoEncontrado() =>
        Resultado<ClienteRespuesta>.Falla(TipoError.NoEncontrado, "El cliente no existe.");

    private static Resultado<ClienteRespuesta> CiDuplicada() =>
        Resultado<ClienteRespuesta>.Falla(
            TipoError.Conflicto,
            "Ya existe un cliente con esa CI.",
            new Dictionary<string, string>
            {
                [nameof(ClienteSolicitud.Ci)] = "Esta CI ya está registrada."
            });
}