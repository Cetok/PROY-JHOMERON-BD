using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V12_CambiosCarrosDatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FechaCompra",
                table: "Carros",
                newName: "FechaTitulo");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Carros",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaRodante",
                table: "Carros",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HabilitacionVehicular",
                columns: table => new
                {
                    IdHabilitacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    IdModalidad = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaVigencia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCulminacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsVigente = table.Column<bool>(type: "bit", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabilitacionVehicular", x => x.IdHabilitacion);
                    table.ForeignKey(
                        name: "FK_HabilitacionVehicular_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HabilitacionVehicular_Modalidades_IdModalidad",
                        column: x => x.IdModalidad,
                        principalTable: "Modalidades",
                        principalColumn: "IdModalidad",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LunaPolarizada",
                columns: table => new
                {
                    IdLuna = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    FechaVigencia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EsVigente = table.Column<bool>(type: "bit", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LunaPolarizada", x => x.IdLuna);
                    table.ForeignKey(
                        name: "FK_LunaPolarizada_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HabilitacionVehicular_IdCarro",
                table: "HabilitacionVehicular",
                column: "IdCarro");

            migrationBuilder.CreateIndex(
                name: "IX_HabilitacionVehicular_IdModalidad",
                table: "HabilitacionVehicular",
                column: "IdModalidad");

            migrationBuilder.CreateIndex(
                name: "IX_LunaPolarizada_IdCarro",
                table: "LunaPolarizada",
                column: "IdCarro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabilitacionVehicular");

            migrationBuilder.DropTable(
                name: "LunaPolarizada");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Carros");

            migrationBuilder.DropColumn(
                name: "FormulaRodante",
                table: "Carros");

            migrationBuilder.RenameColumn(
                name: "FechaTitulo",
                table: "Carros",
                newName: "FechaCompra");
        }
    }
}
