-- ============================================================
-- FERRETERÍA JOEL — BASE DE DATOS DEL MICROSERVICIO DE PRODUCTOS
-- Base de datos: bdproductos  (base separada por microservicio)
--
-- Incluye: categoria, marca, producto e historico_producto
-- con columnas de auditoría estándar del proyecto.
-- NO crea FK hacia empleado porque empleado vive en bdusuarios.
-- ============================================================

CREATE DATABASE IF NOT EXISTS bdproductos
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

USE bdproductos;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS historico_producto;
DROP TABLE IF EXISTS producto;
DROP TABLE IF EXISTS marca;
DROP TABLE IF EXISTS categoria;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- CATEGORIA
-- ============================================================
CREATE TABLE categoria
(
    IdCategoria           SMALLINT     NOT NULL AUTO_INCREMENT,
    Codigo                VARCHAR(20)  NOT NULL,
    Nombre                VARCHAR(100) NOT NULL,
    Descripcion           VARCHAR(255) NULL DEFAULT NULL,
    Estado                TINYINT      NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME     NULL DEFAULT NULL,
    UsuarioCreacion       INT          NULL DEFAULT NULL,
    FechaCreacion         DATETIME     NULL DEFAULT NULL,
    UsuarioModificacion   INT          NULL DEFAULT NULL,
    FechaModificacion     DATETIME     NULL DEFAULT NULL,
    UsuarioBaja           INT          NULL DEFAULT NULL,
    FechaBaja             DATETIME     NULL DEFAULT NULL,
    IdEmpleadoResponsable INT          NULL DEFAULT NULL,
    PRIMARY KEY (IdCategoria),
    UNIQUE KEY UK_categoria_codigo (Codigo),
    UNIQUE KEY UK_categoria_nombre (Nombre),
    KEY IX_categoria_usuario_creacion (UsuarioCreacion),
    KEY IX_categoria_usuario_modificacion (UsuarioModificacion),
    KEY IX_categoria_usuario_baja (UsuarioBaja)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- MARCA
-- ============================================================
CREATE TABLE marca
(
    IdMarca               SMALLINT     NOT NULL AUTO_INCREMENT,
    Nombre                VARCHAR(100) NOT NULL,
    PaisOrigen            VARCHAR(80)  NULL DEFAULT NULL,
    Estado                TINYINT      NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME     NULL DEFAULT NULL,
    UsuarioCreacion       INT          NULL DEFAULT NULL,
    FechaCreacion         DATETIME     NULL DEFAULT NULL,
    UsuarioModificacion   INT          NULL DEFAULT NULL,
    FechaModificacion     DATETIME     NULL DEFAULT NULL,
    UsuarioBaja           INT          NULL DEFAULT NULL,
    FechaBaja             DATETIME     NULL DEFAULT NULL,
    IdEmpleadoResponsable INT          NULL DEFAULT NULL,
    PRIMARY KEY (IdMarca),
    UNIQUE KEY UK_marca_nombre (Nombre),
    KEY IX_marca_usuario_creacion (UsuarioCreacion),
    KEY IX_marca_usuario_modificacion (UsuarioModificacion),
    KEY IX_marca_usuario_baja (UsuarioBaja)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- PRODUCTO
-- ============================================================
CREATE TABLE producto
(
    IdProducto            INT            NOT NULL AUTO_INCREMENT,
    IdCategoria           SMALLINT       NOT NULL,
    IdMarca               SMALLINT       NOT NULL,
    Codigo                VARCHAR(30)    NOT NULL,
    Nombre                VARCHAR(150)   NOT NULL,
    Descripcion           VARCHAR(500)   NULL DEFAULT NULL,
    UnidadMedida          VARCHAR(30)    NOT NULL,
    PrecioVenta           DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    CantidadStock         INT            NOT NULL DEFAULT 0,
    Estado                TINYINT        NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME       NULL DEFAULT NULL,
    UsuarioCreacion       INT            NULL DEFAULT NULL,
    FechaCreacion         DATETIME       NULL DEFAULT NULL,
    UsuarioModificacion   INT            NULL DEFAULT NULL,
    FechaModificacion     DATETIME       NULL DEFAULT NULL,
    UsuarioBaja           INT            NULL DEFAULT NULL,
    FechaBaja             DATETIME       NULL DEFAULT NULL,
    IdEmpleadoResponsable INT            NULL DEFAULT NULL,
    PRIMARY KEY (IdProducto),
    UNIQUE KEY UK_producto_codigo (Codigo),
    KEY IX_producto_categoria (IdCategoria),
    KEY IX_producto_marca (IdMarca),
    KEY IX_producto_usuario_creacion (UsuarioCreacion),
    KEY IX_producto_usuario_modificacion (UsuarioModificacion),
    KEY IX_producto_usuario_baja (UsuarioBaja),
    CONSTRAINT FK_producto_categoria
        FOREIGN KEY (IdCategoria) REFERENCES categoria (IdCategoria)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT FK_producto_marca
        FOREIGN KEY (IdMarca) REFERENCES marca (IdMarca)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- HISTORICO_PRODUCTO
-- ============================================================
CREATE TABLE historico_producto
(
    IdHistorico           INT            NOT NULL AUTO_INCREMENT,
    IdProducto            INT            NOT NULL,
    Accion                VARCHAR(30)    NOT NULL COMMENT 'INSERT | UPDATE | DELETE',
    PrecioAnterior        DECIMAL(10, 2) NULL DEFAULT NULL,
    PrecioNuevo           DECIMAL(10, 2) NULL DEFAULT NULL,
    StockAnterior         INT            NULL DEFAULT NULL,
    StockNuevo            INT            NULL DEFAULT NULL,
    Detalle               VARCHAR(300)   NULL DEFAULT NULL,
    FechaCambio           DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdEmpleadoResponsable INT            NULL DEFAULT NULL,
    PRIMARY KEY (IdHistorico),
    KEY IX_historico_producto (IdProducto),
    KEY IX_historico_fecha (FechaCambio),
    CONSTRAINT FK_historico_producto
        FOREIGN KEY (IdProducto) REFERENCES producto (IdProducto)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- DATOS SEMILLA (SEED)
-- ============================================================
INSERT INTO categoria
    (IdCategoria, Codigo, Nombre, Descripcion, Estado, UsuarioCreacion, FechaCreacion, IdEmpleadoResponsable)
VALUES
    (1,  'CAT-001', 'Herramientas Manuales',    'Herramientas manuales de construcción y reparación.', 1, 1, CURRENT_TIMESTAMP, 1),
    (2,  'CAT-002', 'Herramientas Eléctricas',  'Herramientas eléctricas y a batería.',                  1, 1, CURRENT_TIMESTAMP, 1),
    (3,  'CAT-003', 'Plomería',                 'Tuberías, conexiones y accesorios sanitarios.',         1, 1, CURRENT_TIMESTAMP, 1),
    (4,  'CAT-004', 'Electricidad',             'Cables, interruptores y material eléctrico.',           1, 1, CURRENT_TIMESTAMP, 1),
    (5,  'CAT-005', 'Pinturas',                 'Pinturas, barnices y recubrimientos.',                  1, 1, CURRENT_TIMESTAMP, 1),
    (6,  'CAT-006', 'Fijaciones y Tornillería', 'Anclas, clavos, tornillos y pernos.',                   1, 1, CURRENT_TIMESTAMP, 1),
    (7,  'CAT-007', 'Seguridad Industrial',     'Equipos de protección personal.',                       1, 1, CURRENT_TIMESTAMP, 1),
    (8,  'CAT-008', 'Jardinería',               'Insumos y herramientas de jardín.',                     1, 1, CURRENT_TIMESTAMP, 1),
    (9,  'CAT-009', 'Construcción',             'Materiales básicos de obra gruesa y fina.',             1, 1, CURRENT_TIMESTAMP, 1),
    (10, 'CAT-010', 'Adhesivos y Sellantes',    'Siliconas, pegamentos y selladores.',                   1, 1, CURRENT_TIMESTAMP, 1);

INSERT INTO marca
    (IdMarca, Nombre, PaisOrigen, Estado, UsuarioCreacion, FechaCreacion, IdEmpleadoResponsable)
VALUES
    (1,  'Bosch',   'Alemania',       1, 1, CURRENT_TIMESTAMP, 1),
    (2,  'Stanley', 'Estados Unidos', 1, 1, CURRENT_TIMESTAMP, 1),
    (3,  'Truper',  'México',         1, 1, CURRENT_TIMESTAMP, 1),
    (4,  'DeWalt',  'Estados Unidos', 1, 1, CURRENT_TIMESTAMP, 1),
    (5,  'Makita',  'Japón',          1, 1, CURRENT_TIMESTAMP, 1),
    (6,  'Hilti',   'Liechtenstein',  1, 1, CURRENT_TIMESTAMP, 1),
    (7,  '3M',      'Estados Unidos', 1, 1, CURRENT_TIMESTAMP, 1),
    (8,  'Sika',    'Suiza',          1, 1, CURRENT_TIMESTAMP, 1),
    (9,  'Fischer', 'Alemania',       1, 1, CURRENT_TIMESTAMP, 1),
    (10, 'Cemex',   'México',         1, 1, CURRENT_TIMESTAMP, 1);

INSERT INTO producto
    (IdCategoria, IdMarca, Codigo, Nombre, Descripcion, UnidadMedida, PrecioVenta, CantidadStock, Estado, UsuarioCreacion, FechaCreacion, IdEmpleadoResponsable)
VALUES
    (1,  3,  'PROD-001', 'Martillo de uña 16 oz',        'Mango de fibra de vidrio',      'Pieza',  185.50, 40, 1, 1, CURRENT_TIMESTAMP, 1),
    (1,  2,  'PROD-002', 'Juego de desarmadores 6 pzas', 'Puntas plana y estrella',       'Juego',  320.00, 25, 1, 1, CURRENT_TIMESTAMP, 1),
    (2,  1,  'PROD-003', 'Taladro percutor 650W',        'Portabrocas 1/2 pulgada',       'Pieza', 1450.00, 15, 1, 1, CURRENT_TIMESTAMP, 1),
    (2,  4,  'PROD-004', 'Esmeriladora angular 4-1/2"',  'Velocidad variable',            'Pieza', 1680.00, 12, 1, 1, CURRENT_TIMESTAMP, 1),
    (2,  5,  'PROD-005', 'Sierra circular 7-1/4"',       'Incluye disco de corte',        'Pieza', 2100.00, 10, 1, 1, CURRENT_TIMESTAMP, 1),
    (6,  9,  'PROD-006', 'Ancla expansiva 3/8"',         'Caja con 50 piezas',            'Caja',   420.00, 30, 1, 1, CURRENT_TIMESTAMP, 1),
    (10, 8,  'PROD-007', 'Silicona transparente 300 ml', 'Sellador multipropósito',       'Pieza',   95.00, 60, 1, 1, CURRENT_TIMESTAMP, 1),
    (7,  7,  'PROD-008', 'Respirador media cara',        'Incluye dos filtros',           'Pieza',  540.00, 20, 1, 1, CURRENT_TIMESTAMP, 1),
    (9,  10, 'PROD-009', 'Cemento gris 50 kg',           'Uso general en construcción',   'Saco',   280.00, 50, 1, 1, CURRENT_TIMESTAMP, 1),
    (2,  6,  'PROD-010', 'Rotomartillo SDS Plus 800W',   'Empuñadura antivibración',      'Pieza', 2650.00,  8, 1, 1, CURRENT_TIMESTAMP, 1);

INSERT INTO historico_producto
    (IdProducto, Accion, PrecioAnterior, PrecioNuevo, StockAnterior, StockNuevo, Detalle, IdEmpleadoResponsable)
VALUES
    (1,  'INSERT', NULL,  185.50, NULL, 40, 'Alta inicial en catálogo.', 1),
    (2,  'INSERT', NULL,  320.00, NULL, 25, 'Alta inicial en catálogo.', 1),
    (3,  'INSERT', NULL, 1450.00, NULL, 15, 'Alta inicial en catálogo.', 1),
    (4,  'INSERT', NULL, 1680.00, NULL, 12, 'Alta inicial en catálogo.', 1),
    (5,  'INSERT', NULL, 2100.00, NULL, 10, 'Alta inicial en catálogo.', 1),
    (6,  'INSERT', NULL,  420.00, NULL, 30, 'Alta inicial en catálogo.', 1),
    (7,  'INSERT', NULL,   95.00, NULL, 60, 'Alta inicial en catálogo.', 1),
    (8,  'INSERT', NULL,  540.00, NULL, 20, 'Alta inicial en catálogo.', 1),
    (9,  'INSERT', NULL,  280.00, NULL, 50, 'Alta inicial en catálogo.', 1),
    (10, 'INSERT', NULL, 2650.00, NULL,  8, 'Alta inicial en catálogo.', 1);