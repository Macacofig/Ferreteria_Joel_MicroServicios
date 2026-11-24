-- ============================================================
-- FERRETERÍA JOEL — MIGRACIÓN DE USUARIOS (DATOS ACTUALES)
-- Base de datos: bdusuarios
--
-- Qué hace:
--   1. Añade PrimerApellido / SegundoApellido (idempotente).
--   2. Rellena ambos campos desde el Apellido existente.
--   3. Recalcula Apellido = PrimerApellido + SegundoApellido.
--   4. Fija los apellidos semilla de jrios y mlopez.
--   5. Elimina físicamente a TODOS los empleados excepto:
--        - jrios  (Joel Ríos,  Administrador)
--        - mlopez (María López, EmpleadoVenta)
--      y limpia la bitácora relacionada.
--   6. NO toca Contrasena: se conservan admin123 / clave123 (Keycloak).
--
-- Ejecutar sobre el volumen existente (NO reejecutar 01-usuarios.sql):
--   docker exec -i ferreteria-mysql-usuarios mysql -uroot -pusuarios_root_2026 < Scripts/04-usuarios-apellidos-migracion.sql
--
-- La limpieza de Keycloak está en Scripts/migrar_usuarios_apellidos.ps1.
-- ============================================================

USE bdusuarios;
SET NAMES utf8mb4;

START TRANSACTION;

-- ------------------------------------------------------------
-- 1) Columnas nuevas (MySQL 8 no admite ADD COLUMN IF NOT EXISTS)
-- ------------------------------------------------------------
SET @sql = (
    SELECT IF(
        (SELECT COUNT(*) FROM information_schema.COLUMNS
          WHERE TABLE_SCHEMA = 'bdusuarios'
            AND TABLE_NAME = 'empleado'
            AND COLUMN_NAME = 'PrimerApellido') = 0,
        'ALTER TABLE empleado ADD COLUMN PrimerApellido VARCHAR(100) NULL DEFAULT NULL AFTER Apellido',
        'SELECT 1'
    )
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        (SELECT COUNT(*) FROM information_schema.COLUMNS
          WHERE TABLE_SCHEMA = 'bdusuarios'
            AND TABLE_NAME = 'empleado'
            AND COLUMN_NAME = 'SegundoApellido') = 0,
        'ALTER TABLE empleado ADD COLUMN SegundoApellido VARCHAR(100) NULL DEFAULT NULL AFTER PrimerApellido',
        'SELECT 1'
    )
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ------------------------------------------------------------
-- 2) Backfill desde el Apellido histórico.
--    "López Pérez" -> PrimerApellido = López, SegundoApellido = Pérez
--    "Ríos"        -> PrimerApellido = Ríos,  SegundoApellido = NULL
-- ------------------------------------------------------------
UPDATE empleado
SET PrimerApellido = SUBSTRING_INDEX(TRIM(Apellido), ' ', 1)
WHERE (PrimerApellido IS NULL OR TRIM(PrimerApellido) = '')
  AND TRIM(COALESCE(Apellido, '')) <> '';

UPDATE empleado
SET SegundoApellido = NULLIF(
        TRIM(SUBSTRING(TRIM(Apellido), LOCATE(' ', TRIM(Apellido)) + 1)),
        '')
WHERE (SegundoApellido IS NULL OR TRIM(SegundoApellido) = '')
  AND LOCATE(' ', TRIM(COALESCE(Apellido, ''))) > 0;

-- ------------------------------------------------------------
-- 3) Apellido vuelve a ser la concatenación de ambos campos.
-- ------------------------------------------------------------
UPDATE empleado
SET Apellido = TRIM(CONCAT(
        COALESCE(PrimerApellido, ''),
        ' ',
        COALESCE(SegundoApellido, '')));

-- ------------------------------------------------------------
-- 4) Apellidos y roles semilla de los dos usuarios conservados.
--    Las contraseñas NO se modifican.
-- ------------------------------------------------------------
UPDATE empleado
SET Nombre = 'Joel',
    Apellido = 'Ríos',
    PrimerApellido = 'Ríos',
    SegundoApellido = NULL,
    Rol = 'Administrador',
    Estado = 1
WHERE NombreUsuario = 'jrios';

UPDATE empleado
SET Nombre = 'María',
    Apellido = 'López',
    PrimerApellido = 'López',
    SegundoApellido = NULL,
    Rol = 'EmpleadoVenta',
    Estado = 1
WHERE NombreUsuario = 'mlopez';

-- ------------------------------------------------------------
-- 5) Preparar borrado físico: responsables, bitácora y filas.
-- ------------------------------------------------------------
-- El responsable de los usuarios conservados debe ser jrios (o NULL).
UPDATE empleado e
LEFT JOIN empleado r ON r.IdEmpleado = e.IdEmpleadoResponsable
SET e.IdEmpleadoResponsable = NULL
WHERE e.IdEmpleadoResponsable IS NOT NULL
  AND (r.IdEmpleado IS NULL OR COALESCE(r.NombreUsuario, '') NOT IN ('jrios', 'mlopez'));

DELETE b FROM bitacora_sesion b
LEFT JOIN empleado e ON e.IdEmpleado = b.IdUsuario
WHERE b.IdUsuario IS NOT NULL
  AND (e.IdEmpleado IS NULL OR COALESCE(e.NombreUsuario, '') NOT IN ('jrios', 'mlopez'));

DELETE b FROM bitacora_sesion b
WHERE b.Entidad = 'empleado'
  AND b.IdRegistroAfectado IN (
      SELECT IdEmpleado
      FROM (SELECT IdEmpleado FROM empleado
            WHERE COALESCE(NombreUsuario, '') NOT IN ('jrios', 'mlopez')) eliminados
  );

DELETE FROM empleado
WHERE COALESCE(NombreUsuario, '') NOT IN ('jrios', 'mlopez');

COMMIT;

-- ------------------------------------------------------------
-- Verificación (debe devolver exactamente 2 filas)
-- ------------------------------------------------------------
SELECT IdEmpleado, Nombre, PrimerApellido, SegundoApellido, Apellido,
       NombreUsuario, Rol, CorreoElectronico, Estado
FROM empleado
ORDER BY IdEmpleado;
