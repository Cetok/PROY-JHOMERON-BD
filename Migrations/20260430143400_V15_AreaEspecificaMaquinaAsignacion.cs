using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V15_AreaEspecificaMaquinaAsignacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreaEspecifica",
                table: "MaquinaAsignaciones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaEspecifica",
                table: "MaquinaAsignaciones");
        }
    }
}
