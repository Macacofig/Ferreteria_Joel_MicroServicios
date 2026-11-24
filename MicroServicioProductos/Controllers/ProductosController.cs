using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>
/// Adaptador de entrada HTTP del catálogo de productos. El controlador solo
/// traduce HTTP ↔ casos de uso del puerto <see cref="IServicioProductos"/>;
/// las reglas de negocio viven en la capa de Aplicación.
/// </summary>
[ApiController]
[Route("api/productos")]
[Authorize]
public sealed class ProductosController : ControllerBase
{
    private readonly IServicioProductos _servicio;

    public ProductosController(IServicioProductos servicio)
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

        return OkConMensaje(resultado.Valor!
            .Select(MapeosProducto.AResponse)
            .ToList(), resultado.Mensaje);
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

        return OkConMensaje(resultado.Valor!
            .Select(MapeosProducto.AResponse)
            .ToList(), resultado.Mensaje);
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

        return OkConMensaje(
            new { codigo = resultado.Valor! }, resultado.Mensaje);
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

        return OkConMensaje(resultado.Valor!
            .Select(MapeosProducto.AResponse)
            .ToList(), resultado.Mensaje);
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

        return OkConMensaje(
            MapeosProducto.AResponse(resultado.Valor!), resultado.Mensaje);
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

        return OkConMensaje(
            MapeosProducto.AResponse(resultado.Valor!), resultado.Mensaje);
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

        return OkConMensaje(
            new { codigo = resultado.Valor! }, resultado.Mensaje);
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

        return OkConMensaje(
            MapeosProducto.AResponse(resultado.Valor!), resultado.Mensaje);
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

        return OkConMensaje(resultado.Valor!
            .Select(MapeosProducto.AResponse)
            .ToList(), resultado.Mensaje);
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

        short idCategoria = resultado.Valor!;

        return CreatedAtAction(
            nameof(ObtenerCategoria),
            new { id = idCategoria },
            new
            {
                idCategoria,
                mensaje = resultado.Mensaje
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

        return Ok(new { mensaje = resultado.Mensaje });
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

        return Ok(new { mensaje = resultado.Mensaje });
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

        return Ok(new { mensaje = resultado.Mensaje });
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

        short idMarca = resultado.Valor!;

        return CreatedAtAction(
            nameof(ObtenerMarca),
            new { id = idMarca },
            new
            {
                idMarca,
                mensaje = resultado.Mensaje
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

        return Ok(new { mensaje = resultado.Mensaje });
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

        return Ok(new { mensaje = resultado.Mensaje });
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

        return Ok(new { mensaje = resultado.Mensaje });
    }

    // ===================== Traducción Result → HTTP =====================

    /// <summary>
    /// Los identificadores de categorías y marcas son SMALLINT; un valor fuera
    /// de rango se traduce a 0 para que el caso de uso responda 404.
    /// </summary>
    private static short ConvertirIdCatalogo(int id) =>
        id is >= 1 and <= short.MaxValue ? (short)id : (short)0;

    /// <summary>
    /// Publica el mensaje de éxito del Result en la respuesta HTTP sin alterar
    /// el cuerpo: los clientes del front esperan listas y DTOs directos.
    /// </summary>
    private IActionResult OkConMensaje(object? datos, string mensaje)
    {
        if (!string.IsNullOrWhiteSpace(mensaje))
        {
            Response.Headers["X-Mensaje"] = mensaje;
        }

        return Ok(datos);
    }

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
