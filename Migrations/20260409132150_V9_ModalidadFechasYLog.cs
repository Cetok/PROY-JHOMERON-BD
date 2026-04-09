using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class V9_ModalidadFechasYLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAsignacion",
                table: "Carro_Modalidad",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Carro_Modalidad",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarroModalidadLog",
                columns: table => new
                {
                    IdLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    IdModalidad = table.Column<int>(type: "int", nullable: false),
                    TipoModalidad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarroModalidadLog", x => x.IdLog);
                    table.ForeignKey(
                        name: "FK_CarroModalidadLog_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarroModalidadLog_IdCarro",
                table: "CarroModalidadLog",
                column: "IdCarro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarroModalidadLog");

            migrationBuilder.DropColumn(
                name: "FechaAsignacion",
                table: "Carro_Modalidad");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Carro_Modalidad");
        }
    }
}
