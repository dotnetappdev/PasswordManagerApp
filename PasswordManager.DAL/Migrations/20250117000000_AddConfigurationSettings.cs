using Microsoft.EntityFrameworkCore.Migrations;

namespace PasswordManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigurationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConfigType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystemLevel = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSettings_GroupTypeKeyUser",
                table: "ConfigurationSettings",
                columns: new[] { "GroupKey", "ConfigType", "Key", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSettings_UserId",
                table: "ConfigurationSettings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationSettings");
        }
    }
}