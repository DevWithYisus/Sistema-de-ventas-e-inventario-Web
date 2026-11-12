using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ventas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClientesCreditoPuntos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "cliente_id",
                schema: "ventas",
                table: "ventas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "puntos_ganados",
                schema: "ventas",
                table: "ventas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "puntos_redimidos",
                schema: "ventas",
                table: "ventas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "es_obsequio",
                schema: "ventas",
                table: "venta_detalles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "recibido",
                schema: "ventas",
                table: "pagos",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    complemento_direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    es_general = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    cupo_credito = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    saldo_deuda = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    saldo_puntos = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuracion",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntos_activo = table.Column<bool>(type: "boolean", nullable: false),
                    monto_base_puntos = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    puntos_por_monto_base = table.Column<int>(type: "integer", nullable: false),
                    valor_punto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    nombre_comercial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    mensaje_pie = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ancho_tirilla_mm = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abonos",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    metodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    observacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    puntos_ganados = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_abonos", x => x.id);
                    table.ForeignKey(
                        name: "FK_abonos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "ventas",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_cliente",
                schema: "ventas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    abono_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deuda = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    puntos = table.Column<int>(type: "integer", nullable: false),
                    saldo_deuda = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    saldo_puntos = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_cliente", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_cliente_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "ventas",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_cliente_id",
                schema: "ventas",
                table: "ventas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_tenant_id_cliente_id_fecha",
                schema: "ventas",
                table: "ventas",
                columns: new[] { "tenant_id", "cliente_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_abonos_cliente_id_fecha",
                schema: "ventas",
                table: "abonos",
                columns: new[] { "cliente_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_abonos_tenant_id_numero",
                schema: "ventas",
                table: "abonos",
                columns: new[] { "tenant_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clientes_tenant_id_documento",
                schema: "ventas",
                table: "clientes",
                columns: new[] { "tenant_id", "documento" },
                unique: true,
                filter: "eliminado_en IS NULL AND documento IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_clientes_tenant_id_nombre",
                schema: "ventas",
                table: "clientes",
                columns: new[] { "tenant_id", "nombre" });

            migrationBuilder.CreateIndex(
                name: "ux_clientes_general",
                schema: "ventas",
                table: "clientes",
                column: "tenant_id",
                unique: true,
                filter: "es_general AND eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_tenant_id",
                schema: "ventas",
                table: "configuracion",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_cliente_cliente_id_fecha",
                schema: "ventas",
                table: "movimientos_cliente",
                columns: new[] { "cliente_id", "fecha" });

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_clientes_cliente_id",
                schema: "ventas",
                table: "ventas",
                column: "cliente_id",
                principalSchema: "ventas",
                principalTable: "clientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ventas_clientes_cliente_id",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropTable(
                name: "abonos",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "configuracion",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "movimientos_cliente",
                schema: "ventas");

            migrationBuilder.DropTable(
                name: "clientes",
                schema: "ventas");

            migrationBuilder.DropIndex(
                name: "IX_ventas_cliente_id",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropIndex(
                name: "IX_ventas_tenant_id_cliente_id_fecha",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "cliente_id",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "puntos_ganados",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "puntos_redimidos",
                schema: "ventas",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "es_obsequio",
                schema: "ventas",
                table: "venta_detalles");

            migrationBuilder.DropColumn(
                name: "recibido",
                schema: "ventas",
                table: "pagos");
        }
    }
}
