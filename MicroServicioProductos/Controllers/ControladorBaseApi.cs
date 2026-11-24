using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>
/// Traducción común de <see cref="Result"/> a HTTP. La comparten los cuatro
/// controladores para no repetir la lógica de 404/409/400 ni la forma del
/// cuerpo de las respuestas correctas.
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
    /// Devuelve el cuerpo directo que espera el front (listas y DTOs, sin
    /// envoltorio). El mensaje del Result no se envía como cabecera: los
    /// caracteres no ASCII, como «categoría», hacen que Kestrel rechace la
    /// respuesta con un 500. Las mutaciones ya incluyen «mensaje» en el cuerpo.
    /// </summary>
    protected IActionResult OkDatos(object? datos) => Ok(datos);

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
