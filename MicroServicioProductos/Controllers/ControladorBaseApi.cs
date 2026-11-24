using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>
/// Traducción común de <see cref="Result"/> a HTTP. La comparten los cuatro
/// controladores para no repetir la lógica de 404/409/400 ni la cabecera de
/// mensaje de éxito.
/// </summary>
public abstract class ControladorBaseApi : ControllerBase
{
    /// <summary>
    /// Los identificadores de categorías y marcas son SMALLINT; un valor fuera
    /// de rango se traduce a 0 para que el caso de uso responda 404.
    /// </summary>
    protected static short ConvertirIdCatalogo(int id) =>
        id is >= 1 and <= short.MaxValue ? (short)id : (short)0;

    /// <summary>
    /// Publica el mensaje de éxito del Result sin alterar el cuerpo: los
    /// clientes del front esperan listas y DTOs directos.
    /// </summary>
    protected IActionResult OkConMensaje(object? datos, string mensaje)
    {
        if (!string.IsNullOrWhiteSpace(mensaje))
        {
            Response.Headers["X-Mensaje"] = mensaje;
        }

        return Ok(datos);
    }

    protected IActionResult ResponderFallo(Result resultado)
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
