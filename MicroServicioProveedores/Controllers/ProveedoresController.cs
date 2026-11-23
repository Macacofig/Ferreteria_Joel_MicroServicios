using MicroServicioProveedores.Aplicacion.CasosDeUso;
using MicroServicioProveedores.Aplicacion.Modelos;
using MicroServicioProveedores.Comun;
using MicroServicioProveedores.Dominio.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProveedores.Controllers;

[ApiController]
[Route("api/proveedores")]
[Authorize]
public class ProveedoresController : ControllerBase
{
    private readonly ListarProveedoresCasoDeUso _listar;
    private readonly ObtenerProveedorCasoDeUso _obtener;
    private readonly CrearProveedorCasoDeUso _crear;
    private readonly ActualizarProveedorCasoDeUso _actualizar;
    private readonly EliminarProveedorCasoDeUso _eliminar;

    public ProveedoresController(
        ListarProveedoresCasoDeUso listar,
        ObtenerProveedorCasoDeUso obtener,
        CrearProveedorCasoDeUso crear,
        ActualizarProveedorCasoDeUso actualizar,
        EliminarProveedorCasoDeUso eliminar)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _eliminar = eliminar;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? orden, CancellationToken ct)
    {
        var resultado = await _listar.EjecutarAsync(orden, ct);
        return DesdeResultado(resultado, datos => Ok(RespuestaApi<IReadOnlyList<ProveedorDto>>.Ok(datos)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var resultado = await _obtener.EjecutarAsync(id, ct);
        return DesdeResultado(resultado, datos => Ok(RespuestaApi<ProveedorDto>.Ok(datos)));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearProveedorSolicitud solicitud, CancellationToken ct)
    {
        if (solicitud is null)
        {
            return BadRequest(RespuestaApi<ProveedorDto>.Error("Los datos del proveedor son obligatorios."));
        }

        var resultado = await _crear.EjecutarAsync(solicitud, ct);
        return DesdeResultado(resultado, datos => CreatedAtAction(nameof(Obtener), new { id = datos.Id },
            RespuestaApi<ProveedorDto>.Ok(datos, "Proveedor registrado correctamente.")));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarProveedorSolicitud solicitud, CancellationToken ct)
    {
        if (solicitud is null)
        {
            return BadRequest(RespuestaApi<ProveedorDto>.Error("Los datos del proveedor son obligatorios."));
        }

        var resultado = await _actualizar.EjecutarAsync(id, solicitud, ct);
        return DesdeResultado(resultado, datos => Ok(RespuestaApi<ProveedorDto>.Ok(datos, "Proveedor actualizado correctamente.")));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var resultado = await _eliminar.EjecutarAsync(id, ct);
        if (resultado.EsExitoso)
        {
            return Ok(RespuestaApi<object>.Ok(new { id }, "Proveedor eliminado correctamente."));
        }

        return DesdeFallo<object>(resultado.Error!);
    }

    private IActionResult DesdeResultado<T>(Result<T> resultado, Func<T, IActionResult> exito)
    {
        if (resultado.EsExitoso)
        {
            return exito(resultado.Valor!);
        }

        return DesdeFallo<T>(resultado.Error!);
    }

    private IActionResult DesdeFallo<T>(ErrorResultado error)
    {
        var respuesta = RespuestaApi<T>.Error(
            error.Mensaje,
            new List<string> { error.Mensaje },
            error.ErroresCampo.ToDictionary(k => k.Key, v => v.Value));

        return error.Tipo switch
        {
            TipoError.Validacion => BadRequest(respuesta),
            TipoError.NoEncontrado => NotFound(respuesta),
            TipoError.Conflicto => Conflict(respuesta),
            _ => BadRequest(respuesta)
        };
    }
}
