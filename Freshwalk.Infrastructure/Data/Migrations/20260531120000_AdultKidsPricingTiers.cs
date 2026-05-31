using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freshwalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdultKidsPricingTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add IsActive column — existing colour-tier rows default to false (inactive).
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ShoeServiceTierPrices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 2. Insert the two new active tiers (Adult = KES 300, Kids = KES 250)
            //    for every shoe category that already exists in the table.
            migrationBuilder.Sql(@"
                INSERT INTO ""ShoeServiceTierPrices"" (""CategoryId"", ""ColorTierKey"", ""PriceKes"", ""SortOrder"", ""IsActive"")
                SELECT ""Id"", 'Adult', 300, 4, true
                FROM   ""ShoeServiceCategories"";

                INSERT INTO ""ShoeServiceTierPrices"" (""CategoryId"", ""ColorTierKey"", ""PriceKes"", ""SortOrder"", ""IsActive"")
                SELECT ""Id"", 'Kids', 250, 5, true
                FROM   ""ShoeServiceCategories"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the Adult and Kids rows, restore all remaining rows to active,
            // then drop the column.
            migrationBuilder.Sql(@"
                DELETE FROM ""ShoeServiceTierPrices""
                WHERE  ""ColorTierKey"" IN ('Adult', 'Kids');

                UPDATE ""ShoeServiceTierPrices""
                SET    ""IsActive"" = true;
            ");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ShoeServiceTierPrices");
        }
    }
}
