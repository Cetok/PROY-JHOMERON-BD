using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarGrupoEnAsignacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NumeroCargo",
                table: "Asignaciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdGrupo",
                table: "Asignaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_IdGrupo",
                table: "Asignaciones",
                column: "IdGrupo");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Grupos_IdGrupo",
                table: "Asignaciones",
                column: "IdGrupo",
                principalTable: "Grupos",
                principalColumn: "idGrupo",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Grupos_IdGrupo",
                table: "Asignaciones");

            migrationBuilder.DropIndex(
                name: "IX_Asignaciones_IdGrupo",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "IdGrupo",
                table: "Asignaciones");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCargo",
                table: "Asignaciones",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
