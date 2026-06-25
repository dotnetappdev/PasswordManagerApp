using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Add2FAAndPasskeySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PasskeysEnabled",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasskeysEnabledAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StorePasskeysInVault",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TwoFactorBackupCodesRemaining",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorEnabledAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorRecoveryEmail",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecretKey",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserPasskeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PublicKey = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    SignatureCounter = table.Column<uint>(type: "INTEGER", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsBackedUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresUserVerification = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    StoreInVault = table.Column<bool>(type: "INTEGER", nullable: false),
                    EncryptedVaultData = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPasskeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPasskeys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTwoFactorBackupCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CodeHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CodeSalt = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsedFromIp = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTwoFactorBackupCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTwoFactorBackupCodes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPasskeys_UserId",
                table: "UserPasskeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTwoFactorBackupCodes_UserId",
                table: "UserTwoFactorBackupCodes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPasskeys");

            migrationBuilder.DropTable(
                name: "UserTwoFactorBackupCodes");

            migrationBuilder.DropColumn(
                name: "PasskeysEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasskeysEnabledAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StorePasskeysInVault",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorBackupCodesRemaining",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabledAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorRecoveryEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecretKey",
                table: "AspNetUsers");
        }
    }
}
