using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarInspeccionBotiquinTransporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspeccionBotiquinTransporte",
                columns: table => new
                {
                    IdInspeccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    FechaInspeccion = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroBotiquin = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UbicadoEnSuLugar = table.Column<bool>(type: "bit", nullable: false),
                    LocalizadoVisible = table.Column<bool>(type: "bit", nullable: false),
                    LibreDeObstaculos = table.Column<bool>(type: "bit", nullable: false),
                    Senalizado = table.Column<bool>(type: "bit", nullable: false),
                    InspeccionadoPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirmaBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspeccionBotiquinTransporte", x => x.IdInspeccion);
                    table.ForeignKey(
                        name: "FK_InspeccionBotiquinTransporte_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspeccionBotiquinTransporteItem",
                columns: table => new
                {
                    IdItem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdInspeccion = table.Column<int>(type: "int", nullable: false),
                    Elemento = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SeEncuentra = table.Column<bool>(type: "bit", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspeccionBotiquinTransporteItem", x => x.IdItem);
                    table.ForeignKey(
                        name: "FK_InspeccionBotiquinTransporteItem_InspeccionBotiquinTransporte_IdInspeccion",
                        column: x => x.IdInspeccion,
                        principalTable: "InspeccionBotiquinTransporte",
                        principalColumn: "IdInspeccion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspeccionBotiquinTransporte_IdCarro",
                table: "InspeccionBotiquinTransporte",
                column: "IdCarro");

            migrationBuilder.CreateIndex(
                name: "IX_InspeccionBotiquinTransporteItem_IdInspeccion",
                table: "InspeccionBotiquinTransporteItem",
                column: "IdInspeccion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspeccionBotiquinTransporteItem");

            migrationBuilder.DropTable(
                name: "InspeccionBotiquinTransporte");
        }
    }
}
