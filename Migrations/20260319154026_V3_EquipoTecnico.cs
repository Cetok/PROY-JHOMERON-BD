using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V3_EquipoTecnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "version",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sistema_operativo",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "numero_serie",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "modelo",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "marca",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Disco",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuenteEnergia",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GraficosIntegrados",
                table: "Equipos",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Equipos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Procesador",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ram",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TarjetaGrafica",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TarjetaMadre",
                table: "Equipos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EquipoComponenteLogs",
                columns: table => new
                {
                    IdLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEquipo = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoEvento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Componente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValorAnterior = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ValorNuevo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipoComponenteLogs", x => x.IdLog);
                    table.ForeignKey(
                        name: "FK_EquipoComponenteLogs_Equipos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipos",
                        principalColumn: "idEquipo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipoComponenteLogs_IdEquipo",
                table: "EquipoComponenteLogs",
                column: "IdEquipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipoComponenteLogs");

            migrationBuilder.DropColumn(
                name: "Disco",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "FuenteEnergia",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "GraficosIntegrados",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "Procesador",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "Ram",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "TarjetaGrafica",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "TarjetaMadre",
                table: "Equipos");

            migrationBuilder.AlterColumn<string>(
                name: "version",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sistema_operativo",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "numero_serie",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "modelo",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "marca",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
