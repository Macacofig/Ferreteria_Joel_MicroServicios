-- ============================================================
-- FERRETERÍA JOEL — BASE DE DATOS DEL MICROSERVICIO DE USUARIOS
-- Base de datos: bdusuarios  (base separada por microservicio)
--
-- Script ÚNICO y MERGEADO del microservicio de usuarios. Incluye
-- únicamente lo que necesita MicroServicioUsuarios:
--   - tabla empleado (con las columnas de auditoría ya integradas,
--     provenientes de migracion_auditoria.sql);
--   - tabla bitacora_sesion (login / logout);
--   - datos semilla (10 empleados).
--
-- NO crea las tablas de los demás microservicios (productos, clientes,
-- proveedores): cada uno gestiona su propia base.
--
-- Docker lo monta en /docker-entrypoint-initdb.d/01-usuarios.sql, de modo
-- que MySQL lo ejecuta solo la primera vez que se crea el volumen.
-- ============================================================

CREATE DATABASE IF NOT EXISTS bdusuarios
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

USE bdusuarios;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS bitacora_sesion;
DROP TABLE IF EXISTS empleado;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- EMPLEADO
-- Columnas de auditoría integradas (migracion_auditoria.sql):
--   UsuarioCreacion, FechaCreacion, UsuarioModificacion,
--   FechaModificacion, UsuarioBaja, FechaBaja, IdEmpleadoResponsable.
-- Las credenciales efectivas viven en Keycloak; la columna Contrasena
-- conserva un hash Argon2id como respaldo.
-- ============================================================
CREATE TABLE empleado
(
    IdEmpleado            INT          NOT NULL AUTO_INCREMENT,
    Nombre                VARCHAR(100) NOT NULL,
    Apellido              VARCHAR(100) NOT NULL,
    PrimerApellido        VARCHAR(100) NULL DEFAULT NULL,
    SegundoApellido       VARCHAR(100) NULL DEFAULT NULL,
    NombreUsuario         VARCHAR(150) NULL DEFAULT NULL,
    Rol                   VARCHAR(50)  NOT NULL,
    Telefono              VARCHAR(20)  NOT NULL,
    CorreoElectronico     VARCHAR(150) NULL DEFAULT NULL,
    Contrasena            VARCHAR(255) NULL DEFAULT NULL,
    TokenActivacion       VARCHAR(128) NULL DEFAULT NULL,
    TokenExpira           DATETIME     NULL DEFAULT NULL,
    TokenUsado            TINYINT(1)   NOT NULL DEFAULT 0,
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
    PRIMARY KEY (IdEmpleado),
    UNIQUE KEY UNIQUE_EMPLEADO_CORREO (CorreoElectronico),
    UNIQUE KEY UNIQUE_EMPLEADO_USUARIO (NombreUsuario),
    KEY IDX_EMPLEADO_TOKEN (TokenActivacion),
    KEY IX_empleado_usuario_creacion (UsuarioCreacion),
    KEY IX_empleado_usuario_modificacion (UsuarioModificacion),
    KEY IX_empleado_usuario_baja (UsuarioBaja),
    CONSTRAINT FK_empleado_responsable
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- BITACORA_SESION (login / logout)
-- ============================================================
CREATE TABLE bitacora_sesion
(
    IdBitacora         INT          NOT NULL AUTO_INCREMENT,
    Accion             VARCHAR(20)  NOT NULL COMMENT 'login | logout',
    IdUsuario          INT          NULL DEFAULT NULL,
    NombreUsuario      VARCHAR(150) NULL DEFAULT NULL,
    TipoUsuario        VARCHAR(50)  NOT NULL COMMENT 'Administrador | EmpleadoVenta',
    FechaHora          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Entidad            VARCHAR(50)  NULL DEFAULT NULL,
    IdRegistroAfectado INT          NULL DEFAULT NULL,
    Resultado          VARCHAR(20)  NOT NULL DEFAULT 'EXITOSO',
    PRIMARY KEY (IdBitacora),
    KEY IDX_BITACORA_FECHA (FechaHora),
    KEY IDX_BITACORA_USUARIO (TipoUsuario, IdUsuario)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- DATOS SEMILLA — EMPLEADOS (10)
-- Las contraseñas de estos empleados se provisionan en Keycloak
-- (ver Keycloak/ferreteria-realm.json). La columna Contrasena
-- conserva el hash Argon2id histórico como respaldo.
-- ============================================================
INSERT INTO empleado
    (Nombre, Apellido, PrimerApellido, SegundoApellido, NombreUsuario, Rol, Telefono, CorreoElectronico, Contrasena, Estado, IdEmpleadoResponsable)
VALUES
    ('Joel',  'Ríos',       'Ríos',      NULL,       'jrios',      'Administrador', '70000001', 'joel.rios@ferreteria.com',     '$argon2id$v=19$m=19456,t=2,p=1$Z+Kb/5STembq6DWHh6ZgBg$vSM1I7ha07sC1lViQivu6mQFHo52FiCSl5zSYMFcfqI', 1, NULL),
    ('María', 'López',      'López',     NULL,       'mlopez',     'EmpleadoVenta', '70000002', 'maria.lopez@ferreteria.com',   '$argon2id$v=19$m=19456,t=2,p=1$432CJ2xhzoutD3YceF5mfQ$BGAYPqa6XVk6osyHgnFTeg7pB5NL8yAK05UhDHHlRm0', 1, 1),
    ('Carlos','Mendoza',    'Mendoza',   NULL,       'cmendoza',   'EmpleadoVenta', '70000003', 'carlos.mendoza@ferreteria.com','$argon2id$v=19$m=19456,t=2,p=1$UxDIBBPv5brarthGWQNkRQ$73iY2wXaUNsrAlHpXgImy3ZrigjX0q5UIPATOirSJgY', 1, 1),
    ('Ana',   'Gutiérrez',  'Gutiérrez', NULL,       'agutierrez', 'EmpleadoVenta', '70000004', 'ana.gutierrez@ferreteria.com', '$argon2id$v=19$m=19456,t=2,p=1$U8EUBa0bu4tDk3f82hCdHg$D4YF8Aq9qprVM1q3lPNC9FbXdq09lQkw2dK1pnridQY', 1, 1),
    ('Luis',  'Fernández',  'Fernández', NULL,       'lfernandez', 'EmpleadoVenta', '70000005', 'luis.fernandez@ferreteria.com','$argon2id$v=19$m=19456,t=2,p=1$mXiLh5CNwb1XmsPoZlCKYg$vqF5qWziDQV+EoyFtBBK9omnCnJTM6ywS78DephbAwM', 1, 1),
    ('Sofía', 'Ramírez',    'Ramírez',   NULL,       'sramirez',   'EmpleadoVenta', '70000006', 'sofia.ramirez@ferreteria.com', '$argon2id$v=19$m=19456,t=2,p=1$UKLS7lcxWKbEWJvAacTqRw$b65cck9cbwNYbvCFTLly9nQfAKimKKV/PIEyGj+fvvc', 1, 1),
    ('Diego', 'Torres',     'Torres',    NULL,       'dtorres',    'EmpleadoVenta', '70000007', 'diego.torres@ferreteria.com',  '$argon2id$v=19$m=19456,t=2,p=1$Rrba/l4K/4cRwn61PvA9gw$4IRRCq2JTCzsREoDcDDPqB5DczR+VrzRol4/WGyeTU4', 1, 1),
    ('Lucía', 'Vargas',     'Vargas',    NULL,       'lvargas',    'EmpleadoVenta', '70000008', 'lucia.vargas@ferreteria.com',  '$argon2id$v=19$m=19456,t=2,p=1$0Fns0IHrq/Kd6krlFhsQIQ$yjXH1tZbCRCs8ZbKwoybMf4UEkZVTc/EfZGUX/yFnDo', 1, 1),
    ('Pedro', 'Castillo',   'Castillo',  NULL,       'pcastillo',  'EmpleadoVenta', '70000009', 'pedro.castillo@ferreteria.com','$argon2id$v=19$m=19456,t=2,p=1$DkC+fWxOlRk1/X6aGj7e9A$a34WBFccYECQ3Ka+UwVczdl8Rn5zku1/kXvK+Yjpdsc', 1, 1),
    ('Elena', 'Rojas',      'Rojas',     NULL,       'erojas',     'EmpleadoVenta', '70000010', 'elena.rojas@ferreteria.com',   '$argon2id$v=19$m=19456,t=2,p=1$6lLxKhg6camSJXMOp3SN9A$vkifcdnD0iQr6VnYP1kF05W/7N/4AcCXfrd7Olubf0o', 1, 1);
