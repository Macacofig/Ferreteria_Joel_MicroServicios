namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Unidades de medida estandarizadas que se ofrecen en los formularios de
    /// productos. Se comparan sin distinguir mayúsculas para conservar el valor
    /// de productos cargados antes de existiera el combo.
    /// </summary>
    public static class UnidadesMedidaProducto
    {
        public static readonly string[] Valores =
        {
            "Unidad",
            "Pieza",
            "Juego",
            "Caja",
            "Paquete",
            "Bolsa",
            "Kilogramo",
            "Metro",
            "Litro",
            "Galón",
            "Par",
            "Rollo",
            "Tubo"
        };

        /// <summary>Devuelve la forma canónica de la unidad, si existe.</summary>
        public static string? Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return Valores.FirstOrDefault(unidad =>
                string.Equals(unidad, valor.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Devuelve la lista estándar e incluye temporalmente el valor actual
        /// cuando no coincide con ninguna opción, para que no se pierda al
        /// abrir la edición.
        /// </summary>
        public static List<string> ObtenerConValorActual(string? valorActual)
        {
            List<string> unidades = Valores.ToList();

            if (!string.IsNullOrWhiteSpace(valorActual)
                && Normalizar(valorActual) is null)
            {
                unidades.Add(valorActual.Trim());
            }

            return unidades;
        }
    }
}
