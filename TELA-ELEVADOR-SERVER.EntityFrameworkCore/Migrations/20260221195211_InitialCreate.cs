using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "Predio");

            migrationBuilder.AddColumn<int>(
                name: "CidadeId",
                table: "Predio",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NomeExibicao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cidade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClimaPrevisao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CidadeId = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    TemperaturaMaxima = table.Column<int>(type: "integer", nullable: false),
                    TemperaturaMinima = table.Column<int>(type: "integer", nullable: false),
                    CodigoWmo = table.Column<int>(type: "integer", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClimaPrevisao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClimaPrevisao_Cidade_CidadeId",
                        column: x => x.CidadeId,
                        principalTable: "Cidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Predio_CidadeId",
                table: "Predio",
                column: "CidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Cidade_Nome_Unique",
                table: "Cidade",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClimaPrevisao_CidadeId_Data_Unique",
                table: "ClimaPrevisao",
                columns: new[] { "CidadeId", "Data" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Predio_Cidade_CidadeId",
                table: "Predio",
                column: "CidadeId",
                principalTable: "Cidade",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predio_Cidade_CidadeId",
                table: "Predio");

            migrationBuilder.DropTable(
                name: "ClimaPrevisao");

            migrationBuilder.DropTable(
                name: "Cidade");

            migrationBuilder.DropIndex(
                name: "IX_Predio_CidadeId",
                table: "Predio");

            migrationBuilder.DropColumn(
                name: "CidadeId",
                table: "Predio");

            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "Predio",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
