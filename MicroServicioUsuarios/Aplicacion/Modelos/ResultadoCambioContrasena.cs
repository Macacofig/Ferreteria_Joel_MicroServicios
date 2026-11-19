namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado del cambio de contraseña. Los errores por campo se muestran
    /// junto a cada entrada del formulario. Cuando el cambio se aplica en
    /// Keycloak, se devuelve un token nuevo con la contraseña recién definida
    /// para que la sesión actual no tenga que volver a iniciar sesión.
    /// </summary>
    public class ResultadoCambioContrasena
    {
        public bool Exitoso { get; init; }

        public string Mensaje { get; init; } = string.Empty;

        public Dictionary<string, string> ErroresCampo { get; init; } = new();

        public string? Token { get; init; }

        public DateTime Expira { get; init; }

        public static ResultadoCambioContrasena Ok(string mensaje)
        {
            return new ResultadoCambioContrasena
            {
                Exitoso = true,
                Mensaje = mensaje
            };
        }

        public static ResultadoCambioContrasena Error(string mensaje)
        {
            return new ResultadoCambioContrasena
            {
                Exitoso = false,
                Mensaje = mensaje
            };
        }
    }
}
