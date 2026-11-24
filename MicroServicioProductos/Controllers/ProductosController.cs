using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>Adaptador HTTP de los casos de uso de productos.</summary>
[ApiController]
[Route("api/productos")]
[Authorize]
public sealed class ProductosController : ControladorBaseApi
{
    private readonly IServicioProductos _servicio;

    public ProductosController(IServicioProductos servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? ordenarPor,
        [FromQuery] string? direccion,
        [FromQuery] bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<Producto>> resultado =
            await _servicio.ListarAsync(
                ordenarPor, direccion, incluirInactivos, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return OkDatos(resultado.Valor!
            .Select(MapeosProducto.AResponse)
            .ToList());
    }

    [HttpGet("siguiente-codigo")]
    public async Task<IActionResult> ObtenerSiguienteCodigo(
        CancellationToken ct = default)
    {
        Result<string> resultado =
            await _servicio.ObtenerSiguienteCodigoAsync(ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return OkDatos(
            new { codigo = resultado.Valor! });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(
        int id,
        CancellationToken ct = default)
    {
        Result<Producto> resultado =
            await _servicio.ObtenerPorIdAsync(id, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return OkDatos(
            MapeosProducto.AResponse(resultado.Valor!));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearProductoRequest solicitud,
        CancellationToken ct = default)
    {
        Result<int> resultado = await _servicio.CrearAsync(solicitud, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        int idProducto = resultado.Valor!;

        return CreatedAtAction(
            nameof(Obtener),
            new { id = idProducto },
            new { idProducto, mensaje = resultado.Mensaje });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] ActualizarProductoRequest solicitud,
        CancellationToken ct = default)
    {
        Result resultado = await _servicio.ActualizarAsync(id, solicitud, ct);

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
        Result resultado = await _servicio.DarDeBajaAsync(id, ct);

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
        Result resultado = await _servicio.ReactivarAsync(id, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = resultado.Mensaje });
    }
}
