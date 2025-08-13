using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EditSearch.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddParsedName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParsedName",
                table: "Hotels",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParsedName",
                table: "Hotels");
        }
    }
}
