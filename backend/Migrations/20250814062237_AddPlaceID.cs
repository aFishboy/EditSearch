using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EditSearch.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaceID",
                table: "Hotels",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaceID",
                table: "Hotels");
        }
    }
}
