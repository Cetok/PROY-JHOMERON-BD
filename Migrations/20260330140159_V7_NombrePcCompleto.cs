using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V7_NombrePcCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombrePc",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombrePc",
                table: "Equipos");
        }
    }
}
