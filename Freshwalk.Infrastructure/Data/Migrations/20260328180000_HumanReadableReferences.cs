using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freshwalk.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class HumanReadableReferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReferenceSequences",
            columns: table => new
            {
                Prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                LastValue = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReferenceSequences", x => x.Prefix);
            });

        migrationBuilder.AddColumn<string>(
            name: "CustomerReference",
            table: "FreshwalkUsers",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BookingReference",
            table: "Bookings",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PaymentReference",
            table: "Payments",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OrderReference",
            table: "Orders",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AssignmentReference",
            table: "OrderRiderAssignments",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.Sql("""
            WITH n AS (
              SELECT "Id", 'KE-' || LPAD(ROW_NUMBER() OVER (ORDER BY "CreatedAt")::text, 8, '0') AS ref
              FROM "FreshwalkUsers"
            )
            UPDATE "FreshwalkUsers" u SET "CustomerReference" = n.ref FROM n WHERE u."Id" = n."Id";
            """);

        migrationBuilder.Sql("""
            WITH n AS (
              SELECT "Id", 'FW-' || LPAD(ROW_NUMBER() OVER (ORDER BY "CreatedAt")::text, 8, '0') AS ref
              FROM "Bookings"
            )
            UPDATE "Bookings" b SET "BookingReference" = n.ref FROM n WHERE b."Id" = n."Id";
            """);

        migrationBuilder.Sql("""
            WITH n AS (
              SELECT "Id", 'PM-' || LPAD(ROW_NUMBER() OVER (ORDER BY "CreatedAt")::text, 8, '0') AS ref
              FROM "Payments"
            )
            UPDATE "Payments" p SET "PaymentReference" = n.ref FROM n WHERE p."Id" = n."Id";
            """);

        migrationBuilder.Sql("""
            UPDATE "Orders" o SET "OrderReference" = b."BookingReference" FROM "Bookings" b WHERE o."BookingId" = b."Id";
            """);

        migrationBuilder.Sql("""
            WITH n AS (
              SELECT "Id", 'RA-' || LPAD(ROW_NUMBER() OVER (ORDER BY "AssignedAt")::text, 8, '0') AS ref
              FROM "OrderRiderAssignments"
            )
            UPDATE "OrderRiderAssignments" a SET "AssignmentReference" = n.ref FROM n WHERE a."Id" = n."Id";
            """);

        migrationBuilder.Sql("""
            INSERT INTO "ReferenceSequences" ("Prefix", "LastValue") VALUES
            ('KE', COALESCE((SELECT COUNT(*)::bigint FROM "FreshwalkUsers"), 0)),
            ('FW', COALESCE((SELECT COUNT(*)::bigint FROM "Bookings"), 0)),
            ('PM', COALESCE((SELECT COUNT(*)::bigint FROM "Payments"), 0)),
            ('RA', COALESCE((SELECT COUNT(*)::bigint FROM "OrderRiderAssignments"), 0))
            ON CONFLICT ("Prefix") DO UPDATE SET "LastValue" = EXCLUDED."LastValue";
            """);

        migrationBuilder.AlterColumn<string>(
            name: "CustomerReference",
            table: "FreshwalkUsers",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "BookingReference",
            table: "Bookings",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "PaymentReference",
            table: "Payments",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AssignmentReference",
            table: "OrderRiderAssignments",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_FreshwalkUsers_CustomerReference",
            table: "FreshwalkUsers",
            column: "CustomerReference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_BookingReference",
            table: "Bookings",
            column: "BookingReference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_PaymentReference",
            table: "Payments",
            column: "PaymentReference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_OrderReference",
            table: "Orders",
            column: "OrderReference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OrderRiderAssignments_AssignmentReference",
            table: "OrderRiderAssignments",
            column: "AssignmentReference",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OrderRiderAssignments_AssignmentReference",
            table: "OrderRiderAssignments");

        migrationBuilder.DropIndex(
            name: "IX_Orders_OrderReference",
            table: "Orders");

        migrationBuilder.DropIndex(
            name: "IX_Payments_PaymentReference",
            table: "Payments");

        migrationBuilder.DropIndex(
            name: "IX_Bookings_BookingReference",
            table: "Bookings");

        migrationBuilder.DropIndex(
            name: "IX_FreshwalkUsers_CustomerReference",
            table: "FreshwalkUsers");

        migrationBuilder.DropColumn(
            name: "AssignmentReference",
            table: "OrderRiderAssignments");

        migrationBuilder.DropColumn(
            name: "OrderReference",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "PaymentReference",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "BookingReference",
            table: "Bookings");

        migrationBuilder.DropColumn(
            name: "CustomerReference",
            table: "FreshwalkUsers");

        migrationBuilder.DropTable(
            name: "ReferenceSequences");
    }
}
