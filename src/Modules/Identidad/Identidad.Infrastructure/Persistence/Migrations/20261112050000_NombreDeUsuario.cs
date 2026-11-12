using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identidad.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Inicio de sesión con nombre de usuario en vez de correo.
    /// - Agrega usuarios.nombre_usuario (obligatorio, único por negocio).
    /// - A los usuarios existentes les asigna lo que va antes de la @ de su correo
    ///   (admin@demo-a.com → admin); si se repite dentro del negocio, le agrega un número.
    /// - El correo pasa a ser opcional.
    /// </summary>
    public partial class NombreDeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "nombre_usuario",
                schema: "identidad",
                table: "usuarios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                WITH base AS (
                    SELECT id, tenant_id, creado_en,
                           left(regexp_replace(lower(split_part(email, '@', 1)), '[^a-z0-9._-]', '', 'g'), 45) AS nombre
                    FROM identidad.usuarios
                ),
                normalizado AS (
                    SELECT id, tenant_id, creado_en,
                           CASE WHEN length(nombre) < 3 THEN 'usuario' ELSE nombre END AS nombre
                    FROM base
                ),
                numerado AS (
                    SELECT id, nombre,
                           row_number() OVER (PARTITION BY tenant_id, nombre ORDER BY creado_en, id) AS n
                    FROM normalizado
                )
                UPDATE identidad.usuarios u
                SET nombre_usuario = CASE WHEN x.n = 1 THEN x.nombre ELSE x.nombre || x.n END
                FROM numerado x
                WHERE u.id = x.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "nombre_usuario",
                schema: "identidad",
                table: "usuarios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "identidad",
                table: "usuarios",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_tenant_id_nombre_usuario",
                schema: "identidad",
                table: "usuarios",
                columns: new[] { "tenant_id", "nombre_usuario" },
                unique: true,
                filter: "eliminado_en IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_usuarios_tenant_id_nombre_usuario",
                schema: "identidad",
                table: "usuarios");

            // El correo vuelve a ser obligatorio: a quien no tenga se le pone uno de relleno
            migrationBuilder.Sql("""
                UPDATE identidad.usuarios
                SET email = nombre_usuario || '@sin-correo.local'
                WHERE email IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "identidad",
                table: "usuarios",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "nombre_usuario",
                schema: "identidad",
                table: "usuarios");
        }
    }
}
