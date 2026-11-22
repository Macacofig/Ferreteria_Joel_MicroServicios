using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Servicios;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>
/// API REST del catálogo de productos. El controlador solo traduce HTTP ↔
/// casos de uso; las reglas de negocio viven en <see cref="ServicioProductos"/>.
/// </summary>
[ApiController]
[Route("api/productos")]
[Authorize]
public sealed class ProductosController : ControllerBase
{
    private readonly ServicioProductos _servicio;

    public ProductosController(ServicioProductos servicio)
    {
        _servicio = servicio;
    }

    // ===================== Consultas =====================

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? ordenarPor,
        [FromQuery] string? direccion,
        [FromQuery] bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<Dominio.Entidades.Producto>> resultado =
            await _servicio.ListarAsync(
                ordenarPor, direccion, incluirInactivos, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(resultado.ValorRequerido
            .Select(MapeosProducto.AResponse)
            .ToList());
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias(CancellationToken ct = default)
    {
        Result<IReadOnlyList<Dominio.Entidades.Categoria>> resultado =
            await _servicio.ListarCategoriasAsync(ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(resultado.ValorRequerido
            .Select(MapeosProducto.AResponse)
            .ToList());
    }

    [HttpGet("marcas")]
    public async Task<IActionResult> ListarMarcas(CancellationToken ct = default)
    {
        Result<IReadOnlyList<Dominio.Entidades.Marca>> resultado =
            await _servicio.ListarMarcasAsync(ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(resultado.ValorRequerido
            .Select(MapeosProducto.AResponse)
            .ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct = default)
    {
        Result<Dominio.Entidades.Producto> resultado =
            await _servicio.ObtenerPorIdAsync(id, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(MapeosProducto.AResponse(resultado.ValorRequerido));
    }

    [HttpGet("{id:int}/historico")]
    public async Task<IActionResult> ListarHistorico(
        int id,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<Dominio.Entidades.HistoricoProducto>> resultado =
            await _servicio.ListarHistoricoAsync(id, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(resultado.ValorRequerido
            .Select(MapeosProducto.AResponse)
            .ToList());
    }

    // ===================== Mutaciones =====================

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

        int idProducto = resultado.ValorRequerido;

        return CreatedAtAction(
            nameof(Obtener),
            new { id = idProducto },
            new { idProducto, mensaje = "Producto creado correctamente." });
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

        return Ok(new { mensaje = "Producto actualizado correctamente." });
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

        return Ok(new { mensaje = "Producto dado de baja correctamente." });
    }

    // ===================== Traducción Result → HTTP =====================

    private IActionResult ResponderFallo(Result resultado)
    {
        string error = resultado.Error;

        if (error.StartsWith(
                ServicioProductos.PrefijoNoEncontrado,
                StringComparison.Ordinal))
        {
            string mensaje = error[
                ServicioProductos.PrefijoNoEncontrado.Length..];

            return NotFound(new { mensaje });
        }

        if (error.StartsWith(
                ServicioProductos.PrefijoConflicto,
                StringComparison.Ordinal))
        {
            string mensaje = error[
                ServicioProductos.PrefijoConflicto.Length..];

            return Conflict(new { mensaje });
        }

        return BadRequest(new { mensaje = error });
    }
}
