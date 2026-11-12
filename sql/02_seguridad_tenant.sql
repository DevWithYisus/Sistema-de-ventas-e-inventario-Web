-- =====================================================================
-- Seguridad multi-tenant. EJECUTAR COMO ventas_owner DESPUÉS de aplicar migraciones.
-- Es idempotente: se puede ejecutar cuantas veces quieras (y tras cada módulo nuevo).
--  1. Da permisos al usuario de la aplicación (ventas_app).
--  2. Activa Row Level Security en toda tabla que tenga la columna tenant_id.
--  3. Hace inmutable el kardex (si existe).
-- Para agregar un módulo nuevo, añade su esquema en el arreglo de abajo.
-- =====================================================================
DO $$
DECLARE
    esquemas text[] := ARRAY['catalogo', 'inventario', 'identidad', 'ventas'];
    s text;
    t record;
BEGIN
    FOREACH s IN ARRAY esquemas LOOP
        IF NOT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = s) THEN
            CONTINUE;
        END IF;

        EXECUTE format('GRANT USAGE ON SCHEMA %I TO ventas_app', s);
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO ventas_app', s);
        EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO ventas_app', s);
        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ventas_app', s);
        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT ON SEQUENCES TO ventas_app', s);

        FOR t IN
            SELECT c.table_name
            FROM information_schema.columns c
            JOIN information_schema.tables tb
              ON tb.table_schema = c.table_schema AND tb.table_name = c.table_name
            WHERE c.table_schema = s
              AND c.column_name = 'tenant_id'
              AND tb.table_type = 'BASE TABLE'
        LOOP
            EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY', s, t.table_name);
            EXECUTE format('DROP POLICY IF EXISTS aislamiento_tenant ON %I.%I', s, t.table_name);
            EXECUTE format(
                'CREATE POLICY aislamiento_tenant ON %I.%I USING (tenant_id = NULLIF(current_setting(''app.tenant_id'', true), '''')::uuid)',
                s, t.table_name);
        END LOOP;
    END LOOP;

    -- El kardex es un libro inmutable: la aplicación solo lee e inserta
    IF to_regclass('inventario.kardex') IS NOT NULL THEN
        REVOKE UPDATE, DELETE ON inventario.kardex FROM ventas_app;
    END IF;
END $$;
