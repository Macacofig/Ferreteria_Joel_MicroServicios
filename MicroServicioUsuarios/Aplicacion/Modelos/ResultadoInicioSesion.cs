namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado del inicio de sesión: indica si fue exitoso, el mensaje a
    /// mostrar y, cuando corresponde, la identidad autenticada y el token
    /// emitido por Keycloak (el microservicio ya no firma sus propios JWT).
    /// </summary>
    public class ResultadoInicioSesion
    {
        public bool Exitoso { get; init; }

        public string Mensaje { get; init; } = string.Empty;

        public SesionUsuario? Usuario { get; init; }

        /// <summary>Access token firmado por el realm de Keycloak.</summary>
        public string? Token { get; init; }

        /// <summary>Refresh token para renovar la sesión.</summary>
        public string? RefreshToken { get; init; }

        /// <summary>Fecha de expiración del access token.</summary>
        public DateTime Expira { get; init; }
    }
}
