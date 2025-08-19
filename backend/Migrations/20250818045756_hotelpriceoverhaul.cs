using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EditSearch.Backend.Migrations
{
    /// <inheritdoc />
    public partial class hotelpriceoverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfidenceScore",
                table: "HotelPrice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "HotelPrice",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEstimated",
                table: "HotelPrice",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "HotelPrice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "HotelPrice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                table: "HotelPrice",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Season",
                table: "HotelPrice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "HotelPrice",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "HotelPrice");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "HotelPrice");

            migrationBuilder.DropColumn(
                name: "IsEstimated",
                table: "HotelPrice");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "HotelPrice");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "HotelPrice");

            migrationBuilder.DropColumn(
                name: "RoomType",
                table: "HotelPrice");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "HotelPrice");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "HotelPrice");
        }
    }
}
