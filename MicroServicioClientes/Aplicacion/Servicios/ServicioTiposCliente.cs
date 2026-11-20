using MicroServicioClientes.Aplicacion.Modelos;
using MicroServicioClientes.Aplicacion.Puertos;
using MicroServicioClientes.Aplicacion.Validaciones;
using MicroServicioClientes.Dominio.Comunes;
using MicroServicioClientes.Dominio.Entidades;
using MicroServicioClientes.Dominio.Puertos;

namespace MicroServicioClientes.Aplicacion.Servicios;

/// <summary>Casos de uso de tipos de cliente (segunda tabla con CRUD completo).</summary>
public sealed class ServicioTiposCliente : IServicioTiposCliente
{
    private readonly ITipoClienteRepositorio _tipos;
    private readonly IContextoUsuario _contexto;

    public ServicioTiposCliente(ITipoClienteRepositorio tipos, IContextoUsuario contexto)
    {
        _tipos = tipos;
        _contexto = contexto;
    }

    public async Task<Resultado<IReadOnlyList<TipoClienteRespuesta>>> ListarAsync(
        bool soloActivos,
        CancellationToken cancelacion)
    {
        IReadOnlyList<TipoCliente> tipos = await _tipos.ListarAsync(soloActivos, cancelacion);

        IReadOnlyList<TipoClienteRespuesta> datos = tipos.Select(TipoClienteRespuesta.Desde).ToList();

        return Resultado<IReadOnlyList<TipoClienteRespuesta>>.Exito(datos);
    }

    public async Task<Resultado<TipoClienteRespuesta>> ObtenerAsync(int idTipoCliente, CancellationToken cancelacion)
    {
        TipoCliente? tipo = await _tipos.ObtenerPorIdAsync(idTipoCliente, cancelacion);

        return tipo is null
            ? NoEncontrado()
            : Resultado<TipoClienteRespuesta>.Exito(TipoClienteRespuesta.Desde(tipo));
    }

    public async Task<Resultado<TipoClienteRespuesta>> CrearAsync(
        TipoClienteSolicitud solicitud,
        CancellationToken cancelacion)
    {
        TipoClienteSolicitud datos = solicitud.Normalizada();

        Dictionary<string, string> errores = ValidadorTipoCliente.Validar(datos);
        if (errores.Count > 0)
        {
            return Resultado<TipoClienteRespuesta>.Falla(
                TipoError.Validacion, "Revise los datos del tipo de cliente.", errores);
        }

        if (await _tipos.ExisteNombreAsync(datos.Nombre, null, cancelacion))
        {
            return NombreDuplicado();
        }

        int id = await _tipos.InsertarAsync(Mapear(datos, new TipoCliente()), _contexto.NombreUsuario, cancelacion);

        TipoCliente creado = (await _tipos.ObtenerPorIdAsync(id, cancelacion))!;

        return Resultado<TipoClienteRespuesta>.Exito(
            TipoClienteRespuesta.Desde(creado), "Tipo de cliente registrado correctamente.");
    }

    public async Task<Resultado<TipoClienteRespuesta>> ActualizarAsync(
        int idTipoCliente,
        TipoClienteSolicitud solicitud,
        CancellationToken cancelacion)
    {
        TipoCliente? existente = await _tipos.ObtenerPorIdAsync(idTipoCliente, cancelacion);
        if (existente is null)
        {
            return NoEncontrado();
        }

        TipoClienteSolicitud datos = solicitud.Normalizada();

        Dictionary<string, string> errores = ValidadorTipoCliente.Validar(datos);
        if (errores.Count > 0)
        {
            return Resultado<TipoClienteRespuesta>.Falla(
                TipoError.Validacion, "Revise los datos del tipo de cliente.", errores);
        }

        if (await _tipos.ExisteNombreAsync(datos.Nombre, idTipoCliente, cancelacion))
        {
            return NombreDuplicado();
        }

        await _tipos.ActualizarAsync(Mapear(datos, existente), _contexto.NombreUsuario, cancelacion);

        TipoCliente actualizado = (await _tipos.ObtenerPorIdAsync(idTipoCliente, cancelacion))!;

        return Resultado<TipoClienteRespuesta>.Exito(
            TipoClienteRespuesta.Desde(actualizado), "Tipo de cliente actualizado correctamente.");
    }

    public async Task<Resultado> EliminarAsync(int idTipoCliente, CancellationToken cancelacion)
    {
        TipoCliente? existente = await _tipos.ObtenerPorIdAsync(idTipoCliente, cancelacion);
        if (existente is null)
        {
            return Resultado.Falla(TipoError.NoEncontrado, "El tipo de cliente no existe.");
        }

        int enUso = await _tipos.ContarClientesAsync(idTipoCliente, cancelacion);
        if (enUso > 0)
        {
            return Resultado.Falla(
                TipoError.Conflicto,
                $"No se puede eliminar: {enUso} cliente(s) usan este tipo. Desactívelo en su lugar.");
        }

        await _tipos.EliminarAsync(idTipoCliente, cancelacion);

        return Resultado.Exito("Tipo de cliente eliminado correctamente.");
    }

    private static TipoCliente Mapear(TipoClienteSolicitud datos, TipoCliente destino)
    {
        destino.Nombre = datos.Nombre;
        destino.DescuentoPorcentaje = datos.DescuentoPorcentaje;
        destino.Descripcion = datos.Descripcion;
        destino.Activo = datos.Activo;
        return destino;
    }

    private static Resultado<TipoClienteRespuesta> NoEncontrado() =>
        Resultado<TipoClienteRespuesta>.Falla(TipoError.NoEncontrado, "El tipo de cliente no existe.");

    private static Resultado<TipoClienteRespuesta> NombreDuplicado() =>
        Resultado<TipoClienteRespuesta>.Falla(
            TipoError.Conflicto,
            "Ya existe un tipo de cliente con ese nombre.",
            new Dictionary<string, string>
            {
                [nameof(TipoClienteSolicitud.Nombre)] = "Este nombre ya está registrado."
            });
}