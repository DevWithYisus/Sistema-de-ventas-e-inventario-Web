using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ventas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ventas");

            migrationBuilder.CreateTable(
                name: "consecutivos",
                schema: "ventas",
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
                name: "ventas",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    almacen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cliente_documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    descuento_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    impuesto_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    movimiento_inventario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anulada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    movimiento_anulacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pagos",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos", x => x.id);
                    table.ForeignKey(
                        name: "FK_pagos_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "ventas",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "venta_detalles",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    descuento_porcentaje = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    impuesto_porcentaje = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    impuesto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    afecta_inventario = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venta_detalles", x => x.id);
                    table.ForeignKey(
                        name: "FK_venta_detalles_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "ventas",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consecutivos_tenant_id_clave",
                schema: "ventas",
                table: "consecutivos",
                columns: new[] { "tenant_id", "clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_venta_id",
                schema: "ventas",
                table: "pagos",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_venta_detalles_tenant_id_variante_id",
                schema: "ventas",
                table: "venta_detalles",
                columns: new[] { "tenant_id", "variante_id" });

            migrationBuilder.CreateIndex(
                name: "IX_venta_detalles_venta_id",
                schema: "ventas",
                table: "venta_detalles",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_tenant_id_estado_fecha",
                schema: "ventas",
                table: "ventas",
                columns: new[] { "tenant_id", "estado", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_tenant_id_fecha",
                schema: "ventas",
                table: "ventas",
                columns: new[] { "tenant_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_tenant_id_numero",
                schema: "ventas",
                table: "ventas",
                columns: new[] { "tenant_id", "numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consecutivos",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "pagos",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "venta_detalles",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "ventas",
                schema: "ventas");
        }
    }
}
