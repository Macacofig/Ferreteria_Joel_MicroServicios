using FERRETERIA__Joel.Dominio.Resultados;

namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Puerto de salida hacia el proveedor de identidad (Keycloak). El dominio
    /// y la aplicación dependen de esta abstracción; el adaptador concreto
    /// (HTTP contra Keycloak) vive en la capa de servicios.
    ///
    /// <para>
    /// La tabla <c>empleado</c> conserva los datos de negocio y el rol, pero
    /// las <em>credenciales</em> viven en Keycloak: es Keycloak quien emite los
    /// JWT y quien valida la contraseña. El microservicio solo valida la firma
    /// del token mediante JWKS.
    /// </para>
    /// </summary>
    public interface IProveedorIdentidad
    {
        /// <summary>
        /// Autentica al usuario contra Keycloak (flujo Resource Owner Password
        /// Credentials) y devuelve el token de acceso firmado por el realm.
        /// </summary>
        Task<Result<TokenIdentidad>> IniciarSesionAsync(
            string identificador,
            string contrasena,
            CancellationToken cancelacion = default);

        /// <summary>
        /// Crea el usuario en Keycloak con su contraseña y su rol de realm.
        /// Se usa cuando el empleado activa su cuenta.
        /// </summary>
        Task<Result> RegistrarUsuarioAsync(
            SolicitudIdentidad solicitud,
            CancellationToken cancelacion = default);

        /// <summary>Actualiza correo, nombre o rol del usuario en Keycloak.</summary>
        Task<Result> ActualizarUsuarioAsync(
            string identificadorActual,
            SolicitudIdentidad solicitud,
            CancellationToken cancelacion = default);

        /// <summary>Restablece la contraseña del usuario en Keycloak.</summary>
        Task<Result> EstablecerContrasenaAsync(
            string identificador,
            string contrasena,
            CancellationToken cancelacion = default);

        /// <summary>Habilita o deshabilita la cuenta en Keycloak.</summary>
        Task<Result> CambiarEstadoAsync(
            string identificador,
            bool habilitado,
            CancellationToken cancelacion = default);
    }

    /// <summary>Datos de un usuario que se registra o actualiza en el IdP.</summary>
    public sealed class SolicitudIdentidad
    {
        public string NombreUsuario { get; init; } = string.Empty;

        public string CorreoElectronico { get; init; } = string.Empty;

        public string NombreCompleto { get; init; } = string.Empty;

        /// <summary>Contraseña en claro; solo viaja hacia el IdP sobre TLS.</summary>
        public string? Contrasena { get; init; }

        /// <summary>Rol de realm: Administrador o EmpleadoVenta.</summary>
        public string Rol { get; init; } = string.Empty;
    }

    /// <summary>Token de acceso emitido por el proveedor de identidad.</summary>
    public sealed class TokenIdentidad
    {
        public string Token { get; init; } = string.Empty;

        public string RefreshToken { get; init; } = string.Empty;

        public DateTime Expira { get; init; }
    }
}
