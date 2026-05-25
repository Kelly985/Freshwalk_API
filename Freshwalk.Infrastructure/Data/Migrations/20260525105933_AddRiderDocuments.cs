using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freshwalk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRiderDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BikeRegistration",
                table: "RiderProfiles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicIdIdBack",
                table: "RiderProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicIdIdFront",
                table: "RiderProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicIdSelfie",
                table: "RiderProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "RiderProfiles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "RiderProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdBackUrl",
                table: "RiderProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdFrontUrl",
                table: "RiderProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "RiderProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelfieUrl",
                table: "RiderProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudinaryPublicIdIdBack",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "CloudinaryPublicIdIdFront",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "CloudinaryPublicIdSelfie",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "IdBackUrl",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "IdFrontUrl",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "SelfieUrl",
                table: "RiderProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "BikeRegistration",
                table: "RiderProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
