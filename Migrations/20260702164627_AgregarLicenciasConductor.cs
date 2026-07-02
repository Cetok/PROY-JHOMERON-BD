using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLicenciasConductor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaseLicencia",
                table: "Empleados_Carros",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaseLicenciaEspecial",
                table: "Empleados_Carros",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LicenciaEmision",
                table: "Empleados_Carros",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LicenciaEspecialEmision",
                table: "Empleados_Carros",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LicenciaEspecialExpiracion",
                table: "Empleados_Carros",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LicenciaExpiracion",
                table: "Empleados_Carros",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TieneLicencia",
                table: "Empleados_Carros",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneLicenciaEspecial",
                table: "Empleados_Carros",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaseLicencia",
                table: "Empleados_Carros");

            migrationBuilder.DropColumn(
                name: "ClaseLicenciaEspecial",
                table: "Empleados_Carros");

            migrationBuilder.DropColumn(
                name: "LicenciaEmision",
                table: "Empleados_Carros");

            migrationBuilder.DropColumn(
                name: "LicenciaEspecialEmision",
                table: "Empleados_Carros");

            migrationBuilder.DropColumn(
                name: "LicenciaEspecialExpiracion",
                table: "Empleados_Carros");

            migrationBuilder.DropColumn(
                name: "LicenciaExpiracion",
                table: "Empleados_Carros");

            migrationBuilder.DropColumn(
                name: "TieneLicencia",
                table: "Empleados_Carros");

            migrationBuilder.DropColumn(
                name: "TieneLicenciaEspecial",
                table: "Empleados_Carros");
        }
    }
}
