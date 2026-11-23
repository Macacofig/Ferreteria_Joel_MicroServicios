using MicroServicioClientes.Dominio.Comunes;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioClientes.Controllers;

/// <summary>
/// Patrón Adapter (adaptador de entrada): convierte el <see cref="Resultado"/>
/// del núcleo en una respuesta HTTP, de modo que los controladores no repiten
/// la traducción de errores a códigos 400, 404 o 409.
/// </summary>
public static class AdaptadorResultadoHttp
{
    public static IActionResult Adaptar<T>(this ControllerBase controlador, Resultado<T> resultado) =>
        resultado.Exitoso
            ? controlador.Ok(new RespuestaApi<T>(true, resultado.Mensaje, resultado.Valor, null))
            : Error(controlador, resultado);

    public static IActionResult AdaptarCreado<T>(this ControllerBase controlador, Resultado<T> resultado) =>
        resultado.Exitoso
            ? controlador.StatusCode(
                StatusCodes.Status201Created,
                new RespuestaApi<T>(true, resultado.Mensaje, resultado.Valor, null))
            : Error(controlador, resultado);

    public static IActionResult AdaptarSinDatos(this ControllerBase controlador, Resultado resultado) =>
        resultado.Exitoso
            ? controlador.Ok(new RespuestaApi<object>(true, resultado.Mensaje, null, null))
            : Error(controlador, resultado);

    private static IActionResult Error(ControllerBase controlador, Resultado resultado)
    {
        var cuerpo = new RespuestaApi<object>(
            false,
            resultado.Mensaje,
            null,
            resultado.ErroresCampo.Count > 0 ? resultado.ErroresCampo : null);

        return resultado.TipoError switch
        {
            TipoError.NoEncontrado => controlador.NotFound(cuerpo),
            TipoError.Conflicto => controlador.Conflict(cuerpo),
            _ => controlador.BadRequest(cuerpo)
        };
    }
}