using System.Text.RegularExpressions;

namespace FERRETERIA__Joel.Dominio.Utilidades
{
    /// <summary>
    /// Concilia el campo histórico <c>Apellido</c> con los campos nuevos
    /// <c>PrimerApellido</c> y <c>SegundoApellido</c>.
    ///
    /// <para>
    /// <c>Apellido</c> se mantiene como la concatenación de ambos para no
    /// romper los claims, el perfil, el personal ni la sincronización con
    /// Keycloak (que solo acepta un <c>lastName</c>). Los clientes antiguos
    /// que envían un único apellido siguen funcionando: se divide en primer
    /// y segundo apellido antes de normalizar.
    /// </para>
    /// </summary>
    public static class NormalizadorApellidos
    {
        /// <summary>Resultado de la normalización de los tres campos.</summary>
        public sealed class Resultado
        {
            public string Apellido { get; init; } = string.Empty;

            public string PrimerApellido { get; init; } = string.Empty;

            public string? SegundoApellido { get; init; }
        }

        public static Resultado Normalizar(
            string? apellido,
            string? primerApellido,
            string? segundoApellido)
        {
            string primero = Colapsar(primerApellido);
            string segundo = Colapsar(segundoApellido);
            string completo = Colapsar(apellido);

            // Compatibilidad con formularios/API que todavía envían un solo
            // "Apellido": la primera palabra es el primer apellido y el resto
            // se conserva como segundo (por ejemplo "Ríos" o "López Pérez").
            if (primero.Length == 0
                && segundo.Length == 0
                && completo.Length > 0)
            {
                string[] partes = completo.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

                primero = partes[0];
                segundo = partes.Length > 1
                    ? string.Join(' ', partes[1..])
                    : string.Empty;
            }

            string concatenado = string.Join(
                ' ',
                new[] { primero, segundo }
                    .Where(parte => parte.Length > 0));

            return new Resultado
            {
                Apellido = concatenado,
                PrimerApellido = primero,
                SegundoApellido = segundo.Length > 0 ? segundo : null
            };
        }

        public static string Concatenar(
            string? primerApellido,
            string? segundoApellido)
        {
            return Normalizar(null, primerApellido, segundoApellido).Apellido;
        }

        private static string Colapsar(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto)
                ? string.Empty
                : Regex.Replace(texto.Trim(), @"\s+", " ");
        }
    }
}
