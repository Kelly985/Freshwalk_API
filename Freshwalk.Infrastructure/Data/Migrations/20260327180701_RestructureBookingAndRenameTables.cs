using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freshwalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestructureBookingAndRenameTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_CustomerId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderAuditLogs_AspNetUsers_ActorId",
                table: "OrderAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderRiderAssignments_AspNetUsers_RiderId",
                table: "OrderRiderAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderProfiles_AspNetUsers_UserId",
                table: "RiderProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.DropColumn(
                name: "ColorTier",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ShoeType",
                table: "Bookings");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "FreshwalkUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "FreshwalkUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "FreshwalkUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "FreshwalkUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "FreshwalkUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "FreshwalkRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "FreshwalkRoleClaims");

            migrationBuilder.RenameColumn(
                name: "PairCount",
                table: "Bookings",
                newName: "TotalPairs");

            migrationBuilder.RenameColumn(
                name: "AddOns",
                table: "Bookings",
                newName: "SuedeColorTier");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "FreshwalkUsers",
                newName: "IX_FreshwalkUsers_PhoneNumber");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "FreshwalkUserRoles",
                newName: "IX_FreshwalkUserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "FreshwalkUserLogins",
                newName: "IX_FreshwalkUserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "FreshwalkUserClaims",
                newName: "IX_FreshwalkUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "FreshwalkRoleClaims",
                newName: "IX_FreshwalkRoleClaims_RoleId");

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

            migrationBuilder.AddPrimaryKey(
                name: "PK_FreshwalkUserTokens",
                table: "FreshwalkUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_FreshwalkUsers",
                table: "FreshwalkUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FreshwalkUserRoles",
                table: "FreshwalkUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_FreshwalkUserLogins",
                table: "FreshwalkUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_FreshwalkUserClaims",
                table: "FreshwalkUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FreshwalkRoles",
                table: "FreshwalkRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FreshwalkRoleClaims",
                table: "FreshwalkRoleClaims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_FreshwalkUsers_CustomerId",
                table: "Bookings",
                column: "CustomerId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FreshwalkRoleClaims_FreshwalkRoles_RoleId",
                table: "FreshwalkRoleClaims",
                column: "RoleId",
                principalTable: "FreshwalkRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FreshwalkUserClaims_FreshwalkUsers_UserId",
                table: "FreshwalkUserClaims",
                column: "UserId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FreshwalkUserLogins_FreshwalkUsers_UserId",
                table: "FreshwalkUserLogins",
                column: "UserId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FreshwalkUserRoles_FreshwalkRoles_RoleId",
                table: "FreshwalkUserRoles",
                column: "RoleId",
                principalTable: "FreshwalkRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FreshwalkUserRoles_FreshwalkUsers_UserId",
                table: "FreshwalkUserRoles",
                column: "UserId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FreshwalkUserTokens_FreshwalkUsers_UserId",
                table: "FreshwalkUserTokens",
                column: "UserId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_FreshwalkUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAuditLogs_FreshwalkUsers_ActorId",
                table: "OrderAuditLogs",
                column: "ActorId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderRiderAssignments_FreshwalkUsers_RiderId",
                table: "OrderRiderAssignments",
                column: "RiderId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_FreshwalkUsers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderProfiles_FreshwalkUsers_UserId",
                table: "RiderProfiles",
                column: "UserId",
                principalTable: "FreshwalkUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_FreshwalkUsers_CustomerId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FreshwalkRoleClaims_FreshwalkRoles_RoleId",
                table: "FreshwalkRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_FreshwalkUserClaims_FreshwalkUsers_UserId",
                table: "FreshwalkUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_FreshwalkUserLogins_FreshwalkUsers_UserId",
                table: "FreshwalkUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_FreshwalkUserRoles_FreshwalkRoles_RoleId",
                table: "FreshwalkUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_FreshwalkUserRoles_FreshwalkUsers_UserId",
                table: "FreshwalkUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_FreshwalkUserTokens_FreshwalkUsers_UserId",
                table: "FreshwalkUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_FreshwalkUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderAuditLogs_FreshwalkUsers_ActorId",
                table: "OrderAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderRiderAssignments_FreshwalkUsers_RiderId",
                table: "OrderRiderAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_FreshwalkUsers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderProfiles_FreshwalkUsers_UserId",
                table: "RiderProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FreshwalkUserTokens",
                table: "FreshwalkUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FreshwalkUsers",
                table: "FreshwalkUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FreshwalkUserRoles",
                table: "FreshwalkUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FreshwalkUserLogins",
                table: "FreshwalkUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FreshwalkUserClaims",
                table: "FreshwalkUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FreshwalkRoles",
                table: "FreshwalkRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FreshwalkRoleClaims",
                table: "FreshwalkRoleClaims");

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
                name: "SuedePairs",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Waterproofing",
                table: "Bookings");

            migrationBuilder.RenameTable(
                name: "FreshwalkUserTokens",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "FreshwalkUsers",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "FreshwalkUserRoles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "FreshwalkUserLogins",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "FreshwalkUserClaims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "FreshwalkRoles",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "FreshwalkRoleClaims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameColumn(
                name: "TotalPairs",
                table: "Bookings",
                newName: "PairCount");

            migrationBuilder.RenameColumn(
                name: "SuedeColorTier",
                table: "Bookings",
                newName: "AddOns");

            migrationBuilder.RenameIndex(
                name: "IX_FreshwalkUsers_PhoneNumber",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_PhoneNumber");

            migrationBuilder.RenameIndex(
                name: "IX_FreshwalkUserRoles_RoleId",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_FreshwalkUserLogins_UserId",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FreshwalkUserClaims_UserId",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FreshwalkRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.AddColumn<string>(
                name: "ColorTier",
                table: "Bookings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShoeType",
                table: "Bookings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_CustomerId",
                table: "Bookings",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAuditLogs_AspNetUsers_ActorId",
                table: "OrderAuditLogs",
                column: "ActorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderRiderAssignments_AspNetUsers_RiderId",
                table: "OrderRiderAssignments",
                column: "RiderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderProfiles_AspNetUsers_UserId",
                table: "RiderProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
