-- =====================================================================
-- MODELO DE DATOS: CATÁLOGO + INVENTARIO (PostgreSQL 15+)
-- Multi-tenant (tenant_id en todas las tablas), un esquema por módulo.
-- Los IDs son UUID: genera UUIDv7 desde .NET (Guid.CreateVersion7()) para
-- que sean ordenables y no fragmenten los índices.
-- NOTA: este script es la referencia de diseño. En producción las tablas
-- se crean con migraciones de EF Core (una por módulo/DbContext).
-- =====================================================================

CREATE SCHEMA IF NOT EXISTS catalogo;
CREATE SCHEMA IF NOT EXISTS inventario;

-- =====================================================================
-- CATÁLOGO
-- =====================================================================

CREATE TABLE catalogo.categorias (
    id              uuid PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    categoria_padre_id uuid REFERENCES catalogo.categorias(id),
    codigo          varchar(30)  NOT NULL,
    nombre          varchar(150) NOT NULL,
    activo          boolean      NOT NULL DEFAULT true,
    creado_en       timestamptz  NOT NULL DEFAULT now(),
    creado_por      uuid,
    modificado_en   timestamptz,
    modificado_por  uuid,
    eliminado_en    timestamptz,                       -- soft delete
    UNIQUE (tenant_id, codigo)
);

CREATE TABLE catalogo.marcas (
    id          uuid PRIMARY KEY,
    tenant_id   uuid NOT NULL,
    nombre      varchar(100) NOT NULL,
    activo      boolean NOT NULL DEFAULT true,
    creado_en   timestamptz NOT NULL DEFAULT now(),
    eliminado_en timestamptz,
    UNIQUE (tenant_id, nombre)
);

CREATE TABLE catalogo.unidades_medida (
    id           uuid PRIMARY KEY,
    tenant_id    uuid NOT NULL,
    codigo       varchar(10)  NOT NULL,                -- UND, KG, LT, CJ
    nombre       varchar(60)  NOT NULL,
    permite_decimales boolean NOT NULL DEFAULT false,  -- UND no, KG sí
    activo       boolean NOT NULL DEFAULT true,
    UNIQUE (tenant_id, codigo)
);

CREATE TABLE catalogo.productos (
    id              uuid PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    nombre          varchar(200) NOT NULL,
    descripcion     text,
    categoria_id    uuid REFERENCES catalogo.categorias(id),
    marca_id        uuid REFERENCES catalogo.marcas(id),
    unidad_base_id  uuid NOT NULL REFERENCES catalogo.unidades_medida(id),
    tipo            varchar(20) NOT NULL DEFAULT 'BIEN'
                    CHECK (tipo IN ('BIEN','SERVICIO','COMBO')),
    -- Banderas que activan comportamiento según el tipo de negocio:
    maneja_inventario   boolean NOT NULL DEFAULT true,
    maneja_lotes        boolean NOT NULL DEFAULT false,
    maneja_vencimiento  boolean NOT NULL DEFAULT false,
    maneja_seriales     boolean NOT NULL DEFAULT false,
    permite_stock_negativo boolean,                    -- NULL = usa config del tenant
    impuesto_id     uuid,                              -- referencia lógica al módulo Configuración
    atributos       jsonb NOT NULL DEFAULT '{}'::jsonb,-- campos personalizables por negocio
    activo          boolean NOT NULL DEFAULT true,
    creado_en       timestamptz NOT NULL DEFAULT now(),
    creado_por      uuid,
    modificado_en   timestamptz,
    modificado_por  uuid,
    eliminado_en    timestamptz,
    row_version     bigint NOT NULL DEFAULT 1,         -- concurrencia optimista
    CHECK (maneja_vencimiento = false OR maneja_lotes = true)
);
CREATE INDEX ix_productos_tenant_nombre ON catalogo.productos (tenant_id, nombre);
CREATE INDEX ix_productos_categoria     ON catalogo.productos (tenant_id, categoria_id);
CREATE INDEX ix_productos_atributos     ON catalogo.productos USING gin (atributos);

