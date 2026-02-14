using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Noticia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FonteChave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FonteNome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Link = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ImagemUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PubDateRaw = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PublicadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Categoria = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Noticia", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Noticia_FonteChave",
                table: "Noticia",
                column: "FonteChave");

            migrationBuilder.CreateIndex(
                name: "IX_Noticia_Link",
                table: "Noticia",
                column: "Link",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Noticia_PublicadoEmUtc",
                table: "Noticia",
                column: "PublicadoEmUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Noticia");
        }
    }
}
