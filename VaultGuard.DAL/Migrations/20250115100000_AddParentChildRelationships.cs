using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultGuard.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddParentChildRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create UserRelationships table
            migrationBuilder.CreateTable(
                name: "UserRelationships",
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

            // Create ChildPermissionConfigs table
            migrationBuilder.CreateTable(
                name: "ChildPermissionConfigs",
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

            // Create unique indexes
            migrationBuilder.CreateIndex(
                name: "IX_UserRelationship_Unique",
                table: "UserRelationships",
                columns: new[] { "ParentUserId", "ChildUserId", "RelationshipType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildPermissionConfig_Unique",
                table: "ChildPermissionConfigs",
                columns: new[] { "ChildUserId", "ParentUserId" },
                unique: true);

            // Create other indexes for foreign keys
            migrationBuilder.CreateIndex(
                name: "IX_UserRelationships_ChildUserId",
                table: "UserRelationships",
                column: "ChildUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRelationships_ParentUserId",
                table: "UserRelationships",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildPermissionConfigs_ChildUserId",
                table: "ChildPermissionConfigs",
                column: "ChildUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildPermissionConfigs_ParentUserId",
                table: "ChildPermissionConfigs",
                column: "ParentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildPermissionConfigs");

            migrationBuilder.DropTable(
                name: "UserRelationships");
        }
    }
}