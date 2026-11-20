using Npgsql;

namespace MicroServicioClientes.Infraestructura.Persistencia;

/// <summary>
/// Crea las tablas <c>tipo_cliente</c> y <c>cliente</c> al arrancar (es
/// idempotente) y siembra los tipos iniciales si la tabla está vacía. Evita
/// depender de un script externo montado en Docker.
/// </summary>
public sealed class InicializadorEsquema : IHostedService
{
    private const int MaximoIntentos = 6;

    private const string Esquema = """
        CREATE TABLE IF NOT EXISTS tipo_cliente (
            id_tipo_cliente      SERIAL PRIMARY KEY,
            nombre               VARCHAR(50)  NOT NULL,
            descuento_porcentaje NUMERIC(5,2) NOT NULL DEFAULT 0
                                 CHECK (descuento_porcentaje BETWEEN 0 AND 100),
            descripcion          VARCHAR(150),
            activo               BOOLEAN      NOT NULL DEFAULT TRUE,
            fecha_creacion       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            usuario_creacion     VARCHAR(100) NOT NULL DEFAULT 'sistema',
            fecha_modificacion   TIMESTAMPTZ,
            usuario_modificacion VARCHAR(100)
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ux_tipo_cliente_nombre
            ON tipo_cliente (LOWER(nombre));

        CREATE TABLE IF NOT EXISTS cliente (
            id_cliente           SERIAL PRIMARY KEY,
            nombre               VARCHAR(80)  NOT NULL,
            documento            VARCHAR(20)  NOT NULL,
            correo               VARCHAR(100),
            telefono             VARCHAR(20)  NOT NULL,
            direccion            VARCHAR(150),
            id_tipo_cliente      INT          NOT NULL REFERENCES tipo_cliente (id_tipo_cliente),
            activo               BOOLEAN      NOT NULL DEFAULT TRUE,
            fecha_creacion       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            usuario_creacion     VARCHAR(100) NOT NULL DEFAULT 'sistema',
            fecha_modificacion   TIMESTAMPTZ,
            usuario_modificacion VARCHAR(100)
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ux_cliente_documento
            ON cliente (LOWER(documento));

        INSERT INTO tipo_cliente (nombre, descuento_porcentaje, descripcion)
        SELECT v.nombre, v.descuento, v.descripcion
          FROM (VALUES
                ('Minorista', 0.00,  'Compra al detalle sin descuento'),
                ('Mayorista', 10.00, 'Compra por volumen'),
                ('Empresa',   5.00,  'Cliente con facturación empresarial')
               ) AS v (nombre, descuento, descripcion)
         WHERE NOT EXISTS (SELECT 1 FROM tipo_cliente);
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
