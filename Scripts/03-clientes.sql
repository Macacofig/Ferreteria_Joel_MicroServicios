-- ============================================================
-- FERRETERÍA JOEL — BASE DE DATOS DEL MICROSERVICIO DE CLIENTES
-- Motor: PostgreSQL 16   |   Base de datos: bdclientes
--
-- Mismo esquema que crea InicializadorEsquema.cs al arrancar el
-- microservicio (idempotente), más datos semilla. Sirve como
-- evidencia del diseño de la BDD y para cargar datos de ejemplo.
-- Docker lo monta en /docker-entrypoint-initdb.d/03-clientes.sql.
-- ============================================================

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
    usuario_modificacion VARCHAR(100),
    fecha_baja           TIMESTAMPTZ,
    usuario_baja         VARCHAR(100)
);

-- Compatibilidad con bases ya creadas: la baja lógica también se audita (RF-08).
ALTER TABLE cliente ADD COLUMN IF NOT EXISTS fecha_baja TIMESTAMPTZ;
ALTER TABLE cliente ADD COLUMN IF NOT EXISTS usuario_baja VARCHAR(100);

-- La CI es opcional, pero cuando existe no puede repetirse (los NULL no chocan).
CREATE UNIQUE INDEX IF NOT EXISTS ux_cliente_ci ON cliente (LOWER(ci));

-- Segunda tabla del microservicio: personas de contacto por cliente.
-- Incluye auditoría completa (creación, modificación y baja lógica).
CREATE TABLE IF NOT EXISTS cliente_contacto (
    id_contacto          SERIAL PRIMARY KEY,
    id_cliente           INTEGER      NOT NULL
                             REFERENCES cliente (id_cliente) ON DELETE RESTRICT,
    nombre               VARCHAR(100) NOT NULL,
    cargo                VARCHAR(50),
    telefono             VARCHAR(20),
    correo               VARCHAR(100),
    activo               BOOLEAN      NOT NULL DEFAULT TRUE,
    fecha_creacion       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    usuario_creacion     VARCHAR(100) NOT NULL DEFAULT 'sistema',
    fecha_modificacion   TIMESTAMPTZ,
    usuario_modificacion VARCHAR(100),
    fecha_baja           TIMESTAMPTZ,
    usuario_baja         VARCHAR(100)
);

CREATE INDEX IF NOT EXISTS ix_contacto_cliente_activo
    ON cliente_contacto (id_cliente, activo);

-- ============================================================
-- DATOS SEMILLA — CLIENTES (9)
-- ============================================================
INSERT INTO cliente (ci, nombre, apellido, correo, telefono, direccion, usuario_creacion)
VALUES
    ('4567123', 'Roberto',  'Salazar',   'roberto.salazar@correo.com',  '71230001', 'Av. Blanco Galindo km 4, Cochabamba',    'jrios'),
    ('5123456', 'Carmen',   'Quispe',    'carmen.quispe@correo.com',    '71230002', 'Calle Jordán 520, Cochabamba',           'jrios'),
    ('6234567', 'Fernando', 'Mamani',    'fernando.mamani@correo.com',  '71230003', 'Av. Petrolera km 3, Cochabamba',         'mlopez'),
    ('7345678', 'Patricia', 'Condori',   'patricia.condori@correo.com', '71230004', 'Calle Lanza 310, Cochabamba',            'mlopez'),
    ('8456789', 'Gustavo',  'Flores',    'gustavo.flores@correo.com',   '71230005', 'Av. América 1250, Cochabamba',           'mlopez'),
    ('3567890', 'Daniela',  'Arce',      'daniela.arce@correo.com',     '71230006', 'Av. Villazón km 2, Cochabamba',          'cmendoza'),
    ('9678901', 'Marcelo',  'Choque',    'marcelo.choque@correo.com',   '71230007', 'Av. Ayacucho 780, Cochabamba',           'cmendoza'),
    ('2789012', 'Verónica', 'Rocha',     'veronica.rocha@correo.com',   '71230008', 'Calle Bolívar 145, Quillacollo',         'agutierrez'),
    ('1890123', 'Álvaro',   'Peredo',    NULL,                          '71230009', 'Av. Circunvalación 600, Cochabamba',     'agutierrez')
ON CONFLICT DO NOTHING;

-- Auditoría explícita de los datos semilla (RF-08): creación, modificación
-- y baja. Así el script evidencia quién y cuándo realizó cada acción.
UPDATE cliente
   SET fecha_creacion       = '2026-11-20 09:00:00+00'::TIMESTAMPTZ,
       usuario_creacion     = 'jrios',
       fecha_modificacion   = '2026-11-21 11:30:00+00'::TIMESTAMPTZ,
       usuario_modificacion = 'mlopez'
 WHERE ci IN ('4567123', '5123456', '6234567', '7345678', '8456789',
              '3567890', '9678901', '2789012', '1890123');

-- Ejemplo de baja lógica con auditoría: la fila se conserva y se registra
-- quién la dio de baja y cuándo (nunca se elimina físicamente).
UPDATE cliente
   SET activo               = FALSE,
       fecha_modificacion   = '2026-11-23 16:45:00+00'::TIMESTAMPTZ,
       usuario_modificacion = 'agutierrez',
       fecha_baja           = '2026-11-23 16:45:00+00'::TIMESTAMPTZ,
       usuario_baja         = 'agutierrez'
 WHERE ci = '1890123';

-- ============================================================
-- DATOS SEMILLA — CONTACTOS
-- Se ejecuta después de los clientes para que las FK existan.
-- ============================================================
INSERT INTO cliente_contacto (id_cliente, nombre, cargo, telefono, correo, usuario_creacion)
SELECT c.id_cliente, v.nombre, v.cargo, v.telefono, v.correo, 'jrios'
FROM cliente c
JOIN (VALUES
    ('4567123', 'Ana Salazar',    'Jefa de compras', '71231001', 'ana.salazar@correo.com'),
    ('5123456', 'Luis Quispe',    'Encargado',       '71231002', 'luis.quispe@correo.com'),
    ('6234567', 'Rosa Mamani',    'Almacenera',      '71231003', 'rosa.mamani@correo.com')
) AS v(ci, nombre, cargo, telefono, correo) ON LOWER(v.ci) = LOWER(c.ci)
ON CONFLICT DO NOTHING;

-- La segunda tabla también deja evidencia de creación y modificación.
UPDATE cliente_contacto
   SET fecha_creacion       = '2026-11-20 10:00:00+00'::TIMESTAMPTZ,
       usuario_creacion     = 'jrios',
       fecha_modificacion   = '2026-11-22 12:00:00+00'::TIMESTAMPTZ,
       usuario_modificacion = 'mlopez'
 WHERE correo IN ('ana.salazar@correo.com',
                  'luis.quispe@correo.com',
                  'rosa.mamani@correo.com');
