-- =====================================================================
-- Permisos de caja y gastos para los negocios que ya existían
-- (los nuevos los reciben solos). EJECUTAR COMO ventas_owner. Es idempotente.
-- Después, cierra sesión y vuelve a entrar (los permisos viajan en el token).
-- Recuerda también ejecutar POST /api/ventas/inicializar para crear las
-- categorías de gasto iniciales.
-- =====================================================================

-- Administrador: todos
UPDATE identidad.roles r
SET permisos = ARRAY(
        SELECT DISTINCT p FROM unnest(r.permisos || ARRAY[
            'caja.operar', 'caja.historial',
            'gastos.leer', 'gastos.registrar', 'gastos.anular'
        ]) AS p
        ORDER BY p)
WHERE r.es_sistema AND r.nombre = 'Administrador';

-- Vendedor: maneja su caja y registra gastos pagados del cajón (no ve historial ni anula)
UPDATE identidad.roles r
SET permisos = ARRAY(
        SELECT DISTINCT p FROM unnest(r.permisos || ARRAY[
            'caja.operar', 'gastos.registrar'
        ]) AS p
        ORDER BY p)
WHERE r.es_sistema AND r.nombre = 'Vendedor';
