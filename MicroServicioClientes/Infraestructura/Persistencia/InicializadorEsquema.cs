using Npgsql;

namespace MicroServicioClientes.Infraestructura.Persistencia;

/// <summary>
/// Crea la tabla <c>cliente</c> al arrancar (es idempotente). Evita
/// depender de un script externo montado en Docker.
/// </summary>
public sealed class InicializadorEsquema : IHostedService
{
    private const int MaximoIntentos = 6;

    private const string Esquema = """
        CREATE TABLE IF NOT EXISTS cliente (
            id_cliente           SERIAL PRIMARY KEY,
            ci                   VARCHAR(15),
            nombre               VARCHAR(100) NOT NULL,
            apellido             VARCHAR(100) NOT NULL,
            correo               VARCHAR(100),
            telefono             VARCHAR(20),
            direccion            VARCHAR(150),
            activo               BOOLEAN      NOT NULL DEFAULT TRUE,
            fecha_creacion       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            usuario_creacion     VARCHAR(100) NOT NULL DEFAULT 'sistema',
            fecha_modificacion   TIMESTAMPTZ,
            usuario_modificacion VARCHAR(100)
        );

        -- La CI es opcional, pero cuando existe no puede repetirse (los NULL no chocan).
        CREATE UNIQUE INDEX IF NOT EXISTS ux_cliente_ci
            ON cliente (LOWER(ci));
        """;

    private readonly IFabricaConexion _fabrica;
    private readonly ILogger<InicializadorEsquema> _logger;

    public InicializadorEsquema(IFabricaConexion fabrica, ILogger<InicializadorEsquema> logger)
    {
        _fabrica = fabrica;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancelacion)
    {
        for (int intento = 1; intento <= MaximoIntentos; intento++)
        {
            try
            {
                await using NpgsqlConnection conexion = _fabrica.Crear();
                await conexion.OpenAsync(cancelacion);

                await using var comando = new NpgsqlCommand(Esquema, conexion);
                await comando.ExecuteNonQueryAsync(cancelacion);

                _logger.LogInformation("Esquema de clientes verificado.");
                return;
            }
            catch (NpgsqlException ex)
            {
                if (intento == MaximoIntentos)
                {
                    _logger.LogError(ex, "No se pudo preparar el esquema de clientes.");
                    return;
                }

                _logger.LogWarning(
                    "PostgreSQL no está disponible (intento {Intento}/{Maximo}). Se reintenta en 3 s.",
                    intento, MaximoIntentos);

                await Task.Delay(TimeSpan.FromSeconds(3), cancelacion);
            }
        }
    }

    public Task StopAsync(CancellationToken cancelacion) => Task.CompletedTask;
}
