using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V18_AccesoriosCambiosYFiltros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaquinaAccesorioCambios",
                columns: table => new
                {
                    IdCambio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdMaquina = table.Column<int>(type: "int", nullable: false),
                    NombreAccesorio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccesorioAnterior = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccesorioNuevo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaquinaAccesorioCambios", x => x.IdCambio);
                    table.ForeignKey(
                        name: "FK_MaquinaAccesorioCambios_Maquinas_IdMaquina",
                        column: x => x.IdMaquina,
                        principalTable: "Maquinas",
                        principalColumn: "IdMaquina",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaquinaAccesorioCambios_IdMaquina",
                table: "MaquinaAccesorioCambios",
                column: "IdMaquina");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaquinaAccesorioCambios");
        }
    }
}
