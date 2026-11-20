using Npgsql;

namespace MicroServicioClientes.Infraestructura.Persistencia;

/// <summary>Crea conexiones con la cadena <c>ConnectionStrings:PostgresConnection</c>.</summary>
public sealed class FabricaConexionPostgres : IFabricaConexion
{
    private readonly string _cadenaConexion;

    public FabricaConexionPostgres(IConfiguration configuracion)
    {
        string? cadena = configuracion.GetConnectionString("PostgresConnection");

        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión ConnectionStrings:PostgresConnection.");
        }

        _cadenaConexion = cadena;
    }

    public NpgsqlConnection Crear() => new(_cadenaConexion);
}