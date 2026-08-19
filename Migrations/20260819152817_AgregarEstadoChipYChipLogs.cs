using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoChipYChipLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Chips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ChipLogs",
                columns: table => new
                {
                    IdLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdChip = table.Column<int>(type: "int", nullable: false),
                    TipoEvento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistradoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdUsuario = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChipLogs", x => x.IdLog);
                    table.ForeignKey(
                        name: "FK_ChipLogs_Chips_IdChip",
                        column: x => x.IdChip,
                        principalTable: "Chips",
                        principalColumn: "IdChip",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChipLogs_IdChip",
                table: "ChipLogs",
                column: "IdChip");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChipLogs");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Chips");
        }
    }
}