-- Todo producto tiene al menos una variante (la "por defecto").
-- El inventario y las ventas siempre referencian la variante.
CREATE TABLE catalogo.variantes (
    id              uuid PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    producto_id     uuid NOT NULL REFERENCES catalogo.productos(id),
    sku             varchar(60)  NOT NULL,
    codigo_barras   varchar(60),
    nombre          varchar(200),                      -- ej. "Camisa M - Azul"
    atributos       jsonb NOT NULL DEFAULT '{}'::jsonb,-- {"talla":"M","color":"Azul"}
    es_default      boolean NOT NULL DEFAULT false,
    costo_estandar  numeric(18,4) NOT NULL DEFAULT 0,
    precio_base     numeric(18,4) NOT NULL DEFAULT 0,  -- las listas de precio van en otro módulo
    stock_minimo    numeric(18,4) NOT NULL DEFAULT 0,
    stock_maximo    numeric(18,4),
    activo          boolean NOT NULL DEFAULT true,
    creado_en       timestamptz NOT NULL DEFAULT now(),
    eliminado_en    timestamptz,
    UNIQUE (tenant_id, sku)
);
CREATE UNIQUE INDEX ux_variantes_barras
    ON catalogo.variantes (tenant_id, codigo_barras) WHERE codigo_barras IS NOT NULL;
CREATE UNIQUE INDEX ux_variantes_default
    ON catalogo.variantes (producto_id) WHERE es_default;

-- Presentaciones: vender/comprar en caja, six-pack, docena, etc.
CREATE TABLE catalogo.presentaciones (
    id            uuid PRIMARY KEY,
    tenant_id     uuid NOT NULL,
    variante_id   uuid NOT NULL REFERENCES catalogo.variantes(id),
    unidad_id     uuid NOT NULL REFERENCES catalogo.unidades_medida(id),
    factor        numeric(18,6) NOT NULL CHECK (factor > 0), -- cuántas unidades base contiene
    codigo_barras varchar(60),
    UNIQUE (variante_id, unidad_id)
);

-- =====================================================================
-- INVENTARIO
-- =====================================================================

CREATE TABLE inventario.almacenes (
    id            uuid PRIMARY KEY,
    tenant_id     uuid NOT NULL,
    codigo        varchar(20)  NOT NULL,
    nombre        varchar(120) NOT NULL,
    direccion     varchar(250),
    es_principal  boolean NOT NULL DEFAULT false,
    activo        boolean NOT NULL DEFAULT true,
    creado_en     timestamptz NOT NULL DEFAULT now(),
    eliminado_en  timestamptz,
    UNIQUE (tenant_id, codigo)
);

CREATE TABLE inventario.lotes (
    id                 uuid PRIMARY KEY,
    tenant_id          uuid NOT NULL,
    variante_id        uuid NOT NULL,                  -- referencia lógica a catalogo.variantes
    codigo_lote        varchar(60) NOT NULL,
    fecha_fabricacion  date,
    fecha_vencimiento  date,
    creado_en          timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, variante_id, codigo_lote)
);
CREATE INDEX ix_lotes_vencimiento ON inventario.lotes (tenant_id, fecha_vencimiento);

-- Catálogo de tipos de movimiento (parametrizable, con valores de sistema)
CREATE TABLE inventario.tipos_movimiento (
    id            uuid PRIMARY KEY,
    tenant_id     uuid NOT NULL,
    codigo        varchar(30) NOT NULL,                -- COMPRA, VENTA, AJUSTE_POS, TRASLADO...
    nombre        varchar(100) NOT NULL,
    naturaleza    varchar(10) NOT NULL CHECK (naturaleza IN ('ENTRADA','SALIDA','TRASLADO')),
    afecta_costo  boolean NOT NULL DEFAULT true,       -- las entradas valorizadas recalculan costo promedio
    es_sistema    boolean NOT NULL DEFAULT false,      -- los de sistema no se pueden borrar
    activo        boolean NOT NULL DEFAULT true,
    UNIQUE (tenant_id, codigo)
);

-- Documento de movimiento (cabecera)
CREATE TABLE inventario.movimientos (
    id                  uuid PRIMARY KEY,
    tenant_id           uuid NOT NULL,
    numero              varchar(30) NOT NULL,          -- consecutivo por tenant
    tipo_movimiento_id  uuid NOT NULL REFERENCES inventario.tipos_movimiento(id),
    fecha               timestamptz NOT NULL,
    almacen_origen_id   uuid REFERENCES inventario.almacenes(id),
    almacen_destino_id  uuid REFERENCES inventario.almacenes(id),
    -- Trazabilidad hacia el documento que lo originó (venta, compra, ajuste...)
    origen_tipo         varchar(30),
    origen_id           uuid,
    observacion         varchar(500),
    estado              varchar(15) NOT NULL DEFAULT 'CONFIRMADO'
                        CHECK (estado IN ('BORRADOR','CONFIRMADO','ANULADO')),
    anulado_por_movimiento_id uuid REFERENCES inventario.movimientos(id),
    creado_en           timestamptz NOT NULL DEFAULT now(),
    creado_por          uuid,
    UNIQUE (tenant_id, numero),
    CHECK (almacen_origen_id IS NOT NULL OR almacen_destino_id IS NOT NULL),
    CHECK (almacen_origen_id IS DISTINCT FROM almacen_destino_id)
);
CREATE INDEX ix_mov_origen ON inventario.movimientos (tenant_id, origen_tipo, origen_id);
CREATE INDEX ix_mov_fecha  ON inventario.movimientos (tenant_id, fecha DESC);

