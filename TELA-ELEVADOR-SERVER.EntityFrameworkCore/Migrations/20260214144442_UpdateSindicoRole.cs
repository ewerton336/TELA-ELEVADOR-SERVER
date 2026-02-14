using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSindicoRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sindico_Predio_PredioId",
                table: "Sindico");

            migrationBuilder.AlterColumn<int>(
                name: "PredioId",
                table: "Sindico",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Sindico",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Sindico_Predio_PredioId",
                table: "Sindico",
                column: "PredioId",
                principalTable: "Predio",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sindico_Predio_PredioId",
                table: "Sindico");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Sindico");

            migrationBuilder.AlterColumn<int>(
                name: "PredioId",
                table: "Sindico",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sindico_Predio_PredioId",
                table: "Sindico",
                column: "PredioId",
                principalTable: "Predio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
