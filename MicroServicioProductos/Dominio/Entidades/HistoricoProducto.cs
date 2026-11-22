namespace MicroServicioProductos.Dominio.Entidades;

/// <summary>Registro de auditoría de los cambios sufridos por un producto.</summary>
public sealed class HistoricoProducto
{
    public int IdHistorico { get; set; }
    public int IdProducto { get; set; }
    public string Accion { get; set; } = string.Empty;
    public decimal? PrecioAnterior { get; set; }
    public decimal? PrecioNuevo { get; set; }
    public int? StockAnterior { get; set; }
    public int? StockNuevo { get; set; }
    public string? Detalle { get; set; }
    public DateTime FechaCambio { get; set; }
    public int? IdEmpleadoResponsable { get; set; }
}
