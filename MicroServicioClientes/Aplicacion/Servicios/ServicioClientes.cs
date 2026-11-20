using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Aplicacion.Validaciones;
using MicroServicioClientes.Dominio.Comunes;
using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Dominio.Puertos;

namespace MicroServicioClientes.Aplicacion.Servicios;

/// <summary>
/// Casos de uso de clientes. Depende solo de puertos (repositorios y contexto
/// de usuario), nunca de PostgreSQL ni de ASP.NET.
/// </summary>
public sealed class ServicioClientes : IServicioClientes
{
    private readonly IClienteRepositorio _clientes;
    private readonly ITipoClienteRepositorio _tipos;
    private readonly IContextoUsuario _contexto;

    public ServicioClientes(
        IClienteRepositorio clientes,
        ITipoClienteRepositorio tipos,
        IContextoUsuario contexto)
    {
        _clientes = clientes;
        _tipos = tipos;
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
            : string.Empty;

        return Resultado<IReadOnlyList<ClienteRespuesta>>.Exito(datos, mensaje);
    }

    public async Task<Resultado<ClienteRespuesta>> ObtenerAsync(int idCliente, CancellationToken cancelacion)
    {
        Cliente? cliente = await _clientes.ObtenerPorIdAsync(idCliente, cancelacion);

        return cliente is null
            ? NoEncontrado()
            : Resultado<ClienteRespuesta>.Exito(ClienteRespuesta.Desde(cliente));
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

        Resultado tipo = await VerificarTipoActivoAsync(datos.IdTipoCliente, cancelacion);
        if (!tipo.Exitoso)
        {
            return Resultado<ClienteRespuesta>.DesdeFallo(tipo);
        }

        if (await _clientes.ExisteDocumentoAsync(datos.Documento, null, cancelacion))
        {
            return DocumentoDuplicado();
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
                TipoError.Conflicto, "No se puede modificar un cliente dado de baja.");
        }

        ClienteSolicitud datos = solicitud.Normalizada();

        Dictionary<string, string> errores = ValidadorCliente.Validar(datos);
        if (errores.Count > 0)
        {
            return Resultado<ClienteRespuesta>.Falla(
                TipoError.Validacion, "Revise los datos del cliente.", errores);
        }

        Resultado tipo = await VerificarTipoActivoAsync(datos.IdTipoCliente, cancelacion);
        if (!tipo.Exitoso)
        {
            return Resultado<ClienteRespuesta>.DesdeFallo(tipo);
        }

        if (await _clientes.ExisteDocumentoAsync(datos.Documento, idCliente, cancelacion))
        {
            return DocumentoDuplicado();
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

    private async Task<Resultado> VerificarTipoActivoAsync(int idTipoCliente, CancellationToken cancelacion)
    {
        TipoCliente? tipo = await _tipos.ObtenerPorIdAsync(idTipoCliente, cancelacion);

        if (tipo is null || !tipo.Activo)
        {
            return Resultado.Falla(
                TipoError.Validacion,
                "Revise los datos del cliente.",
                new Dictionary<string, string>
                {
                    [nameof(ClienteSolicitud.IdTipoCliente)] = "El tipo de cliente no existe o está inactivo."
                });
        }

        return Resultado.Exito();
    }

    private static Cliente Mapear(ClienteSolicitud datos, Cliente destino)
    {
        destino.Nombre = datos.Nombre;
        destino.Documento = datos.Documento;
        destino.Correo = datos.Correo;
        destino.Telefono = datos.Telefono;
        destino.Direccion = datos.Direccion;
        destino.IdTipoCliente = datos.IdTipoCliente;
        return destino;
    }

    private static Resultado<ClienteRespuesta> NoEncontrado() =>
        Resultado<ClienteRespuesta>.Falla(TipoError.NoEncontrado, "El cliente no existe.");

    private static Resultado<ClienteRespuesta> DocumentoDuplicado() =>
        Resultado<ClienteRespuesta>.Falla(
            TipoError.Conflicto,
            "Ya existe un cliente con ese documento.",
            new Dictionary<string, string>
            {
                [nameof(ClienteSolicitud.Documento)] = "Este documento ya está registrado."
            });
}