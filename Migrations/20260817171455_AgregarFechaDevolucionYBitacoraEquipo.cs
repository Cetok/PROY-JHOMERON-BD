using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFechaDevolucionYBitacoraEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDevolucion",
                table: "Asignaciones",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EquipoBitacora",
                columns: table => new
                {
                    IdBitacora = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEquipo = table.Column<int>(type: "int", nullable: false),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsProgramado = table.Column<bool>(type: "bit", nullable: false),
                    Completado = table.Column<bool>(type: "bit", nullable: false),
                    RegistradoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipoBitacora", x => x.IdBitacora);
                    table.ForeignKey(
                        name: "FK_EquipoBitacora_Equipos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipos",
                        principalColumn: "idEquipo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipoBitacora_IdEquipo",
                table: "EquipoBitacora",
                column: "IdEquipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipoBitacora");

            migrationBuilder.DropColumn(
                name: "FechaDevolucion",
                table: "Asignaciones");
        }
    }
}
