using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_SmsSettings_UserId",
                table: "SmsSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsSettings_UserId_IsActive",
                table: "SmsSettings",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsSettings_IsActive",
                table: "SmsSettings",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmsSettings");
        }
    }
}