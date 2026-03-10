using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddScreenModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ModuloBuildingNotice",
                table: "Predio",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModuloHeadlineNews",
                table: "Predio",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModuloNewsTicker",
                table: "Predio",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModuloWeather",
                table: "Predio",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModuloBuildingNotice",
                table: "Predio");

            migrationBuilder.DropColumn(
                name: "ModuloHeadlineNews",
                table: "Predio");

            migrationBuilder.DropColumn(
                name: "ModuloNewsTicker",
                table: "Predio");

            migrationBuilder.DropColumn(
                name: "ModuloWeather",
                table: "Predio");
        }
    }
}
