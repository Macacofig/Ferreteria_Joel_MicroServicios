namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Principio de Segregación de Interfaces (ISP): las Razor Pages dependen
    /// solo de las operaciones que realmente usan, en lugar de un contrato
    /// único con 20 métodos. Todas son implementadas por
    /// <see cref="AdaptadorServicioProductos"/>.
    /// </summary>
    public interface IProductosConsulta
    {
        Task<ResultadoApi<List<ProductoApi>>> ListarAsync(
            string? ordenarPor,
            string? direccion,
            bool incluirInactivos = false,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<ProductoApi>> ObtenerAsync(
            int idProducto,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<SiguienteCodigoApi>> ObtenerSiguienteCodigoAsync(
            CancellationToken cancelacion = default);

        Task<ResultadoApi<List<HistoricoProductoApi>>> ListarHistoricoAsync(
            int idProducto,
            CancellationToken cancelacion = default);
    }

    /// <summary>Altas, cambios y bajas lógicas de productos.</summary>
    public interface IProductosComandos
    {
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

        Task<ResultadoApi<MensajeApi>> ReactivarAsync(
            int idProducto,
            CancellationToken cancelacion = default);
    }

    /// <summary>Consultas del catálogo auxiliar: categorías y marcas.</summary>
    public interface ICatalogoConsulta
    {
        Task<ResultadoApi<List<CategoriaApi>>> ListarCategoriasAsync(
            bool incluirInactivos = false,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<List<MarcaApi>>> ListarMarcasAsync(
            bool incluirInactivos = false,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<SiguienteCodigoApi>> ObtenerSiguienteCodigoCategoriaAsync(
            CancellationToken cancelacion = default);

        Task<ResultadoApi<CategoriaApi>> ObtenerCategoriaAsync(
            short idCategoria,
            CancellationToken cancelacion = default);

        Task<ResultadoApi<MarcaApi>> ObtenerMarcaAsync(
            short idMarca,
            CancellationToken cancelacion = default);
    }

    /// <summary>Altas, cambios y bajas lógicas de categorías y marcas.</summary>
    public interface ICatalogoComandos
    {
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

        Task<ResultadoApi<MensajeApi>> ReactivarCategoriaAsync(
            short idCategoria,
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

        Task<ResultadoApi<MensajeApi>> ReactivarMarcaAsync(
            short idMarca,
            CancellationToken cancelacion = default);
    }

    /// <summary>
    /// Target del patrón Adapter para el microservicio de productos. Se conserva
    /// como composición de las interfaces segregadas para código que necesite
    /// el contrato completo; las páginas usan directamente las interfaces
    /// pequeñas. El JWT se toma de la sesión dentro del adaptador.
    /// </summary>
    public interface IServicioProductos :
        IProductosConsulta,
        IProductosComandos,
        ICatalogoConsulta,
        ICatalogoComandos
    {
    }
}
