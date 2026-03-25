using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposExtintor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaVencimientoExtintor",
                table: "Carro_Asesorio",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PesoExtintor",
                table: "Carro_Asesorio",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoExtintor",
                table: "Carro_Asesorio",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaVencimientoExtintor",
                table: "Carro_Asesorio");

            migrationBuilder.DropColumn(
                name: "PesoExtintor",
                table: "Carro_Asesorio");

            migrationBuilder.DropColumn(
                name: "TipoExtintor",
                table: "Carro_Asesorio");
        }
    }
}
