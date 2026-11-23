namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Target del patrón Adapter para el microservicio de productos. Las Razor
    /// Pages dependen de este contrato y nunca conocen la URL del ApiGateway ni
    /// el cliente HTTP concreto. El JWT se toma de la sesión dentro del
    /// adaptador, de modo que las páginas no lo manipulan.
    /// </summary>
    public interface IServicioProductos
    {
        Task<ResultadoApi<List<ProductoApi>>> ListarAsync(
            string? ordenarPor,
            string? direccion,
            bool incluirInactivos = false,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<List<CategoriaApi>>> ListarCategoriasAsync(
            CancellationToken cancelacion = default);

        Task<ResultadoApi<List<MarcaApi>>> ListarMarcasAsync(
            CancellationToken cancelacion = default);

        Task<ResultadoApi<ProductoApi>> ObtenerAsync(
            int idProducto,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<SiguienteCodigoApi>> ObtenerSiguienteCodigoAsync(
            CancellationToken cancelacion = default);

        Task<ResultadoApi<CrearProductoRespuestaApi>> CrearAsync(
            GuardarProductoApi solicitud,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<MensajeApi>> ActualizarAsync(
            int idProducto,
            GuardarProductoApi solicitud,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<MensajeApi>> DarDeBajaAsync(
            int idProducto,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<List<HistoricoProductoApi>>> ListarHistoricoAsync(
            int idProducto,
            CancellationToken cancelacion = default);

        // ===================== Categorías =====================

        Task<ResultadoApi<SiguienteCodigoApi>> ObtenerSiguienteCodigoCategoriaAsync(
            CancellationToken cancelacion = default);

        Task<ResultadoApi<CategoriaApi>> ObtenerCategoriaAsync(
            short idCategoria,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<CrearCategoriaRespuestaApi>> CrearCategoriaAsync(
            GuardarCategoriaApi solicitud,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<MensajeApi>> ActualizarCategoriaAsync(
            short idCategoria,
            GuardarCategoriaApi solicitud,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<MensajeApi>> DarDeBajaCategoriaAsync(
            short idCategoria,
            CancellationToken cancelacion = default);

        // ===================== Marcas =====================

        Task<ResultadoApi<MarcaApi>> ObtenerMarcaAsync(
            short idMarca,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<CrearMarcaRespuestaApi>> CrearMarcaAsync(
            GuardarMarcaApi solicitud,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<MensajeApi>> ActualizarMarcaAsync(
            short idMarca,
            GuardarMarcaApi solicitud,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<MensajeApi>> DarDeBajaMarcaAsync(
            short idMarca,
            CancellationToken cancelacion = default);
    }
}
