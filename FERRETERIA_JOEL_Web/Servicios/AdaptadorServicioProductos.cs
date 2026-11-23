using Microsoft.AspNetCore.Http;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Adapter de objetos del microservicio de productos. Envuelve a
    /// <see cref="ProductosApiCliente"/> (el <em>adaptee</em>) y ofrece a las
    /// Razor Pages el contrato del front <see cref="IServicioProductos"/>.
    /// Lee el JWT de la misma sesión que usa Usuarios y lo adjunta en cada
    /// petición como <c>Authorization: Bearer &lt;token&gt;</c>.
    /// </summary>
    public class AdaptadorServicioProductos : IServicioProductos
    {
        private readonly ProductosApiCliente _adaptee;
        private readonly IHttpContextAccessor _accesor;

        public AdaptadorServicioProductos(
            ProductosApiCliente adaptee,
            IHttpContextAccessor accesor)
        {
            _adaptee = adaptee;
            _accesor = accesor;
        }

        /// <summary>JWT del usuario autenticado, guardado en sesión por Login.</summary>
        private string? ObtenerToken() =>
            _accesor.HttpContext?.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

        public Task<ResultadoApi<List<ProductoApi>>> ListarAsync(
            string? ordenarPor,
            string? direccion,
            bool incluirInactivos = false,
            CancellationToken cancelacion = default) =>
            _adaptee.ListarAsync(
                ObtenerToken(),
                ordenarPor,
                direccion,
                incluirInactivos,
                cancelacion);

        public Task<ResultadoApi<List<CategoriaApi>>> ListarCategoriasAsync(
            CancellationToken cancelacion = default) =>
            _adaptee.ListarCategoriasAsync(ObtenerToken(), cancelacion);

        public Task<ResultadoApi<List<MarcaApi>>> ListarMarcasAsync(
            CancellationToken cancelacion = default) =>
            _adaptee.ListarMarcasAsync(ObtenerToken(), cancelacion);

        public Task<ResultadoApi<ProductoApi>> ObtenerAsync(
            int idProducto,
            CancellationToken cancelacion = default) =>
            _adaptee.ObtenerAsync(ObtenerToken(), idProducto, cancelacion);

        public Task<ResultadoApi<SiguienteCodigoApi>> ObtenerSiguienteCodigoAsync(
            CancellationToken cancelacion = default) =>
            _adaptee.ObtenerSiguienteCodigoAsync(
                ObtenerToken(), cancelacion);

        public Task<ResultadoApi<CrearProductoRespuestaApi>> CrearAsync(
            GuardarProductoApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.CrearAsync(ObtenerToken(), solicitud, cancelacion);

        public Task<ResultadoApi<MensajeApi>> ActualizarAsync(
            int idProducto,
            GuardarProductoApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.ActualizarAsync(
                ObtenerToken(), idProducto, solicitud, cancelacion);

        public Task<ResultadoApi<MensajeApi>> DarDeBajaAsync(
            int idProducto,
            CancellationToken cancelacion = default) =>
            _adaptee.DarDeBajaAsync(ObtenerToken(), idProducto, cancelacion);

        public Task<ResultadoApi<List<HistoricoProductoApi>>> ListarHistoricoAsync(
            int idProducto,
            CancellationToken cancelacion = default) =>
            _adaptee.ListarHistoricoAsync(ObtenerToken(), idProducto, cancelacion);

        public Task<ResultadoApi<SiguienteCodigoApi>> ObtenerSiguienteCodigoCategoriaAsync(
            CancellationToken cancelacion = default) =>
            _adaptee.ObtenerSiguienteCodigoCategoriaAsync(
                ObtenerToken(), cancelacion);

        public Task<ResultadoApi<CategoriaApi>> ObtenerCategoriaAsync(
            short idCategoria,
            CancellationToken cancelacion = default) =>
            _adaptee.ObtenerCategoriaAsync(ObtenerToken(), idCategoria, cancelacion);

        public Task<ResultadoApi<CrearCategoriaRespuestaApi>> CrearCategoriaAsync(
            GuardarCategoriaApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.CrearCategoriaAsync(ObtenerToken(), solicitud, cancelacion);

        public Task<ResultadoApi<MensajeApi>> ActualizarCategoriaAsync(
            short idCategoria,
            GuardarCategoriaApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.ActualizarCategoriaAsync(
                ObtenerToken(), idCategoria, solicitud, cancelacion);

        public Task<ResultadoApi<MensajeApi>> DarDeBajaCategoriaAsync(
            short idCategoria,
            CancellationToken cancelacion = default) =>
            _adaptee.DarDeBajaCategoriaAsync(
                ObtenerToken(), idCategoria, cancelacion);

        public Task<ResultadoApi<MarcaApi>> ObtenerMarcaAsync(
            short idMarca,
            CancellationToken cancelacion = default) =>
            _adaptee.ObtenerMarcaAsync(ObtenerToken(), idMarca, cancelacion);

        public Task<ResultadoApi<CrearMarcaRespuestaApi>> CrearMarcaAsync(
            GuardarMarcaApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.CrearMarcaAsync(ObtenerToken(), solicitud, cancelacion);

        public Task<ResultadoApi<MensajeApi>> ActualizarMarcaAsync(
            short idMarca,
            GuardarMarcaApi solicitud,
            CancellationToken cancelacion = default) =>
            _adaptee.ActualizarMarcaAsync(
                ObtenerToken(), idMarca, solicitud, cancelacion);

        public Task<ResultadoApi<MensajeApi>> DarDeBajaMarcaAsync(
            short idMarca,
            CancellationToken cancelacion = default) =>
            _adaptee.DarDeBajaMarcaAsync(ObtenerToken(), idMarca, cancelacion);
    }
}
