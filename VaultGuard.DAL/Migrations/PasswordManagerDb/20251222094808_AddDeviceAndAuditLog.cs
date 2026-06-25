using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.DAL.Migrations.PasswordManagerDb
{
    /// <inheritdoc />
    public partial class AddDeviceAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // QrLoginTokens is already created by PasswordManagerDbContextApp's firstmigration
            // when both contexts share the same SQLite file. Use IF NOT EXISTS to be idempotent.
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS ""QrLoginTokens"" (
    ""Token"" TEXT NOT NULL CONSTRAINT ""PK_QrLoginTokens"" PRIMARY KEY,
    ""UserId"" TEXT NOT NULL,
    ""ExpiresAt"" TEXT NOT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    ""IsUsed"" INTEGER NOT NULL,
    ""UsedAt"" TEXT NULL,
    ""UserAgent"" TEXT NULL,
    ""IpAddress"" TEXT NULL,
    ""Status"" INTEGER NOT NULL
);");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MasterPasswordHint = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserSalt = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MasterPasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MasterPasswordIterations = table.Column<int>(type: "INTEGER", nullable: false),
                    MasterKeyIdentifier = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsTwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PhoneNumberConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BackupCodes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    BackupCodesUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorSecretKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TwoFactorEnabledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TwoFactorRecoveryEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    TwoFactorBackupCodesRemaining = table.Column<int>(type: "INTEGER", nullable: false),
                    PasskeysEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasskeysEnabledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StorePasskeysInVault = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: true),
                    ProviderConfig = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
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
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChildPermissionConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ParentUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CanViewPasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanCreatePasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditPasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDeletePasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanRevealPasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanSharePasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanExportData = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanCreateCollections = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManageOwnCollections = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxPasswordItems = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessStartTime = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    AccessEndTime = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    AllowedDaysOfWeek = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RequireParentApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    LogActivities = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildPermissionConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildPermissionConfig_Users_ChildUserId",
                        column: x => x.ChildUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildPermissionConfig_Users_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentCollectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_Collections_ParentCollectionId",
                        column: x => x.ParentCollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Collections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
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
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
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
                        name: "FK_OtpCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
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
                        name: "FK_SmsSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSystemTag = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBackupSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
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
                        name: "FK_UserBackupSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                        name: "FK_UserPasskeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRelationship",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ChildUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    RelationshipType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRelationship", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRelationship_Users_ChildUserId",
                        column: x => x.ChildUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRelationship_Users_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "Users",
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
                        name: "FK_UserTwoFactorBackupCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Categories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PasswordItems_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PasswordItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CardholderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CardNumber = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ExpiryDate = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CVV = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PIN = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    CardType = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuingBank = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ValidFrom = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    BankWebsite = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BankPhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CustomerServicePhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    OnlineBankingUsername = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OnlineBankingPassword = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OnlineBankingUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreditLimit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    InterestRate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    CashAdvanceLimit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AvailableCredit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    BillingAddressLine1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BillingAddressLine2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BillingCity = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BillingState = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BillingZipCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    BillingCountry = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RewardsProgram = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RewardsNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BenefitsDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TravelInsurance = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AirportLoungeAccess = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FraudAlertPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FraudAlertEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastUsed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ExpirationMonth = table.Column<string>(type: "TEXT", nullable: true),
                    ExpirationYear = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityCode = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresMasterPassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordId = table.Column<int>(type: "INTEGER", nullable: true),
                    EncryptedCardNumber = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CardNumberNonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CardNumberAuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EncryptedCvv = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CvvNonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CvvAuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardItems_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditCardItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsProtected = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFields_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoginItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PasswordNonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PasswordAuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EncryptedTotpSecret = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TotpNonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TotpAuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TwoFactorType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityQuestion1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EncryptedSecurityAnswer1 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SecurityAnswer1Nonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityAnswer1AuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityQuestion2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EncryptedSecurityAnswer2 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SecurityAnswer2Nonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityAnswer2AuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityQuestion3 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EncryptedSecurityAnswer3 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SecurityAnswer3Nonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityAnswer3AuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RecoveryEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RecoveryPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    LoginUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SupportUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AdminConsoleUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PasswordLastChanged = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequiresPasswordChange = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EncryptedNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    NotesNonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NotesAuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Password = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LastAutoFill = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequiresMasterPassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginItems_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoginItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                        name: "FK_PasskeyItem_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PasskeyItem_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PasswordItemTags",
                columns: table => new
                {
                    PasswordItemsId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordItemTags", x => new { x.PasswordItemsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_PasswordItemTags_PasswordItems_PasswordItemsId",
                        column: x => x.PasswordItemsId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PasswordItemTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecureNoteItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    EncryptedContent = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    ContentNonce = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContentAuthTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsMarkdown = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRichText = table.Column<bool>(type: "INTEGER", nullable: false),
                    AttachmentPaths = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsHighSecurity = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsShared = table.Column<bool>(type: "INTEGER", nullable: false),
                    SharedWith = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    LastEditedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastUsed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresMasterPassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecureNoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecureNoteItems_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecureNoteItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WiFiItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    NetworkName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SecurityType = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    IsHidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    IPAddress = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    SubnetMask = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    DefaultGateway = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    PrimaryDNS = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    SecondaryDNS = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    UseStaticIP = table.Column<bool>(type: "INTEGER", nullable: false),
                    RouterBrand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RouterModel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RouterIP = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    RouterUsername = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RouterPassword = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RouterAdminUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Bandwidth = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    WirelessStandard = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SignalStrength = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    MacAddress = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    BSSID = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    ISPName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PlanType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DownloadSpeed = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    UploadSpeed = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DataLimit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ISPPhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    InstallationAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Building = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Floor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Room = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    GuestNetworkName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GuestNetworkPassword = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HasGuestNetwork = table.Column<bool>(type: "INTEGER", nullable: false),
                    QRCodeData = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LastUsed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RequiresMasterPassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiFiItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WiFiItems_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WiFiItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CollectionId",
                table: "Categories",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId",
                table: "Categories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildPermissionConfig_ChildUserId",
                table: "ChildPermissionConfig",
                column: "ChildUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildPermissionConfig_ParentUserId",
                table: "ChildPermissionConfig",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_ParentCollectionId",
                table: "Collections",
                column: "ParentCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_UserId",
                table: "Collections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardItems_PasswordItemId",
                table: "CreditCardItems",
                column: "PasswordItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardItems_UserId",
                table: "CreditCardItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFields_PasswordItemId",
                table: "CustomFields",
                column: "PasswordItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId",
                table: "Devices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginItems_PasswordItemId",
                table: "LoginItems",
                column: "PasswordItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginItems_UserId",
                table: "LoginItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_CreatedAt",
                table: "OtpCodes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId_ExpiresAt",
                table: "OtpCodes",
                columns: new[] { "UserId", "ExpiresAt" });

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
                name: "IX_PasswordItems_CategoryId",
                table: "PasswordItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordItems_CollectionId",
                table: "PasswordItems",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordItems_UserId",
                table: "PasswordItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordItemTags_TagsId",
                table: "PasswordItemTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_SecureNoteItems_PasswordItemId",
                table: "SecureNoteItems",
                column: "PasswordItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecureNoteItems_UserId",
                table: "SecureNoteItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsSettings_IsActive",
                table: "SmsSettings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SmsSettings_UserId",
                table: "SmsSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsSettings_UserId_IsActive",
                table: "SmsSettings",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId",
                table: "Tags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBackupSettings_NextBackupAt",
                table: "UserBackupSettings",
                column: "NextBackupAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserBackupSettings_UserId",
                table: "UserBackupSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPasskeys_UserId",
                table: "UserPasskeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRelationship_ChildUserId",
                table: "UserRelationship",
                column: "ChildUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRelationship_ParentUserId",
                table: "UserRelationship",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTwoFactorBackupCodes_UserId",
                table: "UserTwoFactorBackupCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WiFiItems_PasswordItemId",
                table: "WiFiItems",
                column: "PasswordItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WiFiItems_UserId",
                table: "WiFiItems",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ChildPermissionConfig");

            migrationBuilder.DropTable(
                name: "CreditCardItems");

            migrationBuilder.DropTable(
                name: "CustomFields");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "LoginItems");

            migrationBuilder.DropTable(
                name: "OtpCodes");

            migrationBuilder.DropTable(
                name: "PasskeyItem");

            migrationBuilder.DropTable(
                name: "PasswordItemTags");

            migrationBuilder.DropTable(
                name: "QrLoginTokens");

            migrationBuilder.DropTable(
                name: "SecureNoteItems");

            migrationBuilder.DropTable(
                name: "SmsSettings");

            migrationBuilder.DropTable(
                name: "UserBackupSettings");

            migrationBuilder.DropTable(
                name: "UserPasskeys");

            migrationBuilder.DropTable(
                name: "UserRelationship");

            migrationBuilder.DropTable(
                name: "UserTwoFactorBackupCodes");

            migrationBuilder.DropTable(
                name: "WiFiItems");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "PasswordItems");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Collections");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
