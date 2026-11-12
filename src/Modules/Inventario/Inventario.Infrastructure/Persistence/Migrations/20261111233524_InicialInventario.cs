using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventario.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventario");

            migrationBuilder.CreateTable(
                name: "almacenes",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_almacenes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consecutivos",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ultimo = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consecutivos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lotes",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    variante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_lote = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    fecha_fabricacion = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lotes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_movimiento",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    naturaleza = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    afecta_costo = table.Column<bool>(type: "boolean", nullable: false),
                    es_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_movimiento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "existencias",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    almacen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cantidad_reservada = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    costo_promedio = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ultima_secuencia = table.Column<long>(type: "bigint", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_existencias", x => x.id);
                    table.CheckConstraint("ck_existencias_reservada", "cantidad_reservada >= 0");
                    table.ForeignKey(
                        name: "FK_existencias_almacenes_almacen_id",
                        column: x => x.almacen_id,
                        principalSchema: "inventario",
                        principalTable: "almacenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_existencias_lotes_lote_id",
                        column: x => x.lote_id,
                        principalSchema: "inventario",
                        principalTable: "lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    tipo_movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    naturaleza = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    afecta_costo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    almacen_origen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    almacen_destino_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origen_tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    origen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_almacenes_almacen_destino_id",
                        column: x => x.almacen_destino_id,
                        principalSchema: "inventario",
                        principalTable: "almacenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_almacenes_almacen_origen_id",
                        column: x => x.almacen_origen_id,
                        principalSchema: "inventario",
                        principalTable: "almacenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_tipos_movimiento_tipo_movimiento_id",
                        column: x => x.tipo_movimiento_id,
                        principalSchema: "inventario",
                        principalTable: "tipos_movimiento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_detalles",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    factor = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    costo_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_detalles", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimiento_detalles_lotes_lote_id",
                        column: x => x.lote_id,
                        principalSchema: "inventario",
                        principalTable: "lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_detalles_movimientos_movimiento_id",
                        column: x => x.movimiento_id,
                        principalSchema: "inventario",
                        principalTable: "movimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kardex",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    secuencia = table.Column<long>(type: "bigint", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    almacen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movimiento_detalle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_movimiento_codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    saldo_cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    costo_promedio = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    saldo_valor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kardex", x => x.id);
                    table.ForeignKey(
                        name: "FK_kardex_almacenes_almacen_id",
                        column: x => x.almacen_id,
                        principalSchema: "inventario",
                        principalTable: "almacenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_kardex_lotes_lote_id",
                        column: x => x.lote_id,
                        principalSchema: "inventario",
                        principalTable: "lotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_kardex_movimiento_detalles_movimiento_detalle_id",
                        column: x => x.movimiento_detalle_id,
                        principalSchema: "inventario",
                        principalTable: "movimiento_detalles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_kardex_movimientos_movimiento_id",
                        column: x => x.movimiento_id,
                        principalSchema: "inventario",
                        principalTable: "movimientos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_almacenes_tenant_id_codigo",
                schema: "inventario",
                table: "almacenes",
                columns: new[] { "tenant_id", "codigo" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_consecutivos_tenant_id_clave",
                schema: "inventario",
                table: "consecutivos",
                columns: new[] { "tenant_id", "clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_existencias_almacen_id",
                schema: "inventario",
                table: "existencias",
                column: "almacen_id");

            migrationBuilder.CreateIndex(
                name: "IX_existencias_lote_id",
                schema: "inventario",
                table: "existencias",
                column: "lote_id");

            migrationBuilder.CreateIndex(
                name: "IX_existencias_tenant_id_almacen_id_variante_id_lote_id",
                schema: "inventario",
                table: "existencias",
                columns: new[] { "tenant_id", "almacen_id", "variante_id", "lote_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_existencias_tenant_id_variante_id",
                schema: "inventario",
                table: "existencias",
                columns: new[] { "tenant_id", "variante_id" });

            migrationBuilder.CreateIndex(
                name: "IX_kardex_almacen_id",
                schema: "inventario",
                table: "kardex",
                column: "almacen_id");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_lote_id",
                schema: "inventario",
                table: "kardex",
                column: "lote_id");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_movimiento_detalle_id",
                schema: "inventario",
                table: "kardex",
                column: "movimiento_detalle_id");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_movimiento_id",
                schema: "inventario",
                table: "kardex",
                column: "movimiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_kardex_tenant_id_almacen_id_variante_id_lote_id_secuencia",
                schema: "inventario",
                table: "kardex",
                columns: new[] { "tenant_id", "almacen_id", "variante_id", "lote_id", "secuencia" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_kardex_tenant_id_variante_id_almacen_id_fecha_secuencia",
                schema: "inventario",
                table: "kardex",
                columns: new[] { "tenant_id", "variante_id", "almacen_id", "fecha", "secuencia" });

            migrationBuilder.CreateIndex(
                name: "IX_lotes_tenant_id_fecha_vencimiento",
                schema: "inventario",
                table: "lotes",
                columns: new[] { "tenant_id", "fecha_vencimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_lotes_tenant_id_variante_id_codigo_lote",
                schema: "inventario",
                table: "lotes",
                columns: new[] { "tenant_id", "variante_id", "codigo_lote" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_detalles_lote_id",
                schema: "inventario",
                table: "movimiento_detalles",
                column: "lote_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_detalles_movimiento_id",
                schema: "inventario",
                table: "movimiento_detalles",
                column: "movimiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_almacen_destino_id",
                schema: "inventario",
                table: "movimientos",
                column: "almacen_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_almacen_origen_id",
                schema: "inventario",
                table: "movimientos",
                column: "almacen_origen_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_tenant_id_fecha",
                schema: "inventario",
                table: "movimientos",
                columns: new[] { "tenant_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_tenant_id_numero",
                schema: "inventario",
                table: "movimientos",
                columns: new[] { "tenant_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_tenant_id_origen_tipo_origen_id",
                schema: "inventario",
                table: "movimientos",
                columns: new[] { "tenant_id", "origen_tipo", "origen_id" });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_tipo_movimiento_id",
                schema: "inventario",
                table: "movimientos",
                column: "tipo_movimiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_tipos_movimiento_tenant_id_codigo",
                schema: "inventario",
                table: "tipos_movimiento",
                columns: new[] { "tenant_id", "codigo" },
                unique: true,
                filter: "eliminado_en IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consecutivos",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "existencias",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "kardex",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "movimiento_detalles",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "lotes",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "movimientos",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "almacenes",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "tipos_movimiento",
                schema: "inventario");
        }
    }
}
