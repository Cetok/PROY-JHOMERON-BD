using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCheckListTransporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckListTransporte",
                columns: table => new
                {
                    IdCheckList = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    FechaInspeccion = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraInspeccion = table.Column<TimeOnly>(type: "time", nullable: false),
                    SedeArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreResponsable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirmaBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObservacionesGenerales = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckListTransporte", x => x.IdCheckList);
                    table.ForeignKey(
                        name: "FK_CheckListTransporte_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheckListTransporteItem",
                columns: table => new
                {
                    IdItem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCheckList = table.Column<int>(type: "int", nullable: false),
                    Seccion = table.Column<int>(type: "int", nullable: false),
                    NombreSeccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Elemento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Cumple = table.Column<bool>(type: "bit", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckListTransporteItem", x => x.IdItem);
                    table.ForeignKey(
                        name: "FK_CheckListTransporteItem_CheckListTransporte_IdCheckList",
                        column: x => x.IdCheckList,
                        principalTable: "CheckListTransporte",
                        principalColumn: "IdCheckList",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckListTransporte_IdCarro",
                table: "CheckListTransporte",
                column: "IdCarro");

            migrationBuilder.CreateIndex(
                name: "IX_CheckListTransporteItem_IdCheckList",
                table: "CheckListTransporteItem",
                column: "IdCheckList");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckListTransporteItem");

            migrationBuilder.DropTable(
                name: "CheckListTransporte");
        }
    }
}
