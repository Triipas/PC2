using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    /// <inheritdoc />
    public partial class VisitasReservasValidaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inmuebles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Imagen = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Ciudad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Dormitorios = table.Column<int>(type: "INTEGER", nullable: false),
                    Banos = table.Column<int>(type: "INTEGER", nullable: false),
                    MetrosCuadrados = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inmuebles", x => x.Id);
                    table.CheckConstraint("CK_Inmueble_MetrosCuadrados_Positive", "[MetrosCuadrados] > 0");
                    table.CheckConstraint("CK_Inmueble_Precio_Positive", "[Precio] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InmuebleId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Inmuebles_InmuebleId",
                        column: x => x.InmuebleId,
                        principalTable: "Inmuebles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Visitas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InmuebleId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visitas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visitas_Inmuebles_InmuebleId",
                        column: x => x.InmuebleId,
                        principalTable: "Inmuebles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inmuebles_Codigo",
                table: "Inmuebles",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Inmueble_Expiracion",
                table: "Reservas",
                columns: new[] { "InmuebleId", "FechaExpiracion" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId",
                table: "Reservas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitas_Inmueble_Fechas",
                table: "Visitas",
                columns: new[] { "InmuebleId", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_Visitas_UsuarioId",
                table: "Visitas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Visitas");

            migrationBuilder.DropTable(
                name: "Inmuebles");
        }
    }
}
