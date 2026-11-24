namespace MicroServicioProductos.Dominio.Puertos;

/// <summary>
/// Unidad de trabajo del dominio: agrupa varias escrituras en una sola
/// transacción, de modo que el producto y su historial se confirmen o se
/// reviertan juntos. La capa de Aplicación solo conoce estos tres métodos;
/// MySQL queda confinado a Infraestructura.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Inicia la transacción. No es necesario llamarlo para consultas.</summary>
    Task IniciarAsync(CancellationToken ct = default);

    /// <summary>Confirma todas las escrituras realizadas desde IniciarAsync.</summary>
    Task ConfirmarAsync(CancellationToken ct = default);

    /// <summary>
    /// Revierte todas las escrituras. Se ejecuta también ante fallos, por eso
    /// ignora la cancelación de la petición para no dejar la transacción abierta.
    /// </summary>
    Task RevertirAsync();
}
