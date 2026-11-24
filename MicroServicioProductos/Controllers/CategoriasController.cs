using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>Adaptador HTTP de los casos de uso de categorías.</summary>
[ApiController]
[Route("api/productos/categorias")]
[Authorize]
public sealed class CategoriasController : ControladorBaseApi
{
    private readonly IServicioCategorias _servicio;

    public CategoriasController(IServicioCategorias servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<Categoria>> resultado =
            await _servicio.ListarAsync(incluirInactivos, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return OkDatos(resultado.Valor!
            .Select(MapeosCategoria.AResponse)
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
        Result<Categoria> resultado =
            await _servicio.ObtenerPorIdAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return OkDatos(
            MapeosCategoria.AResponse(resultado.Valor!));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearCategoriaRequest solicitud,
        CancellationToken ct = default)
    {
        Result<short> resultado = await _servicio.CrearAsync(solicitud, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        short idCategoria = resultado.Valor!;

        return CreatedAtAction(
            nameof(Obtener),
            new { id = idCategoria },
            new { idCategoria, mensaje = resultado.Mensaje });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] ActualizarCategoriaRequest solicitud,
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
