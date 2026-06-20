using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTickerMensagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TickerMensagem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PredioId = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TickerMensagem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TickerMensagem_Predio_PredioId",
                        column: x => x.PredioId,
                        principalTable: "Predio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TickerMensagem_PredioId",
                table: "TickerMensagem",
                column: "PredioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TickerMensagem");
        }
    }
}
