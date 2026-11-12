using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ventas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CajaYGastos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "sesion_caja_id",
                schema: "ventas",
                table: "ventas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vendedor_nombre",
                schema: "ventas",
                table: "ventas",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registrado_por_nombre",
                schema: "ventas",
                table: "abonos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sesion_caja_id",
                schema: "ventas",
                table: "abonos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categorias_gasto",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
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
                    table.PrimaryKey("PK_categorias_gasto", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sesiones_caja",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    abierta_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    abierta_por_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    monto_inicial = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    observacion_apertura = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    cerrada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cerrada_por = table.Column<Guid>(type: "uuid", nullable: true),
                    cerrada_por_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    efectivo_esperado = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    efectivo_contado = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    diferencia = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    observacion_cierre = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiones_caja", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gastos",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    metodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    proveedor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    sesion_caja_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registrado_por_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    anulado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gastos", x => x.id);
                    table.ForeignKey(
                        name: "FK_gastos_categorias_gasto_categoria_id",
                        column: x => x.categoria_id,
                        principalSchema: "ventas",
                        principalTable: "categorias_gasto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gastos_sesiones_caja_sesion_caja_id",
                        column: x => x.sesion_caja_id,
                        principalSchema: "ventas",
                        principalTable: "sesiones_caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_caja",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_caja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    documento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_caja", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_sesiones_caja_sesion_caja_id",
                        column: x => x.sesion_caja_id,
                        principalSchema: "ventas",
                        principalTable: "sesiones_caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_sesion_caja_id",
                schema: "ventas",
                table: "ventas",
                column: "sesion_caja_id");

            migrationBuilder.CreateIndex(
                name: "IX_abonos_sesion_caja_id",
                schema: "ventas",
                table: "abonos",
                column: "sesion_caja_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_gasto_tenant_id_nombre",
                schema: "ventas",
                table: "categorias_gasto",
                columns: new[] { "tenant_id", "nombre" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_gastos_categoria_id",
                schema: "ventas",
                table: "gastos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_gastos_sesion_caja_id",
                schema: "ventas",
                table: "gastos",
                column: "sesion_caja_id");

            migrationBuilder.CreateIndex(
                name: "IX_gastos_tenant_id_fecha",
                schema: "ventas",
                table: "gastos",
                columns: new[] { "tenant_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_gastos_tenant_id_numero",
                schema: "ventas",
                table: "gastos",
                columns: new[] { "tenant_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_sesion_caja_id_fecha",
                schema: "ventas",
                table: "movimientos_caja",
                columns: new[] { "sesion_caja_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_caja_tenant_id_abierta_en",
                schema: "ventas",
                table: "sesiones_caja",
                columns: new[] { "tenant_id", "abierta_en" });

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_caja_tenant_id_numero",
                schema: "ventas",
                table: "sesiones_caja",
                columns: new[] { "tenant_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_sesiones_caja_abierta",
                schema: "ventas",
                table: "sesiones_caja",
                column: "tenant_id",
                unique: true,
                filter: "estado = 'Abierta'");

            migrationBuilder.AddForeignKey(
                name: "FK_abonos_sesiones_caja_sesion_caja_id",
                schema: "ventas",
                table: "abonos",
                column: "sesion_caja_id",
                principalSchema: "ventas",
                principalTable: "sesiones_caja",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_sesiones_caja_sesion_caja_id",
                schema: "ventas",
                table: "ventas",
                column: "sesion_caja_id",
                principalSchema: "ventas",
                principalTable: "sesiones_caja",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_abonos_sesiones_caja_sesion_caja_id",
                schema: "ventas",
                table: "abonos");

            migrationBuilder.DropForeignKey(
                name: "FK_ventas_sesiones_caja_sesion_caja_id",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropTable(
                name: "gastos",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "movimientos_caja",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "categorias_gasto",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "sesiones_caja",
                schema: "ventas");

            migrationBuilder.DropIndex(
                name: "IX_ventas_sesion_caja_id",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropIndex(
                name: "IX_abonos_sesion_caja_id",
                schema: "ventas",
                table: "abonos");

            migrationBuilder.DropColumn(
                name: "sesion_caja_id",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "vendedor_nombre",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "registrado_por_nombre",
                schema: "ventas",
                table: "abonos");

            migrationBuilder.DropColumn(
                name: "sesion_caja_id",
                schema: "ventas",
                table: "abonos");
        }
    }
}
