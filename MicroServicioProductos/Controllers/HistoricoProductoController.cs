using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Aplicacion.Puertos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroServicioProductos.Controllers;

/// <summary>Adaptador HTTP de la consulta del historial de un producto.</summary>
[ApiController]
[Route("api/productos/{id:int}/historico")]
[Authorize]
public sealed class HistoricoProductoController : ControladorBaseApi
{
    private readonly IServicioHistoricoProducto _servicio;

    public HistoricoProductoController(IServicioHistoricoProducto servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        int id,
        CancellationToken ct = default)
    {
        Result<IReadOnlyList<HistoricoProducto>> resultado =
            await _servicio.ListarPorProductoAsync(id, ct);

        if (resultado.EsFallo)
        {
            return ResponderFallo(resultado);
        }

        return OkConMensaje(resultado.Valor!
            .Select(MapeosHistoricoProducto.AResponse)
            .ToList(), resultado.Mensaje);
    }
}
