using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIdEquipoPadreYMousepad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdEquipoPadre",
                table: "Equipos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MousepadMarca",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TieneMousepad",
                table: "Equipos",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_IdEquipoPadre",
                table: "Equipos",
                column: "IdEquipoPadre");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipos_Equipos_IdEquipoPadre",
                table: "Equipos",
                column: "IdEquipoPadre",
                principalTable: "Equipos",
                principalColumn: "idEquipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipos_Equipos_IdEquipoPadre",
                table: "Equipos");

            migrationBuilder.DropIndex(
                name: "IX_Equipos_IdEquipoPadre",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "IdEquipoPadre",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "MousepadMarca",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "TieneMousepad",
                table: "Equipos");
        }
    }
}
