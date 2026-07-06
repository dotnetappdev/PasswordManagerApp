using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultGuard.DAL.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MasterPasswordHint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserSalt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MasterPasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MasterPasswordIterations = table.Column<int>(type: "int", nullable: false),
                    MasterKeyIdentifier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsTwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PhoneNumberConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BackupCodes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BackupCodesUsed = table.Column<int>(type: "int", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorSecretKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TwoFactorEnabledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TwoFactorRecoveryEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TwoFactorBackupCodesRemaining = table.Column<int>(type: "int", nullable: false),
                    PasskeysEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PasskeysEnabledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StorePasskeysInVault = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QrLoginTokens",
                columns: table => new
                {
                    Token = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrLoginTokens", x => x.Token);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: true),
                    ProviderConfig = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
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
                name: "ChildPermissionConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ParentUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CanViewPasswords = table.Column<bool>(type: "bit", nullable: false),
                    CanCreatePasswords = table.Column<bool>(type: "bit", nullable: false),
                    CanEditPasswords = table.Column<bool>(type: "bit", nullable: false),
                    CanDeletePasswords = table.Column<bool>(type: "bit", nullable: false),
                    CanRevealPasswords = table.Column<bool>(type: "bit", nullable: false),
                    CanSharePasswords = table.Column<bool>(type: "bit", nullable: false),
                    CanExportData = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateCollections = table.Column<bool>(type: "bit", nullable: false),
                    CanManageOwnCollections = table.Column<bool>(type: "bit", nullable: false),
                    MaxPasswordItems = table.Column<int>(type: "int", nullable: false),
                    AccessStartTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    AccessEndTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    AllowedDaysOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RequireParentApproval = table.Column<bool>(type: "bit", nullable: false),
                    LogActivities = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildPermissionConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildPermissionConfigs_AspNetUsers_ChildUserId",
                        column: x => x.ChildUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildPermissionConfigs_AspNetUsers_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Device",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrimaryDevice = table.Column<bool>(type: "bit", nullable: false)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RequestUserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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
                name: "SmsSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCountryCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CodeLength = table.Column<int>(type: "int", nullable: false),
                    ExpirationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    MaxSmsPerHour = table.Column<int>(type: "int", nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TwilioAccountSid = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TwilioAuthToken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TwilioFromPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AwsAccessKeyId = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AwsSecretAccessKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AwsRegion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AwsSenderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AzureConnectionString = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AzureFromPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSystemTag = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserBackupSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EnableCloudBackup = table.Column<bool>(type: "bit", nullable: false),
                    SelectedCloudProvider = table.Column<int>(type: "int", nullable: false),
                    AutoBackupEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxBackupsToKeep = table.Column<int>(type: "int", nullable: false),
                    BackupIntervalHours = table.Column<int>(type: "int", nullable: false),
                    BackupScheduleInterval = table.Column<int>(type: "int", nullable: false),
                    PreferredBackupTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CompressBackups = table.Column<bool>(type: "bit", nullable: false),
                    BackupFolderPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NetworkPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastBackupAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextBackupAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "UserPasskeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CredentialId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsBackedUp = table.Column<bool>(type: "bit", nullable: false),
                    RequiresUserVerification = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StoreInVault = table.Column<bool>(type: "bit", nullable: false),
                    EncryptedVaultData = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true)
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
                name: "UserRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChildUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRelationships_AspNetUsers_ChildUserId",
                        column: x => x.ChildUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRelationships_AspNetUsers_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserTwoFactorBackupCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CodeSalt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedFromIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Vault",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParentCollectionId = table.Column<int>(type: "int", nullable: true),
                    VaultId = table.Column<int>(type: "int", nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Collections_Collections_ParentCollectionId",
                        column: x => x.ParentCollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Collections_Vault_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vault",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CollectionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Categories_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PasswordItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    CollectionId = table.Column<int>(type: "int", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PasswordItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PasswordItems_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CreditCardItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasswordItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CardholderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CardNumber = table.Column<string>(type: "nvarchar(19)", maxLength: 19, nullable: true),
                    ExpiryDate = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    CVV = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    PIN = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    IssuingBank = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValidFrom = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    BankWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BankPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CustomerServicePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OnlineBankingUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OnlineBankingPassword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OnlineBankingUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreditLimit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InterestRate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CashAdvanceLimit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AvailableCredit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BillingAddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillingAddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillingCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BillingState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BillingZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BillingCountry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RewardsProgram = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RewardsNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BenefitsDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TravelInsurance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AirportLoungeAccess = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FraudAlertPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FraudAlertEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpirationMonth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpirationYear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresMasterPassword = table.Column<bool>(type: "bit", nullable: false),
                    PasswordId = table.Column<int>(type: "int", nullable: true),
                    EncryptedCardNumber = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CardNumberNonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CardNumberAuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EncryptedCvv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CvvNonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CvvAuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CreditCardItems_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomField",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsProtected = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PasswordItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomField_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoginItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasswordItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PasswordNonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PasswordAuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EncryptedTotpSecret = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotpNonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TotpAuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TwoFactorType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecurityQuestion1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EncryptedSecurityAnswer1 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SecurityAnswer1Nonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecurityAnswer1AuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecurityQuestion2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EncryptedSecurityAnswer2 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SecurityAnswer2Nonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecurityAnswer2AuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecurityQuestion3 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EncryptedSecurityAnswer3 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SecurityAnswer3Nonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecurityAnswer3AuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecoveryEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecoveryPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LoginUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupportUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AdminConsoleUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PasswordLastChanged = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresPasswordChange = table.Column<bool>(type: "bit", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EncryptedNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NotesNonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NotesAuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastAutoFill = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresMasterPassword = table.Column<bool>(type: "bit", nullable: false),
                    PasswordId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LoginItems_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasskeyItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasswordItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EncryptedCredentialId = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CredentialIdNonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CredentialIdAuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlatformName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsBackedUp = table.Column<bool>(type: "bit", nullable: false),
                    RequiresUserVerification = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    EncryptedNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NotesNonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NotesAuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
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
                name: "PasswordItemTag",
                columns: table => new
                {
                    PasswordItemsId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordItemTag", x => new { x.PasswordItemsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_PasswordItemTag_PasswordItems_PasswordItemsId",
                        column: x => x.PasswordItemsId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PasswordItemTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecureNoteItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasswordItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    EncryptedContent = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    ContentNonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentAuthTag = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsMarkdown = table.Column<bool>(type: "bit", nullable: false),
                    IsRichText = table.Column<bool>(type: "bit", nullable: false),
                    AttachmentPaths = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TemplateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsHighSecurity = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    SharedWith = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    LastEditedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    RequiresMasterPassword = table.Column<bool>(type: "bit", nullable: false),
                    PasswordId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecureNoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecureNoteItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SecureNoteItems_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WiFiItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasswordItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    NetworkName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SecurityType = table.Column<int>(type: "int", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    SubnetMask = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DefaultGateway = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PrimaryDNS = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    SecondaryDNS = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    UseStaticIP = table.Column<bool>(type: "bit", nullable: false),
                    RouterBrand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RouterModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RouterIP = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    RouterUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RouterPassword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RouterAdminUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Bandwidth = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WirelessStandard = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SignalStrength = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MacAddress = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    BSSID = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    ISPName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlanType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DownloadSpeed = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UploadSpeed = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DataLimit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ISPPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InstallationAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Building = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Room = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GuestNetworkName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GuestNetworkPassword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasGuestNetwork = table.Column<bool>(type: "bit", nullable: false),
                    QRCodeData = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequiresMasterPassword = table.Column<bool>(type: "bit", nullable: false),
                    PasswordId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiFiItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WiFiItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WiFiItems_PasswordItems_PasswordItemId",
                        column: x => x.PasswordItemId,
                        principalTable: "PasswordItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLog",
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
                name: "IX_ChildPermissionConfig_Unique",
                table: "ChildPermissionConfigs",
                columns: new[] { "ChildUserId", "ParentUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildPermissionConfigs_ParentUserId",
                table: "ChildPermissionConfigs",
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
                name: "IX_Collections_VaultId",
                table: "Collections",
                column: "VaultId");

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
                name: "IX_CustomField_PasswordItemId",
                table: "CustomField",
                column: "PasswordItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Device_UserId",
                table: "Device",
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
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId");

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
                name: "IX_PasswordItemTag_TagsId",
                table: "PasswordItemTag",
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
                name: "IX_SmsSettings_UserId",
                table: "SmsSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId",
                table: "Tags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBackupSettings_UserId",
                table: "UserBackupSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPasskeys_UserId",
                table: "UserPasskeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRelationship_Unique",
                table: "UserRelationships",
                columns: new[] { "ParentUserId", "ChildUserId", "RelationshipType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRelationships_ChildUserId",
                table: "UserRelationships",
                column: "ChildUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTwoFactorBackupCodes_UserId",
                table: "UserTwoFactorBackupCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vault_UserId",
                table: "Vault",
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
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "ChildPermissionConfigs");

            migrationBuilder.DropTable(
                name: "CreditCardItems");

            migrationBuilder.DropTable(
                name: "CustomField");

            migrationBuilder.DropTable(
                name: "Device");

            migrationBuilder.DropTable(
                name: "LoginItems");

            migrationBuilder.DropTable(
                name: "OtpCodes");

            migrationBuilder.DropTable(
                name: "PasskeyItem");

            migrationBuilder.DropTable(
                name: "PasswordItemTag");

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
                name: "UserRelationships");

            migrationBuilder.DropTable(
                name: "UserTwoFactorBackupCodes");

            migrationBuilder.DropTable(
                name: "WiFiItems");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "PasswordItems");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Collections");

            migrationBuilder.DropTable(
                name: "Vault");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
