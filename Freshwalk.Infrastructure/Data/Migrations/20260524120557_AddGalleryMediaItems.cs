using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freshwalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryMediaItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GalleryMediaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BeforeUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AfterUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CloudinaryPublicIdBefore = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CloudinaryPublicIdAfter = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsHero = table.Column<bool>(type: "boolean", nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryMediaItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GalleryMediaItems");
        }
    }
}
