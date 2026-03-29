using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freshwalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BookingDiscountBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BundleDiscountPercent",
                table: "Bookings",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmountKes",
                table: "Bookings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalBeforeDiscountKes",
                table: "Bookings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""UPDATE "Bookings" SET "SubtotalBeforeDiscountKes" = "PriceKes" WHERE "SubtotalBeforeDiscountKes" = 0;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BundleDiscountPercent",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DiscountAmountKes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SubtotalBeforeDiscountKes",
                table: "Bookings");
        }
    }
}
