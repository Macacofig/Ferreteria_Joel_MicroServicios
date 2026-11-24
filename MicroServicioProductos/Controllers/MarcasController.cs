using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>Adaptador HTTP de los casos de uso de marcas.</summary>
[ApiController]
[Route("api/productos/marcas")]
[Authorize]
public sealed class MarcasController : ControladorBaseApi
{
    private readonly IServicioMarcas _servicio;

    public MarcasController(IServicioMarcas servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<Marca>> resultado =
            await _servicio.ListarAsync(incluirInactivos, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return OkDatos(resultado.Valor!
            .Select(MapeosMarca.AResponse)
            .ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(
        int id,
        CancellationToken ct = default)
    {
        Result<Marca> resultado =
            await _servicio.ObtenerPorIdAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return OkDatos(
            MapeosMarca.AResponse(resultado.Valor!));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearMarcaRequest solicitud,
        CancellationToken ct = default)
    {
        Result<short> resultado = await _servicio.CrearAsync(solicitud, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        short idMarca = resultado.Valor!;

        return CreatedAtAction(
            nameof(Obtener),
            new { id = idMarca },
            new { idMarca, mensaje = resultado.Mensaje });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] ActualizarMarcaRequest solicitud,
        CancellationToken ct = default)
    {
        Result resultado = await _servicio.ActualizarAsync(
            ConvertirIdCatalogo(id), solicitud, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = resultado.Mensaje });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DarDeBaja(
        int id,
        CancellationToken ct = default)
    {
        Result resultado =
            await _servicio.DarDeBajaAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = resultado.Mensaje });
    }

    [HttpPatch("{id:int}/reactivar")]
    public async Task<IActionResult> Reactivar(
        int id,
        CancellationToken ct = default)
    {
        Result resultado =
            await _servicio.ReactivarAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = resultado.Mensaje });
    }
}
