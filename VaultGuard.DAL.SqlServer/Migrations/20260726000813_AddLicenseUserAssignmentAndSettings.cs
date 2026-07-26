using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultGuard.DAL.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseUserAssignmentAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "LicenseKeys",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LicensingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SigningPrivateKeyPem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SigningPublicKeyPem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AesKeyBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultMaxActivations = table.Column<int>(type: "int", nullable: false),
                    DefaultPlan = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensingSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseKeys_UserId",
                table: "LicenseKeys",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LicenseKeys_AspNetUsers_UserId",
                table: "LicenseKeys",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LicenseKeys_AspNetUsers_UserId",
                table: "LicenseKeys");

            migrationBuilder.DropTable(
                name: "LicensingSettings");

            migrationBuilder.DropIndex(
                name: "IX_LicenseKeys_UserId",
                table: "LicenseKeys");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LicenseKeys");
        }
    }
}
