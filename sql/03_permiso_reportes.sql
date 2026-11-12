-- =====================================================================
-- Agrega el permiso nuevo 'reportes.ver' (dashboard) al rol Administrador
-- de los negocios que ya existían. Los negocios nuevos lo reciben solos.
-- EJECUTAR COMO ventas_owner. Es idempotente.
-- Después, los administradores deben cerrar sesión y volver a entrar
-- (los permisos viajan dentro del token).
-- =====================================================================
UPDATE identidad.roles
SET permisos = array_append(permisos, 'reportes.ver')
WHERE es_sistema
  AND nombre = 'Administrador'
  AND NOT ('reportes.ver' = ANY (permisos));
