using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PasswordManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ADDmigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentCollectionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_Collections_ParentCollectionId",
                        column: x => x.ParentCollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSystemTag = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "CreditCardItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CardholderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CardNumber = table.Column<string>(type: "TEXT", maxLength: 19, nullable: true),
                    ExpiryDate = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    CVV = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    PIN = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    CardType = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuingBank = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ValidFrom = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    BankWebsite = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "LoginItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Website = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TotpSecret = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TwoFactorType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityQuestion1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityAnswer1 = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SecurityQuestion2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityAnswer2 = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SecurityQuestion3 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SecurityAnswer3 = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
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
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
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
                    Content = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
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
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "WiFiItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    NetworkName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SecurityType = table.Column<int>(type: "INTEGER", nullable: false),
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
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
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
                });

            migrationBuilder.InsertData(
                table: "Collections",
                columns: new[] { "Id", "Color", "CreatedAt", "Description", "Icon", "IsDefault", "Name" },
                values: new object[,]
                {
                    { 1, "#3B82F6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "👤", true, "Personal" },
                    { 2, "#10B981", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "💼", false, "Work" },
                    { 3, "#8B5CF6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "👪", false, "Family" }
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Color", "CreatedAt", "Description", "IsSystemTag", "Name" },
                values: new object[,]
                {
                    { 1, "#3B82F6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Personal" },
                    { 2, "#10B981", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Work" },
                    { 3, "#F59E0B", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Finance" },
                    { 4, "#8B5CF6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Social" },
                    { 5, "#EF4444", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Shopping" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CollectionId", "Color", "CreatedAt", "Icon", "Name" },
                values: new object[,]
                {
                    { 1, 1, "#3B82F6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "👤", "Personal" },
                    { 2, 2, "#10B981", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "💼", "Work" },
                    { 3, 1, "#F59E0B", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "💰", "Finance" },
                    { 4, 1, "#8B5CF6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "🌐", "Social" },
                    { 5, 1, "#EF4444", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "🛒", "Shopping" },
                    { 6, 3, "#EC4899", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "🎮", "Entertainment" },
                    { 7, 3, "#06B6D4", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "✈️", "Travel" },
                    { 8, 1, "#84CC16", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "🏥", "Health" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CollectionId",
                table: "Categories",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_Name",
                table: "Collections",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardItems_PasswordItemId",
                table: "CreditCardItems",
                column: "PasswordItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginItems_PasswordItemId",
                table: "LoginItems",
                column: "PasswordItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordItems_CategoryId",
                table: "PasswordItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordItems_CollectionId",
                table: "PasswordItems",
                column: "CollectionId");

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
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WiFiItems_PasswordItemId",
                table: "WiFiItems",
                column: "PasswordItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardItems");

            migrationBuilder.DropTable(
                name: "LoginItems");

            migrationBuilder.DropTable(
                name: "PasswordItemTags");

            migrationBuilder.DropTable(
                name: "SecureNoteItems");

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
        }
    }
}
