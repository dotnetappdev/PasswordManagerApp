using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddVaultSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Vaults table
            migrationBuilder.CreateTable(
                name: "Vaults",
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
                    table.PrimaryKey("PK_Vaults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vaults_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add VaultId column to Categories table
            migrationBuilder.AddColumn<int>(
                name: "VaultId",
                table: "Categories",
                type: "INTEGER",
                nullable: true);

            // Add VaultId column to PasswordItems table
            migrationBuilder.AddColumn<int>(
                name: "VaultId",
                table: "PasswordItems",
                type: "INTEGER",
                nullable: true);

            // Create index on Vaults.UserId
            migrationBuilder.CreateIndex(
                name: "IX_Vaults_UserId",
                table: "Vaults",
                column: "UserId");

            // Create index on Categories.VaultId
            migrationBuilder.CreateIndex(
                name: "IX_Categories_VaultId",
                table: "Categories",
                column: "VaultId");

            // Create index on PasswordItems.VaultId
            migrationBuilder.CreateIndex(
                name: "IX_PasswordItems_VaultId",
                table: "PasswordItems",
                column: "VaultId");

            // Create foreign key for Categories.VaultId
            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Vaults_VaultId",
                table: "Categories",
                column: "VaultId",
                principalTable: "Vaults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Create foreign key for PasswordItems.VaultId
            migrationBuilder.AddForeignKey(
                name: "FK_PasswordItems_Vaults_VaultId",
                table: "PasswordItems",
                column: "VaultId",
                principalTable: "Vaults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Vaults_VaultId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_PasswordItems_Vaults_VaultId",
                table: "PasswordItems");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_PasswordItems_VaultId",
                table: "PasswordItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_VaultId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Vaults_UserId",
                table: "Vaults");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "VaultId",
                table: "PasswordItems");

            migrationBuilder.DropColumn(
                name: "VaultId",
                table: "Categories");

            // Drop Vaults table
            migrationBuilder.DropTable(
                name: "Vaults");
        }
    }
}
