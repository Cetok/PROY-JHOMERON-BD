using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V6_TipoDocumentoPcCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PcCpuDisco",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuFuenteEnergia",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PcCpuGraficosIntegrados",
                table: "Equipos",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuMarca",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuModelo",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuProcesador",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuRam",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuSerie",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuSistemaOperativo",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuTarjetaGrafica",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuTarjetaMadre",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcCpuVersionSO",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcMonitorMarca",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcMonitorModelo",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcMonitorSerie",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PcMouseEsInalambrico",
                table: "Equipos",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcMouseMarca",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcMouseModelo",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcMouseSerie",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcMousepadMarca",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcTecladoMarca",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcTecladoModelo",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PcTecladoSerie",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Empleados",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PcCpuDisco",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuFuenteEnergia",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuGraficosIntegrados",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuMarca",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuModelo",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuProcesador",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuRam",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuSerie",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuSistemaOperativo",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuTarjetaGrafica",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuTarjetaMadre",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcCpuVersionSO",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcMonitorMarca",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcMonitorModelo",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcMonitorSerie",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcMouseEsInalambrico",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcMouseMarca",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcMouseModelo",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcMouseSerie",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcMousepadMarca",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcTecladoMarca",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcTecladoModelo",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "PcTecladoSerie",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Empleados");
        }
    }
}
