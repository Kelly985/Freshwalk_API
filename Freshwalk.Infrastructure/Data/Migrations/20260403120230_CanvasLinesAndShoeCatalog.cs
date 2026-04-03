using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Freshwalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CanvasLinesAndShoeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OfficialLeatherLines",
                table: "Bookings",
                newName: "CanvasLines");

            migrationBuilder.CreateTable(
                name: "ShoeServiceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tagline = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ThemeColorHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ImageBeforeUrl = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    ImageAfterUrl = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoeServiceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoeServiceTierPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    ColorTierKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PriceKes = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoeServiceTierPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoeServiceTierPrices_ShoeServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ShoeServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoeServiceCategories_Key",
                table: "ShoeServiceCategories",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoeServiceTierPrices_CategoryId_ColorTierKey",
                table: "ShoeServiceTierPrices",
                columns: new[] { "CategoryId", "ColorTierKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoeServiceTierPrices");

            migrationBuilder.DropTable(
                name: "ShoeServiceCategories");

            migrationBuilder.RenameColumn(
                name: "CanvasLines",
                table: "Bookings",
                newName: "OfficialLeatherLines");
        }
    }
}
