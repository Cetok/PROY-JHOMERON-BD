using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarInspeccionBotiquinGrupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspeccionBotiquinGrupo",
                columns: table => new
                {
                    IdInspeccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdGrupo = table.Column<int>(type: "int", nullable: false),
                    FechaInspeccion = table.Column<DateOnly>(type: "date", nullable: false),
                    Piso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NumeroBotiquin = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstaladoEnPared = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_InspeccionBotiquinGrupo", x => x.IdInspeccion);
                    table.ForeignKey(
                        name: "FK_InspeccionBotiquinGrupo_Grupos_IdGrupo",
                        column: x => x.IdGrupo,
                        principalTable: "Grupos",
                        principalColumn: "idGrupo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspeccionBotiquinGrupoItem",
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
                    table.PrimaryKey("PK_InspeccionBotiquinGrupoItem", x => x.IdItem);
                    table.ForeignKey(
                        name: "FK_InspeccionBotiquinGrupoItem_InspeccionBotiquinGrupo_IdInspeccion",
                        column: x => x.IdInspeccion,
                        principalTable: "InspeccionBotiquinGrupo",
                        principalColumn: "IdInspeccion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspeccionBotiquinGrupo_IdGrupo",
                table: "InspeccionBotiquinGrupo",
                column: "IdGrupo");

            migrationBuilder.CreateIndex(
                name: "IX_InspeccionBotiquinGrupoItem_IdInspeccion",
                table: "InspeccionBotiquinGrupoItem",
                column: "IdInspeccion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspeccionBotiquinGrupoItem");

            migrationBuilder.DropTable(
                name: "InspeccionBotiquinGrupo");
        }
    }
}
