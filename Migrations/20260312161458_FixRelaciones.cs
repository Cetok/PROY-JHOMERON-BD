using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class FixRelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Equipos_EquipoidEquipo",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipos_TiposEquipo_TipoEquipoidTipoEquipo",
                table: "Equipos");

            migrationBuilder.DropIndex(
                name: "IX_Equipos_TipoEquipoidTipoEquipo",
                table: "Equipos");

            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_EquipoidEquipo",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "TipoEquipoidTipoEquipo",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "EquipoidEquipo",
                table: "Asignaciones");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_idTipoEquipo",
                table: "Equipos",
                column: "idTipoEquipo");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipos_TiposEquipo_idTipoEquipo",
                table: "Equipos",
                column: "idTipoEquipo",
                principalTable: "TiposEquipo",
                principalColumn: "idTipoEquipo",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipos_TiposEquipo_idTipoEquipo",
                table: "Equipos");

            migrationBuilder.DropIndex(
                name: "IX_Equipos_idTipoEquipo",
                table: "Equipos");

            migrationBuilder.AddColumn<int>(
                name: "TipoEquipoidTipoEquipo",
                table: "Equipos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EquipoidEquipo",
                table: "Asignaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_TipoEquipoidTipoEquipo",
                table: "Equipos",
                column: "TipoEquipoidTipoEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_EquipoidEquipo",
                table: "Asignaciones",
                column: "EquipoidEquipo");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Equipos_EquipoidEquipo",
                table: "Asignaciones",
                column: "EquipoidEquipo",
                principalTable: "Equipos",
                principalColumn: "idEquipo");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipos_TiposEquipo_TipoEquipoidTipoEquipo",
                table: "Equipos",
                column: "TipoEquipoidTipoEquipo",
                principalTable: "TiposEquipo",
                principalColumn: "idTipoEquipo");
        }
    }
}
