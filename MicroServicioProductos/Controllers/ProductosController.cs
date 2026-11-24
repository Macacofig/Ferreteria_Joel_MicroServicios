using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>
/// API REST del catálogo de productos. El controlador solo traduce HTTP ↔
/// casos de uso; las reglas de negocio viven en los servicios de aplicación
/// segregados.
/// </summary>
[ApiController]
[Route("api/productos")]
[Authorize]
public sealed class ProductosController : ControllerBase
{
    private readonly IServicioProductos _productos;
    private readonly IServicioCategorias _categorias;
    private readonly IServicioMarcas _marcas;

    public ProductosController(
        IServicioProductos productos,
        IServicioCategorias categorias,
        IServicioMarcas marcas)
    {
        _productos = productos;
        _categorias = categorias;
        _marcas = marcas;
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
            await _productos.ListarAsync(
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
            await _categorias.ListarCategoriasAsync(incluirInactivos, ct);

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
            await _categorias.ObtenerSiguienteCodigoCategoriaAsync(ct);

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
            await _marcas.ListarMarcasAsync(incluirInactivos, ct);

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
            await _categorias.ObtenerCategoriaPorIdAsync(ConvertirIdCatalogo(id), ct);

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
            await _marcas.ObtenerMarcaPorIdAsync(ConvertirIdCatalogo(id), ct);

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
            await _productos.ObtenerSiguienteCodigoAsync(ct);

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
            await _productos.ObtenerPorIdAsync(id, ct);

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
            await _productos.ListarHistoricoAsync(id, ct);

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
        Result<int> resultado = await _productos.CrearAsync(solicitud, ct);

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
        Result resultado = await _productos.ActualizarAsync(id, solicitud, ct);

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
        Result resultado = await _productos.DarDeBajaAsync(id, ct);

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
        Result resultado = await _productos.ReactivarAsync(id, ct);

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
            await _categorias.CrearCategoriaAsync(solicitud, ct);

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
        Result resultado = await _categorias.ActualizarCategoriaAsync(
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
            await _categorias.DarDeBajaCategoriaAsync(ConvertirIdCatalogo(id), ct);

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
            await _categorias.ReactivarCategoriaAsync(ConvertirIdCatalogo(id), ct);

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
        Result<short> resultado = await _marcas.CrearMarcaAsync(solicitud, ct);

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
        Result resultado = await _marcas.ActualizarMarcaAsync(
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
            await _marcas.DarDeBajaMarcaAsync(ConvertirIdCatalogo(id), ct);

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
            await _marcas.ReactivarMarcaAsync(ConvertirIdCatalogo(id), ct);

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
                PrefijosResultado.NoEncontrado,
                StringComparison.Ordinal))
        {
            string mensaje = error[
                PrefijosResultado.NoEncontrado.Length..];

            return NotFound(new { mensaje });
        }

        if (error.StartsWith(
                PrefijosResultado.Conflicto,
                StringComparison.Ordinal))
        {
            string mensaje = error[
                PrefijosResultado.Conflicto.Length..];

            return Conflict(new { mensaje });
        }

        return BadRequest(new { mensaje = error });
    }
}
