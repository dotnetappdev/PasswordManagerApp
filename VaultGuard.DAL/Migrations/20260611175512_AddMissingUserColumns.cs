using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingUserColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing AspNetUsers columns that were in the model snapshot but never
            // included in a migration's Up() method, causing INSERT failures at registration.
            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorEnabled",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneNumberConfirmedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VaultId",
                table: "PasswordItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VaultId",
                table: "Categories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Changes = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Device",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeviceToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPrimaryDevice = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Device", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Device_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtpCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CodeHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequestIpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    RequestUserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtpCodes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBackupSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    EnableCloudBackup = table.Column<bool>(type: "INTEGER", nullable: false),
                    SelectedCloudProvider = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoBackupEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxBackupsToKeep = table.Column<int>(type: "INTEGER", nullable: false),
                    BackupIntervalHours = table.Column<int>(type: "INTEGER", nullable: false),
                    BackupScheduleInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredBackupTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    CompressBackups = table.Column<bool>(type: "INTEGER", nullable: false),
                    BackupFolderPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NetworkPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastBackupAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextBackupAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBackupSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBackupSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vault",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vault", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vault_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordItems_VaultId",
                table: "PasswordItems",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_VaultId",
                table: "Categories",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Device_UserId",
                table: "Device",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBackupSettings_UserId",
                table: "UserBackupSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vault_UserId",
                table: "Vault",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Vault_VaultId",
                table: "Categories",
                column: "VaultId",
                principalTable: "Vault",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordItems_Vault_VaultId",
                table: "PasswordItems",
                column: "VaultId",
                principalTable: "Vault",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsTwoFactorEnabled",    table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "BackupCodes",           table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "BackupCodesUsed",       table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "PhoneNumberConfirmedAt", table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Vault_VaultId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_PasswordItems_Vault_VaultId",
                table: "PasswordItems");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "Device");

            migrationBuilder.DropTable(
                name: "OtpCodes");

            migrationBuilder.DropTable(
                name: "UserBackupSettings");

            migrationBuilder.DropTable(
                name: "Vault");

            migrationBuilder.DropIndex(
                name: "IX_PasswordItems_VaultId",
                table: "PasswordItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_VaultId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "VaultId",
                table: "PasswordItems");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "VaultId",
                table: "Categories");
        }
    }
}
