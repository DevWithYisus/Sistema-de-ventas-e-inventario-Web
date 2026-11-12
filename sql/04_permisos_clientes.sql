-- =====================================================================
-- Permisos nuevos del módulo de clientes, crédito, abonos y configuración
-- para los negocios que ya existían (los nuevos los reciben solos).
-- EJECUTAR COMO ventas_owner. Es idempotente.
-- Después, cierra sesión y vuelve a entrar (los permisos viajan en el token).
-- =====================================================================

-- Administrador: todos los permisos nuevos
UPDATE identidad.roles r
SET permisos = ARRAY(
        SELECT DISTINCT p FROM unnest(r.permisos || ARRAY[
            'clientes.leer', 'clientes.escribir', 'clientes.credito',
            'abonos.registrar', 'abonos.anular', 'configuracion.administrar'
        ]) AS p
        ORDER BY p)
WHERE r.es_sistema AND r.nombre = 'Administrador';

-- Vendedor: atiende clientes y recibe abonos (no asigna cupos ni anula abonos)
UPDATE identidad.roles r
SET permisos = ARRAY(
        SELECT DISTINCT p FROM unnest(r.permisos || ARRAY[
            'clientes.leer', 'clientes.escribir', 'abonos.registrar'
        ]) AS p
        ORDER BY p)
WHERE r.es_sistema AND r.nombre = 'Vendedor';
