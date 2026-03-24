using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V5_ConductorLogIMEIMouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsInalambrico",
                table: "Equipos",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IMEI",
                table: "Equipos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroCargo",
                table: "Asignaciones",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarroConductorLog",
                columns: table => new
                {
                    IdLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdEmpleadoAnterior = table.Column<int>(type: "int", nullable: true),
                    NombreConductorAnterior = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdEmpleadoNuevo = table.Column<int>(type: "int", nullable: true),
                    NombreConductorNuevo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarroConductorLog", x => x.IdLog);
                    table.ForeignKey(
                        name: "FK_CarroConductorLog_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarroConductorLog_IdCarro",
                table: "CarroConductorLog",
                column: "IdCarro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarroConductorLog");

            migrationBuilder.DropColumn(
                name: "EsInalambrico",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "IMEI",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "NumeroCargo",
                table: "Asignaciones");
        }
    }
}
