using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalogo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoriaObsequios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_para_obsequios",
                schema: "catalogo",
                table: "categorias",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "es_para_obsequios",
                schema: "catalogo",
                table: "categorias");
        }
    }
}
