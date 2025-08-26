using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryIconColorOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastModified",
                table: "LoginItems",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tags",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SecureNoteItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAt",
                table: "PasswordItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "PasswordItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CreditCardItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Collections",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Categories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "BackupCodes",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BackupCodesUsed",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorEnabled",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneNumberConfirmedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderConfig",
                table: "ApiKeys",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PasskeyItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EncryptedCredentialId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CredentialIdNonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CredentialIdAuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PlatformName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsBackedUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresUserVerification = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EncryptedNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    NotesNonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NotesAuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasskeyItem_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PasskeyItem_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmsSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultCountryCode = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    CodeLength = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpirationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxSmsPerHour = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageTemplate = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TwilioAccountSid = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TwilioAuthToken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TwilioFromPhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AwsAccessKeyId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AwsSecretAccessKey = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AwsRegion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AwsSenderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AzureConnectionString = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AzureFromPhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyItem_PasswordItemId",
                table: "PasskeyItem",
                column: "PasswordItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyItem_UserId",
                table: "PasskeyItem",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsSettings_UserId",
                table: "SmsSettings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasskeyItem");

            migrationBuilder.DropTable(
                name: "SmsSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SecureNoteItems");

            migrationBuilder.DropColumn(
                name: "LastAccessedAt",
                table: "PasswordItems");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "PasswordItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CreditCardItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "BackupCodes",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BackupCodesUsed",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsTwoFactorEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "ProviderConfig",
                table: "ApiKeys");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "LoginItems",
                newName: "LastModified");
        }
    }
}
