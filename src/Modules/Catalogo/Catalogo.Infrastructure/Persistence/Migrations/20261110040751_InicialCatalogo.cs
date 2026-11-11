using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalogo");

            migrationBuilder.CreateTable(
                name: "categorias",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_padre_id = table.Column<Guid>(type: "uuid", nullable: true),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_categorias", x => x.id);
                    table.ForeignKey(
                        name: "FK_categorias_categorias_categoria_padre_id",
                        column: x => x.categoria_padre_id,
                        principalSchema: "catalogo",
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "marcas",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_marcas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unidades_medida",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    permite_decimales = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_unidades_medida", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    marca_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unidad_base_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    maneja_inventario = table.Column<bool>(type: "boolean", nullable: false),
                    maneja_lotes = table.Column<bool>(type: "boolean", nullable: false),
                    maneja_vencimiento = table.Column<bool>(type: "boolean", nullable: false),
                    maneja_seriales = table.Column<bool>(type: "boolean", nullable: false),
                    permite_stock_negativo = table.Column<bool>(type: "boolean", nullable: true),
                    impuesto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    atributos = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_productos", x => x.id);
                    table.ForeignKey(
                        name: "FK_productos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalSchema: "catalogo",
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_marcas_marca_id",
                        column: x => x.marca_id,
                        principalSchema: "catalogo",
                        principalTable: "marcas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_unidades_medida_unidad_base_id",
                        column: x => x.unidad_base_id,
                        principalSchema: "catalogo",
                        principalTable: "unidades_medida",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "variantes",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    atributos = table.Column<string>(type: "jsonb", nullable: false),
                    es_default = table.Column<bool>(type: "boolean", nullable: false),
                    costo_estandar = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    precio_base = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    stock_minimo = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    stock_maximo = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("PK_variantes", x => x.id);
                    table.ForeignKey(
                        name: "FK_variantes_productos_producto_id",
                        column: x => x.producto_id,
                        principalSchema: "catalogo",
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "presentaciones",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    variante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factor = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presentaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_presentaciones_unidades_medida_unidad_id",
                        column: x => x.unidad_id,
                        principalSchema: "catalogo",
                        principalTable: "unidades_medida",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_presentaciones_variantes_variante_id",
                        column: x => x.variante_id,
                        principalSchema: "catalogo",
                        principalTable: "variantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_categoria_padre_id",
                schema: "catalogo",
                table: "categorias",
                column: "categoria_padre_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_tenant_id_codigo",
                schema: "catalogo",
                table: "categorias",
                columns: new[] { "tenant_id", "codigo" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_marcas_tenant_id_nombre",
                schema: "catalogo",
                table: "marcas",
                columns: new[] { "tenant_id", "nombre" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_presentaciones_unidad_id",
                schema: "catalogo",
                table: "presentaciones",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_presentaciones_variante_id_unidad_id",
                schema: "catalogo",
                table: "presentaciones",
                columns: new[] { "variante_id", "unidad_id" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_productos_atributos",
                schema: "catalogo",
                table: "productos",
                column: "atributos")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_productos_categoria_id",
                schema: "catalogo",
                table: "productos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_marca_id",
                schema: "catalogo",
                table: "productos",
                column: "marca_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_tenant_id_categoria_id",
                schema: "catalogo",
                table: "productos",
                columns: new[] { "tenant_id", "categoria_id" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_tenant_id_nombre",
                schema: "catalogo",
                table: "productos",
                columns: new[] { "tenant_id", "nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_unidad_base_id",
                schema: "catalogo",
                table: "productos",
                column: "unidad_base_id");

            migrationBuilder.CreateIndex(
                name: "IX_unidades_medida_tenant_id_codigo",
                schema: "catalogo",
                table: "unidades_medida",
                columns: new[] { "tenant_id", "codigo" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_variantes_tenant_id_codigo_barras",
                schema: "catalogo",
                table: "variantes",
                columns: new[] { "tenant_id", "codigo_barras" },
                unique: true,
                filter: "eliminado_en IS NULL AND codigo_barras IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_variantes_tenant_id_sku",
                schema: "catalogo",
                table: "variantes",
                columns: new[] { "tenant_id", "sku" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_variantes_default",
                schema: "catalogo",
                table: "variantes",
                column: "producto_id",
                unique: true,
                filter: "es_default AND eliminado_en IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "presentaciones",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "variantes",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "productos",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "categorias",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "marcas",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "unidades_medida",
                schema: "catalogo");
        }
    }
}
