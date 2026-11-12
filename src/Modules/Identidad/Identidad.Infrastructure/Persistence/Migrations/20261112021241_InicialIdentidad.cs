using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identidad.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialIdentidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identidad");

            migrationBuilder.CreateTable(
                name: "negocios",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_negocios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    es_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    permisos = table.Column<List<string>>(type: "text[]", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_roles_negocios_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "identidad",
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    ultimo_acceso = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_negocios_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "identidad",
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_roles",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_roles_roles_rol_id",
                        column: x => x.rol_id,
                        principalSchema: "identidad",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_roles_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identidad",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_negocios_codigo",
                schema: "identidad",
                table: "negocios",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_tenant_id_nombre",
                schema: "identidad",
                table: "roles",
                columns: new[] { "tenant_id", "nombre" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_tenant_id_email",
                schema: "identidad",
                table: "usuarios",
                columns: new[] { "tenant_id", "email" },
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_roles_rol_id",
                schema: "identidad",
                table: "usuarios_roles",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_roles_usuario_id_rol_id",
                schema: "identidad",
                table: "usuarios_roles",
                columns: new[] { "usuario_id", "rol_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios_roles",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "negocios",
                schema: "identidad");
        }
    }
}
