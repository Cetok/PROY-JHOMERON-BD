using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarInspeccionExtintor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspeccionExtintor",
                columns: table => new
                {
                    IdInspeccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAsesorio = table.Column<int>(type: "int", nullable: false),
                    FechaInspeccion = table.Column<DateOnly>(type: "date", nullable: false),
                    InspeccionadoPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirmaBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspeccionExtintor", x => x.IdInspeccion);
                    table.ForeignKey(
                        name: "FK_InspeccionExtintor_Asesorios_IdAsesorio",
                        column: x => x.IdAsesorio,
                        principalTable: "Asesorios",
                        principalColumn: "IdAsesorio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspeccionExtintorFila",
                columns: table => new
                {
                    IdFila = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdInspeccion = table.Column<int>(type: "int", nullable: false),
                    IdGrupo = table.Column<int>(type: "int", nullable: false),
                    NombreGrupo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoExtintor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PesoExtintor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Comentario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ObservacionesMarcadas = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observacion18 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspeccionExtintorFila", x => x.IdFila);
                    table.ForeignKey(
                        name: "FK_InspeccionExtintorFila_Grupos_IdGrupo",
                        column: x => x.IdGrupo,
                        principalTable: "Grupos",
                        principalColumn: "idGrupo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspeccionExtintorFila_InspeccionExtintor_IdInspeccion",
                        column: x => x.IdInspeccion,
                        principalTable: "InspeccionExtintor",
                        principalColumn: "IdInspeccion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspeccionExtintor_IdAsesorio",
                table: "InspeccionExtintor",
                column: "IdAsesorio");

            migrationBuilder.CreateIndex(
                name: "IX_InspeccionExtintorFila_IdGrupo",
                table: "InspeccionExtintorFila",
                column: "IdGrupo");

            migrationBuilder.CreateIndex(
                name: "IX_InspeccionExtintorFila_IdInspeccion",
                table: "InspeccionExtintorFila",
                column: "IdInspeccion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspeccionExtintorFila");

            migrationBuilder.DropTable(
                name: "InspeccionExtintor");
        }
    }
}
