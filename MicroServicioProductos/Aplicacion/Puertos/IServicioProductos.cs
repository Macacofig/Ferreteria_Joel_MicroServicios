using MicroServicioProductos.Aplicacion.Modelos;
using MicroServicioProductos.Dominio.Entidades;
using MicroServicioProductos.Dominio.Resultados;

namespace MicroServicioProductos.Aplicacion.Puertos;

/// <summary>
/// Puerto de entrada del microservicio de productos: casos de uso que el
/// adaptador HTTP (ProductosController) consume. La capa de presentación no
/// conoce la clase concreta ServicioProductos ni sus dependencias.
/// </summary>
public interface IServicioProductos
{
    // ===================== Consultas =====================

    Task<Result<IReadOnlyList<Producto>>> ListarAsync(
        string? ordenarPor,
        string? direccion,
        bool incluirInactivos,
        CancellationToken ct = default);

    Task<Result<Producto>> ObtenerPorIdAsync(
        int idProducto,
        CancellationToken ct = default);

    Task<Result<string>> ObtenerSiguienteCodigoAsync(
        CancellationToken ct = default);

    Task<Result<string>> ObtenerSiguienteCodigoCategoriaAsync(
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<Categoria>>> ListarCategoriasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<Marca>>> ListarMarcasAsync(
        bool incluirInactivos = false,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<HistoricoProducto>>> ListarHistoricoAsync(
        int idProducto,
        CancellationToken ct = default);

    // ===================== Mutaciones =====================

    Task<Result<int>> CrearAsync(
        CrearProductoRequest solicitud,
        CancellationToken ct = default);

    Task<Result> ActualizarAsync(
        int idProducto,
        ActualizarProductoRequest solicitud,
        CancellationToken ct = default);

    Task<Result> DarDeBajaAsync(
        int idProducto,
        CancellationToken ct = default);

    Task<Result> ReactivarAsync(
        int idProducto,
        CancellationToken ct = default);

    // ===================== Categorías =====================

    Task<Result<Categoria>> ObtenerCategoriaPorIdAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<Result<short>> CrearCategoriaAsync(
        CrearCategoriaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> ActualizarCategoriaAsync(
        short idCategoria,
        ActualizarCategoriaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> DarDeBajaCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default);

    Task<Result> ReactivarCategoriaAsync(
        short idCategoria,
        CancellationToken ct = default);

    // ===================== Marcas =====================

    Task<Result<Marca>> ObtenerMarcaPorIdAsync(
        short idMarca,
        CancellationToken ct = default);

    Task<Result<short>> CrearMarcaAsync(
        CrearMarcaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> ActualizarMarcaAsync(
        short idMarca,
        ActualizarMarcaRequest solicitud,
        CancellationToken ct = default);

    Task<Result> DarDeBajaMarcaAsync(
        short idMarca,
        CancellationToken ct = default);

    Task<Result> ReactivarMarcaAsync(
        short idMarca,
        CancellationToken ct = default);
}

/// <summary>
/// Prefijos que permiten al adaptador HTTP distinguir 404 y 409 del resto de
/// validaciones sin inspeccionar excepciones. Viven en Aplicación para que el
/// controlador no tenga que referenciar la implementación concreta.
/// </summary>
public static class PrefijosResultado
{
    public const string NoEncontrado = "[NO_ENCONTRADO]";
    public const string Conflicto = "[CONFLICTO]";
}
