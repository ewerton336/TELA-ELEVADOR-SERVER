using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPredioOrientationMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrientationMode",
                table: "Predio",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "auto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrientationMode",
                table: "Predio");
        }
    }
}
