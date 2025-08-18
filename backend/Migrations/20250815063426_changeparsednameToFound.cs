using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EditSearch.Backend.Migrations
{
    /// <inheritdoc />
    public partial class changeparsednameToFound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParsedName",
                table: "Hotels",
                newName: "FoundApiName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FoundApiName",
                table: "Hotels",
                newName: "ParsedName");
        }
    }
}
