using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROYJHOME2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNuevosModelos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipos_TipoEquipos_idTipoEquipo",
                table: "Equipos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TipoEquipos",
                table: "TipoEquipos");

            migrationBuilder.RenameTable(
                name: "TipoEquipos",
                newName: "TiposEquipo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TiposEquipo",
                table: "TiposEquipo",
                column: "idTipoEquipo");

            migrationBuilder.CreateTable(
                name: "Asesorios",
                columns: table => new
                {
                    IdAsesorio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoAsesorio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asesorios", x => x.IdAsesorio);
                });

            migrationBuilder.CreateTable(
                name: "Carros",
                columns: table => new
                {
                    IdCarro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Placa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroMotor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaCarro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCompra = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Peso = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Ejes = table.Column<int>(type: "int", nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CargaUtil = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carros", x => x.IdCarro);
                });

            migrationBuilder.CreateTable(
                name: "Chips",
                columns: table => new
                {
                    IdChip = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroCelular = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chips", x => x.IdChip);
                });

            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    idGrupo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    area = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.idGrupo);
                });

            migrationBuilder.CreateTable(
                name: "Modalidades",
                columns: table => new
                {
                    IdModalidad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoModalidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaVigente = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modalidades", x => x.IdModalidad);
                });

            migrationBuilder.CreateTable(
                name: "Motivos",
                columns: table => new
                {
                    IdMotivo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoMotivo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motivos", x => x.IdMotivo);
                });

            migrationBuilder.CreateTable(
                name: "Seguro",
                columns: table => new
                {
                    IdSeguro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoSeguro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seguro", x => x.IdSeguro);
                });

            migrationBuilder.CreateTable(
                name: "TipoMantenimiento",
                columns: table => new
                {
                    IdTipoMante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoMantenimiento", x => x.IdTipoMante);
                });

            migrationBuilder.CreateTable(
                name: "Carro_Asesorio",
                columns: table => new
                {
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    IdAsesorio = table.Column<int>(type: "int", nullable: false),
                    FechaAsignada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carro_Asesorio", x => new { x.IdCarro, x.IdAsesorio });
                    table.ForeignKey(
                        name: "FK_Carro_Asesorio_Asesorios_IdAsesorio",
                        column: x => x.IdAsesorio,
                        principalTable: "Asesorios",
                        principalColumn: "IdAsesorio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Carro_Asesorio_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Empleados_Carros",
                columns: table => new
                {
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    IdCarro = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados_Carros", x => new { x.IdEmpleado, x.IdCarro });
                    table.ForeignKey(
                        name: "FK_Empleados_Carros_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Empleados_Carros_Empleados_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Empleados",
                        principalColumn: "idEmpleado",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asignaciones",
                columns: table => new
                {
                    IdAsignacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    IdEquipo = table.Column<int>(type: "int", nullable: false),
                    IdChip = table.Column<int>(type: "int", nullable: true),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaDevolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstadoAsignacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignaciones", x => x.IdAsignacion);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Chips_IdChip",
                        column: x => x.IdChip,
                        principalTable: "Chips",
                        principalColumn: "IdChip",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Empleados_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Empleados",
                        principalColumn: "idEmpleado",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Equipos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipos",
                        principalColumn: "idEquipo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Empleado_Grupo",
                columns: table => new
                {
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    IdGrupo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleado_Grupo", x => new { x.IdEmpleado, x.IdGrupo });
                    table.ForeignKey(
                        name: "FK_Empleado_Grupo_Empleados_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Empleados",
                        principalColumn: "idEmpleado",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Empleado_Grupo_Grupos_IdGrupo",
                        column: x => x.IdGrupo,
                        principalTable: "Grupos",
                        principalColumn: "idGrupo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Grupo_Asesorio",
                columns: table => new
                {
                    IdGrupo = table.Column<int>(type: "int", nullable: false),
                    IdAsesorio = table.Column<int>(type: "int", nullable: false),
                    FechaAsignada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupo_Asesorio", x => new { x.IdGrupo, x.IdAsesorio });
                    table.ForeignKey(
                        name: "FK_Grupo_Asesorio_Asesorios_IdAsesorio",
                        column: x => x.IdAsesorio,
                        principalTable: "Asesorios",
                        principalColumn: "IdAsesorio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Grupo_Asesorio_Grupos_IdGrupo",
                        column: x => x.IdGrupo,
                        principalTable: "Grupos",
                        principalColumn: "idGrupo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Carro_Modalidad",
                columns: table => new
                {
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    IdModalidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carro_Modalidad", x => new { x.IdCarro, x.IdModalidad });
                    table.ForeignKey(
                        name: "FK_Carro_Modalidad_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Carro_Modalidad_Modalidades_IdModalidad",
                        column: x => x.IdModalidad,
                        principalTable: "Modalidades",
                        principalColumn: "IdModalidad",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Carro_Seguro",
                columns: table => new
                {
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    IdSeguro = table.Column<int>(type: "int", nullable: false),
                    FechaAsignada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCulminada = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carro_Seguro", x => new { x.IdCarro, x.IdSeguro });
                    table.ForeignKey(
                        name: "FK_Carro_Seguro_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Carro_Seguro_Seguro_IdSeguro",
                        column: x => x.IdSeguro,
                        principalTable: "Seguro",
                        principalColumn: "IdSeguro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Empleado_Seguro",
                columns: table => new
                {
                    IdEmpleado = table.Column<int>(type: "int", nullable: false),
                    IdSeguro = table.Column<int>(type: "int", nullable: false),
                    FechaAsignada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCulminada = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleado_Seguro", x => new { x.IdEmpleado, x.IdSeguro });
                    table.ForeignKey(
                        name: "FK_Empleado_Seguro_Empleados_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Empleados",
                        principalColumn: "idEmpleado",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Empleado_Seguro_Seguro_IdSeguro",
                        column: x => x.IdSeguro,
                        principalTable: "Seguro",
                        principalColumn: "IdSeguro",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mantenimiento_carro",
                columns: table => new
                {
                    IdMante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTipoMante = table.Column<int>(type: "int", nullable: false),
                    FechaMante = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdCarro = table.Column<int>(type: "int", nullable: false),
                    FechaCulminada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mantenimiento_carro", x => x.IdMante);
                    table.ForeignKey(
                        name: "FK_Mantenimiento_carro_Carros_IdCarro",
                        column: x => x.IdCarro,
                        principalTable: "Carros",
                        principalColumn: "IdCarro",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mantenimiento_carro_TipoMantenimiento_IdTipoMante",
                        column: x => x.IdTipoMante,
                        principalTable: "TipoMantenimiento",
                        principalColumn: "IdTipoMante",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Historiales",
                columns: table => new
                {
                    IdHistoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAsignacion = table.Column<int>(type: "int", nullable: false),
                    IdMotivo = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Historiales", x => x.IdHistoria);
                    table.ForeignKey(
                        name: "FK_Historiales_Asignaciones_IdAsignacion",
                        column: x => x.IdAsignacion,
                        principalTable: "Asignaciones",
                        principalColumn: "IdAsignacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Historiales_Motivos_IdMotivo",
                        column: x => x.IdMotivo,
                        principalTable: "Motivos",
                        principalColumn: "IdMotivo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_IdChip",
                table: "Asignaciones",
                column: "IdChip");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_IdEmpleado",
                table: "Asignaciones",
                column: "IdEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_IdEquipo",
                table: "Asignaciones",
                column: "IdEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Carro_Asesorio_IdAsesorio",
                table: "Carro_Asesorio",
                column: "IdAsesorio");

            migrationBuilder.CreateIndex(
                name: "IX_Carro_Modalidad_IdModalidad",
                table: "Carro_Modalidad",
                column: "IdModalidad");

            migrationBuilder.CreateIndex(
                name: "IX_Carro_Seguro_IdSeguro",
                table: "Carro_Seguro",
                column: "IdSeguro");

            migrationBuilder.CreateIndex(
                name: "IX_Empleado_Grupo_IdGrupo",
                table: "Empleado_Grupo",
                column: "IdGrupo");

            migrationBuilder.CreateIndex(
                name: "IX_Empleado_Seguro_IdSeguro",
                table: "Empleado_Seguro",
                column: "IdSeguro");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Carros_IdCarro",
                table: "Empleados_Carros",
                column: "IdCarro");

            migrationBuilder.CreateIndex(
                name: "IX_Grupo_Asesorio_IdAsesorio",
                table: "Grupo_Asesorio",
                column: "IdAsesorio");

            migrationBuilder.CreateIndex(
                name: "IX_Historiales_IdAsignacion",
                table: "Historiales",
                column: "IdAsignacion");

            migrationBuilder.CreateIndex(
                name: "IX_Historiales_IdMotivo",
                table: "Historiales",
                column: "IdMotivo");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimiento_carro_IdCarro",
                table: "Mantenimiento_carro",
                column: "IdCarro");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimiento_carro_IdTipoMante",
                table: "Mantenimiento_carro",
                column: "IdTipoMante");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipos_TiposEquipo_idTipoEquipo",
                table: "Equipos",
                column: "idTipoEquipo",
                principalTable: "TiposEquipo",
                principalColumn: "idTipoEquipo",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipos_TiposEquipo_idTipoEquipo",
                table: "Equipos");

            migrationBuilder.DropTable(
                name: "Carro_Asesorio");

            migrationBuilder.DropTable(
                name: "Carro_Modalidad");

            migrationBuilder.DropTable(
                name: "Carro_Seguro");

            migrationBuilder.DropTable(
                name: "Empleado_Grupo");

            migrationBuilder.DropTable(
                name: "Empleado_Seguro");

            migrationBuilder.DropTable(
                name: "Empleados_Carros");

            migrationBuilder.DropTable(
                name: "Grupo_Asesorio");

            migrationBuilder.DropTable(
                name: "Historiales");

            migrationBuilder.DropTable(
                name: "Mantenimiento_carro");

            migrationBuilder.DropTable(
                name: "Modalidades");

            migrationBuilder.DropTable(
                name: "Seguro");

            migrationBuilder.DropTable(
                name: "Asesorios");

            migrationBuilder.DropTable(
                name: "Grupos");

            migrationBuilder.DropTable(
                name: "Asignaciones");

            migrationBuilder.DropTable(
                name: "Motivos");

            migrationBuilder.DropTable(
                name: "Carros");

            migrationBuilder.DropTable(
                name: "TipoMantenimiento");

            migrationBuilder.DropTable(
                name: "Chips");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TiposEquipo",
                table: "TiposEquipo");

            migrationBuilder.RenameTable(
                name: "TiposEquipo",
                newName: "TipoEquipos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TipoEquipos",
                table: "TipoEquipos",
                column: "idTipoEquipo");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipos_TipoEquipos_idTipoEquipo",
                table: "Equipos",
                column: "idTipoEquipo",
                principalTable: "TipoEquipos",
                principalColumn: "idTipoEquipo",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
