using MicroServicioProductos.Dominio.Entidades;

namespace MicroServicioProductos.Aplicacion.Modelos;

public sealed record HistoricoProductoResponse(
    int IdHistorico,
    int IdProducto,
    string Accion,
    decimal? PrecioAnterior,
    decimal? PrecioNuevo,
    int? StockAnterior,
    int? StockNuevo,
    string? Detalle,
    DateTime FechaCambio,
    int? IdEmpleadoResponsable);

/// <summary>
/// Traducción entre <see cref="HistoricoProducto"/> y su DTO de respuesta.
/// </summary>
public static class MapeosHistoricoProducto
{
    public static HistoricoProductoResponse AResponse(HistoricoProducto registro) => new(
        registro.IdHistorico,
        registro.IdProducto,
        registro.Accion,
        registro.PrecioAnterior,
        registro.PrecioNuevo,
        registro.StockAnterior,
        registro.StockNuevo,
        registro.Detalle,
        registro.FechaCambio,
        registro.IdEmpleadoResponsable);
}
