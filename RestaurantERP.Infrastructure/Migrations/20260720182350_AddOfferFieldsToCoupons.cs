using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferFieldsToCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeText",
                table: "Coupons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtaText",
                table: "Coupons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FirstOrderOnly",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnHomepage",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "Coupons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Coupons",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeText",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "CtaText",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "FirstOrderOnly",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "ShowOnHomepage",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Coupons");
        }
    }
}
