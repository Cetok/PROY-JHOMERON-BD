using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V21_IAGenerativa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IAConversaciones",
                columns: table => new
                {
                    IdConversacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimoMensaje = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsActiva = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IAConversaciones", x => x.IdConversacion);
                    table.ForeignKey(
                        name: "FK_IAConversaciones_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "idUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IAMensajes",
                columns: table => new
                {
                    IdMensaje = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdConversacion = table.Column<int>(type: "int", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GraficoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recomendacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TieneExportacion = table.Column<bool>(type: "bit", nullable: false),
                    DatosExportacionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IAMensajes", x => x.IdMensaje);
                    table.ForeignKey(
                        name: "FK_IAMensajes_IAConversaciones_IdConversacion",
                        column: x => x.IdConversacion,
                        principalTable: "IAConversaciones",
                        principalColumn: "IdConversacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IAConversaciones_IdUsuario",
                table: "IAConversaciones",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_IAMensajes_IdConversacion",
                table: "IAMensajes",
                column: "IdConversacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IAMensajes");

            migrationBuilder.DropTable(
                name: "IAConversaciones");
        }
    }
}
