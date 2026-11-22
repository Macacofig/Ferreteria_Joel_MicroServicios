using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace MicroServicioProductos.Infraestructura.Conexion;

/// <summary>
/// Construye conexiones a <c>bdproductos</c>. La cadena se toma de
/// <c>ConnectionStrings:MySqlConnection</c> (o de la variable de entorno
/// equivalente <c>ConnectionStrings__MySqlConnection</c>); si no está, se arma
/// con las variables MYSQL_* del entorno.
/// </summary>
public sealed class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        string? cadena = configuration.GetConnectionString("MySqlConnection");

        if (string.IsNullOrWhiteSpace(cadena))
        {
            cadena = configuration["ConnectionStrings__MySqlConnection"];
        }

        if (string.IsNullOrWhiteSpace(cadena))
        {
            string servidor = PrimeraNoVacia(
                configuration["MYSQL_HOST"], "localhost");
            string puerto = PrimeraNoVacia(
                configuration["MYSQL_PRODUCTOS_PUERTO_HOST"],
                configuration["MYSQL_PUERTO_HOST"],
                "3308");
            string baseDatos = PrimeraNoVacia(
                configuration["MYSQL_DATABASE"], "bdproductos");
            string usuario = PrimeraNoVacia(configuration["MYSQL_USER"], "root");
            string clave = PrimeraNoVacia(
                configuration["MYSQL_PRODUCTOS_ROOT_PASSWORD"],
                configuration["MYSQL_ROOT_PASSWORD"]);

            cadena = $"Server={servidor};Port={puerto};Database={baseDatos};" +
                     $"Uid={usuario};Pwd={clave};";
        }

        _connectionString = cadena;
    }

    public MySqlConnection CreateConnection() => new(_connectionString);

    private static string PrimeraNoVacia(params string?[] valores) =>
        valores.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor))
        ?? string.Empty;
}
