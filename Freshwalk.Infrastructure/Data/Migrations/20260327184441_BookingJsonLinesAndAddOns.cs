using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freshwalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BookingJsonLinesAndAddOns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddOns",
                table: "Bookings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "NubuckLines",
                table: "Bookings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "OfficialLeatherLines",
                table: "Bookings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "SneakersLines",
                table: "Bookings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "SuedeLines",
                table: "Bookings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.Sql("""
                UPDATE "Bookings" b SET "AddOns" = COALESCE((
                  SELECT jsonb_agg(t.v ORDER BY t.ord)
                  FROM (
                    SELECT 1 AS ord, 'Waterproofing'::text AS v WHERE COALESCE(b."Waterproofing", false)
                    UNION ALL
                    SELECT 2, 'ShadeChanging' WHERE COALESCE(b."ShadeChanging", false)
                    UNION ALL
                    SELECT 3, 'PickupDelivery' WHERE COALESCE(b."PickupDelivery", false)
                    UNION ALL
                    SELECT 4, 'ExpressService' WHERE COALESCE(b."ExpressService", false)
                  ) t
                ), '[]'::jsonb);

                UPDATE "Bookings" SET "SneakersLines" = CASE
                  WHEN COALESCE("SneakersPairs", 0) > 0 THEN jsonb_build_array(
                    jsonb_build_object(
                      'color', COALESCE(NULLIF(TRIM("SneakersColorTier"), ''), 'MixedColored'),
                      'quantity', "SneakersPairs"
                    )
                  )
                  ELSE '[]'::jsonb
                END;

                UPDATE "Bookings" SET "SuedeLines" = CASE
                  WHEN COALESCE("SuedePairs", 0) > 0 THEN jsonb_build_array(
                    jsonb_build_object(
                      'color', COALESCE(NULLIF(TRIM("SuedeColorTier"), ''), 'MixedColored'),
                      'quantity', "SuedePairs"
                    )
                  )
                  ELSE '[]'::jsonb
                END;

                UPDATE "Bookings" SET "NubuckLines" = CASE
                  WHEN COALESCE("NubuckPairs", 0) > 0 THEN jsonb_build_array(
                    jsonb_build_object(
                      'color', COALESCE(NULLIF(TRIM("NubuckColorTier"), ''), 'MixedColored'),
                      'quantity', "NubuckPairs"
                    )
                  )
                  ELSE '[]'::jsonb
                END;

                UPDATE "Bookings" SET "OfficialLeatherLines" = CASE
                  WHEN COALESCE("OfficialLeatherPairs", 0) > 0 THEN jsonb_build_array(
                    jsonb_build_object(
                      'color', COALESCE(NULLIF(TRIM("OfficialLeatherColorTier"), ''), 'MixedColored'),
                      'quantity', "OfficialLeatherPairs"
                    )
                  )
                  ELSE '[]'::jsonb
                END;
                """);

            migrationBuilder.DropColumn(
                name: "ExpressService",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NubuckColorTier",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NubuckPairs",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OfficialLeatherColorTier",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OfficialLeatherPairs",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PickupDelivery",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ShadeChanging",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SneakersColorTier",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SneakersPairs",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SuedeColorTier",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SuedePairs",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Waterproofing",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExpressService",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NubuckColorTier",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NubuckPairs",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OfficialLeatherColorTier",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OfficialLeatherPairs",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PickupDelivery",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShadeChanging",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SneakersColorTier",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SneakersPairs",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SuedeColorTier",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuedePairs",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Waterproofing",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE "Bookings" b SET
                  "Waterproofing" = COALESCE(("AddOns"::jsonb @> '["Waterproofing"]'::jsonb), false),
                  "ShadeChanging" = COALESCE(("AddOns"::jsonb @> '["ShadeChanging"]'::jsonb), false),
                  "PickupDelivery" = COALESCE(("AddOns"::jsonb @> '["PickupDelivery"]'::jsonb), false),
                  "ExpressService" = COALESCE(("AddOns"::jsonb @> '["ExpressService"]'::jsonb), false);
                """);

            migrationBuilder.Sql("""
                UPDATE "Bookings" SET
                  "SneakersPairs" = COALESCE((
                    SELECT SUM((elem->>'quantity')::int)
                    FROM jsonb_array_elements("SneakersLines"::jsonb) elem
                  ), 0),
                  "SneakersColorTier" = (
                    SELECT elem->>'color'
                    FROM jsonb_array_elements("SneakersLines"::jsonb) elem
                    LIMIT 1
                  );
                UPDATE "Bookings" SET
                  "SuedePairs" = COALESCE((
                    SELECT SUM((elem->>'quantity')::int)
                    FROM jsonb_array_elements("SuedeLines"::jsonb) elem
                  ), 0),
                  "SuedeColorTier" = (
                    SELECT elem->>'color'
                    FROM jsonb_array_elements("SuedeLines"::jsonb) elem
                    LIMIT 1
                  );
                UPDATE "Bookings" SET
                  "NubuckPairs" = COALESCE((
                    SELECT SUM((elem->>'quantity')::int)
                    FROM jsonb_array_elements("NubuckLines"::jsonb) elem
                  ), 0),
                  "NubuckColorTier" = (
                    SELECT elem->>'color'
                    FROM jsonb_array_elements("NubuckLines"::jsonb) elem
                    LIMIT 1
                  );
                UPDATE "Bookings" SET
                  "OfficialLeatherPairs" = COALESCE((
                    SELECT SUM((elem->>'quantity')::int)
                    FROM jsonb_array_elements("OfficialLeatherLines"::jsonb) elem
                  ), 0),
                  "OfficialLeatherColorTier" = (
                    SELECT elem->>'color'
                    FROM jsonb_array_elements("OfficialLeatherLines"::jsonb) elem
                    LIMIT 1
                  );
                """);

            migrationBuilder.DropColumn(
                name: "AddOns",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NubuckLines",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OfficialLeatherLines",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SneakersLines",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SuedeLines",
                table: "Bookings");
        }
    }
}
