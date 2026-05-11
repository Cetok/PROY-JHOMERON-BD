using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V16_MultipleEncargadosMaquina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaquinaAsignaciones_Empleados_IdEmpleadoEncargado",
                table: "MaquinaAsignaciones");

            migrationBuilder.DropIndex(
                name: "IX_MaquinaAsignaciones_IdMaquina",
                table: "MaquinaAsignaciones");

            migrationBuilder.AlterColumn<int>(
                name: "IdEmpleadoEncargado",
                table: "MaquinaAsignaciones",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "MaquinaAsignacionEncargados",
                columns: table => new
                {
                    IdEncargado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAsignacion = table.Column<int>(type: "int", nullable: false),
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    FechaAgregado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaquinaAsignacionEncargados", x => x.IdEncargado);
                    table.ForeignKey(
                        name: "FK_MaquinaAsignacionEncargados_Empleados_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Empleados",
                        principalColumn: "idEmpleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaquinaAsignacionEncargados_MaquinaAsignaciones_IdAsignacion",
                        column: x => x.IdAsignacion,
                        principalTable: "MaquinaAsignaciones",
                        principalColumn: "IdAsignacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaquinaAsignaciones_IdMaquina",
                table: "MaquinaAsignaciones",
                column: "IdMaquina");

            migrationBuilder.CreateIndex(
                name: "IX_MaquinaAsignacionEncargados_IdAsignacion_IdEmpleado",
                table: "MaquinaAsignacionEncargados",
                columns: new[] { "IdAsignacion", "IdEmpleado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaquinaAsignacionEncargados_IdEmpleado",
                table: "MaquinaAsignacionEncargados",
                column: "IdEmpleado");

            migrationBuilder.AddForeignKey(
                name: "FK_MaquinaAsignaciones_Empleados_IdEmpleadoEncargado",
                table: "MaquinaAsignaciones",
                column: "IdEmpleadoEncargado",
                principalTable: "Empleados",
                principalColumn: "idEmpleado",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaquinaAsignaciones_Empleados_IdEmpleadoEncargado",
                table: "MaquinaAsignaciones");

            migrationBuilder.DropTable(
                name: "MaquinaAsignacionEncargados");

            migrationBuilder.DropIndex(
                name: "IX_MaquinaAsignaciones_IdMaquina",
                table: "MaquinaAsignaciones");

            migrationBuilder.AlterColumn<int>(
                name: "IdEmpleadoEncargado",
                table: "MaquinaAsignaciones",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaquinaAsignaciones_IdMaquina",
                table: "MaquinaAsignaciones",
                column: "IdMaquina",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaquinaAsignaciones_Empleados_IdEmpleadoEncargado",
                table: "MaquinaAsignaciones",
                column: "IdEmpleadoEncargado",
                principalTable: "Empleados",
                principalColumn: "idEmpleado",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
