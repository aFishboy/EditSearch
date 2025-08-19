using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EditSearch.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedHotelProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HotelPrice_Hotels_HotelId",
                table: "HotelPrice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HotelPrice",
                table: "HotelPrice");

            migrationBuilder.RenameTable(
                name: "HotelPrice",
                newName: "HotelPrice");

            migrationBuilder.RenameColumn(
                name: "ZipCode",
                table: "Hotels",
                newName: "Postcode");

            migrationBuilder.RenameIndex(
                name: "IX_HotelPrice_HotelId",
                table: "HotelPrice",
                newName: "IX_HotelPrice_HotelId");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Hotels",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Hotels",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "Hotels",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedAddress",
                table: "Hotels",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Hotels",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Hotels",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HotelPrice",
                table: "HotelPrice",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HotelPrice_Hotels_HotelId",
                table: "HotelPrice",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HotelPrice_Hotels_HotelId",
                table: "HotelPrice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HotelPrice",
                table: "HotelPrice");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "County",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "FormattedAddress",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Hotels");

            migrationBuilder.RenameTable(
                name: "HotelPrice",
                newName: "HotelPrice");

            migrationBuilder.RenameColumn(
                name: "Postcode",
                table: "Hotels",
                newName: "ZipCode");

            migrationBuilder.RenameIndex(
                name: "IX_HotelPrice_HotelId",
                table: "HotelPrice",
                newName: "IX_HotelPrice_HotelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HotelPrice",
                table: "HotelPrice",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HotelPrice_Hotels_HotelId",
                table: "HotelPrice",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
