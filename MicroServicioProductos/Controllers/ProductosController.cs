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
    public async Task<IActionResult> ListarCategorias(
        [FromQuery] bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<Dominio.Entidades.Categoria>> resultado =
            await _servicio.ListarCategoriasAsync(incluirInactivos, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(resultado.ValorRequerido
            .Select(MapeosProducto.AResponse)
            .ToList());
    }

    [HttpGet("categorias/siguiente-codigo")]
    public async Task<IActionResult> ObtenerSiguienteCodigoCategoria(
        CancellationToken ct = default)
    {
        Result<string> resultado =
            await _servicio.ObtenerSiguienteCodigoCategoriaAsync(ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { codigo = resultado.ValorRequerido });
    }

    [HttpGet("marcas")]
    public async Task<IActionResult> ListarMarcas(
        [FromQuery] bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<Dominio.Entidades.Marca>> resultado =
            await _servicio.ListarMarcasAsync(incluirInactivos, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(resultado.ValorRequerido
            .Select(MapeosProducto.AResponse)
            .ToList());
    }

    [HttpGet("categorias/{id:int}")]
    public async Task<IActionResult> ObtenerCategoria(
        int id,
        CancellationToken ct = default)
    {
        Result<Dominio.Entidades.Categoria> resultado =
            await _servicio.ObtenerCategoriaPorIdAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(MapeosProducto.AResponse(resultado.ValorRequerido));
    }

    [HttpGet("marcas/{id:int}")]
    public async Task<IActionResult> ObtenerMarca(
        int id,
        CancellationToken ct = default)
    {
        Result<Dominio.Entidades.Marca> resultado =
            await _servicio.ObtenerMarcaPorIdAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(MapeosProducto.AResponse(resultado.ValorRequerido));
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

        return Ok(new { codigo = resultado.ValorRequerido });
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

        return Ok(new { mensaje = "Producto reactivado correctamente." });
    }

    // ===================== Categorías =====================

    [HttpPost("categorias")]
    public async Task<IActionResult> CrearCategoria(
        [FromBody] CrearCategoriaRequest solicitud,
        CancellationToken ct = default)
    {
        Result<short> resultado =
            await _servicio.CrearCategoriaAsync(solicitud, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        short idCategoria = resultado.ValorRequerido;

        return CreatedAtAction(
            nameof(ObtenerCategoria),
            new { id = idCategoria },
            new
            {
                idCategoria,
                mensaje = "Categoría creada correctamente."
            });
    }

    [HttpPut("categorias/{id:int}")]
    public async Task<IActionResult> ActualizarCategoria(
        int id,
        [FromBody] ActualizarCategoriaRequest solicitud,
        CancellationToken ct = default)
    {
        Result resultado = await _servicio.ActualizarCategoriaAsync(
            ConvertirIdCatalogo(id), solicitud, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = "Categoría actualizada correctamente." });
    }

    [HttpDelete("categorias/{id:int}")]
    public async Task<IActionResult> DarDeBajaCategoria(
        int id,
        CancellationToken ct = default)
    {
        Result resultado =
            await _servicio.DarDeBajaCategoriaAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = "Categoría dada de baja correctamente." });
    }

    [HttpPatch("categorias/{id:int}/reactivar")]
    public async Task<IActionResult> ReactivarCategoria(
        int id,
        CancellationToken ct = default)
    {
        Result resultado =
            await _servicio.ReactivarCategoriaAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = "Categoría reactivada correctamente." });
    }

    // ===================== Marcas =====================

    [HttpPost("marcas")]
    public async Task<IActionResult> CrearMarca(
        [FromBody] CrearMarcaRequest solicitud,
        CancellationToken ct = default)
    {
        Result<short> resultado = await _servicio.CrearMarcaAsync(solicitud, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        short idMarca = resultado.ValorRequerido;

        return CreatedAtAction(
            nameof(ObtenerMarca),
            new { id = idMarca },
            new
            {
                idMarca,
                mensaje = "Marca creada correctamente."
            });
    }

    [HttpPut("marcas/{id:int}")]
    public async Task<IActionResult> ActualizarMarca(
        int id,
        [FromBody] ActualizarMarcaRequest solicitud,
        CancellationToken ct = default)
    {
        Result resultado = await _servicio.ActualizarMarcaAsync(
            ConvertirIdCatalogo(id), solicitud, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = "Marca actualizada correctamente." });
    }

    [HttpDelete("marcas/{id:int}")]
    public async Task<IActionResult> DarDeBajaMarca(
        int id,
        CancellationToken ct = default)
    {
        Result resultado =
            await _servicio.DarDeBajaMarcaAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = "Marca dada de baja correctamente." });
    }

    [HttpPatch("marcas/{id:int}/reactivar")]
    public async Task<IActionResult> ReactivarMarca(
        int id,
        CancellationToken ct = default)
    {
        Result resultado =
            await _servicio.ReactivarMarcaAsync(ConvertirIdCatalogo(id), ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return Ok(new { mensaje = "Marca reactivada correctamente." });
    }

    // ===================== Traducción Result → HTTP =====================

    /// <summary>
    /// Los identificadores de categorías y marcas son SMALLINT; un valor fuera
    /// de rango se traduce a 0 para que el caso de uso responda 404.
    /// </summary>
    private static short ConvertirIdCatalogo(int id) =>
        id is >= 1 and <= short.MaxValue ? (short)id : (short)0;

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