CREATE TABLE inventario.movimiento_detalles (
    id              uuid PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    movimiento_id   uuid NOT NULL REFERENCES inventario.movimientos(id),
    variante_id     uuid NOT NULL,                     -- referencia lógica a catalogo.variantes
    lote_id         uuid REFERENCES inventario.lotes(id),
    unidad_id       uuid NOT NULL,                     -- unidad en que se digitó
    cantidad        numeric(18,4) NOT NULL CHECK (cantidad > 0),
    factor          numeric(18,6) NOT NULL DEFAULT 1,  -- conversión a unidad base
    cantidad_base   numeric(18,4) NOT NULL CHECK (cantidad_base > 0),
    costo_unitario  numeric(18,4) NOT NULL DEFAULT 0,  -- por unidad base
    costo_total     numeric(18,4) NOT NULL DEFAULT 0
);
CREATE INDEX ix_movdet_mov ON inventario.movimiento_detalles (movimiento_id);

-- KARDEX: libro mayor inmutable. Solo INSERT, nunca UPDATE/DELETE.
-- Una línea por cada efecto sobre un almacén (un traslado genera 2).
CREATE TABLE inventario.kardex (
    id                      bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id               uuid NOT NULL,
    fecha                   timestamptz NOT NULL,
    almacen_id              uuid NOT NULL REFERENCES inventario.almacenes(id),
    variante_id             uuid NOT NULL,
    lote_id                 uuid REFERENCES inventario.lotes(id),
    movimiento_detalle_id   uuid NOT NULL REFERENCES inventario.movimiento_detalles(id),
    tipo_movimiento_codigo  varchar(30) NOT NULL,      -- copia para consultas rápidas
    cantidad                numeric(18,4) NOT NULL,    -- con signo: + entra, - sale
    costo_unitario          numeric(18,4) NOT NULL,
    saldo_cantidad          numeric(18,4) NOT NULL,    -- saldo tras este movimiento
    costo_promedio          numeric(18,4) NOT NULL,
    saldo_valor             numeric(18,4) NOT NULL
);
CREATE INDEX ix_kardex_consulta
    ON inventario.kardex (tenant_id, variante_id, almacen_id, fecha, id);

-- EXISTENCIAS: stock actual materializado (se actualiza en la misma
-- transacción que el movimiento). Es una caché de lo que dice el kardex.
CREATE TABLE inventario.existencias (
    id                  uuid PRIMARY KEY,
    tenant_id           uuid NOT NULL,
    almacen_id          uuid NOT NULL REFERENCES inventario.almacenes(id),
    variante_id         uuid NOT NULL,
    lote_id             uuid REFERENCES inventario.lotes(id),
    cantidad            numeric(18,4) NOT NULL DEFAULT 0,
    cantidad_reservada  numeric(18,4) NOT NULL DEFAULT 0, -- pedidos aún no facturados
    costo_promedio      numeric(18,4) NOT NULL DEFAULT 0,
    actualizado_en      timestamptz NOT NULL DEFAULT now(),
    row_version         bigint NOT NULL DEFAULT 1,     -- evita dos ventas simultáneas pisándose
    CHECK (cantidad_reservada >= 0)
);
-- Un solo registro por tenant+almacén+variante+lote (el lote NULL cuenta como un valor)
CREATE UNIQUE INDEX ux_existencias
    ON inventario.existencias (tenant_id, almacen_id, variante_id, lote_id) NULLS NOT DISTINCT;
CREATE INDEX ix_existencias_variante ON inventario.existencias (tenant_id, variante_id);

-- =====================================================================
-- ROW LEVEL SECURITY (defensa extra de aislamiento entre clientes)
-- La API ejecuta: SELECT set_config('app.tenant_id', '<guid>', true);
-- al abrir cada transacción. Repetir para cada tabla con tenant_id.
-- =====================================================================
ALTER TABLE catalogo.productos ENABLE ROW LEVEL SECURITY;
CREATE POLICY aislamiento_tenant ON catalogo.productos
    USING (tenant_id = current_setting('app.tenant_id')::uuid);
