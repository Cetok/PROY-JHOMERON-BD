using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V11_FueEditadoInspecciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FueEditado",
                table: "InspeccionExtintor",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FueEditado",
                table: "InspeccionBotiquinTransporte",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FueEditado",
                table: "InspeccionBotiquinGrupo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FueEditado",
                table: "CheckListTransporte",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FueEditado",
                table: "InspeccionExtintor");

            migrationBuilder.DropColumn(
                name: "FueEditado",
                table: "InspeccionBotiquinTransporte");

            migrationBuilder.DropColumn(
                name: "FueEditado",
                table: "InspeccionBotiquinGrupo");

            migrationBuilder.DropColumn(
                name: "FueEditado",
                table: "CheckListTransporte");
        }
    }
}
