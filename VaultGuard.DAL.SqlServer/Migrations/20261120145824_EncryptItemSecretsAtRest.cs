using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultGuard.DAL.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class EncryptItemSecretsAtRest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedGuestNetworkPassword",
                table: "WiFiItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPassword",
                table: "WiFiItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedRouterPassword",
                table: "WiFiItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestNetworkPasswordAuthTag",
                table: "WiFiItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestNetworkPasswordNonce",
                table: "WiFiItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordAuthTag",
                table: "WiFiItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordNonce",
                table: "WiFiItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouterPasswordAuthTag",
                table: "WiFiItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouterPasswordNonce",
                table: "WiFiItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedOnlineBankingPassword",
                table: "CreditCardItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPin",
                table: "CreditCardItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineBankingPasswordAuthTag",
                table: "CreditCardItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineBankingPasswordNonce",
                table: "CreditCardItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinAuthTag",
                table: "CreditCardItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinNonce",
                table: "CreditCardItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedGuestNetworkPassword",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "EncryptedPassword",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "EncryptedRouterPassword",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "GuestNetworkPasswordAuthTag",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "GuestNetworkPasswordNonce",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "PasswordAuthTag",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "PasswordNonce",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "RouterPasswordAuthTag",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "RouterPasswordNonce",
                table: "WiFiItems");

            migrationBuilder.DropColumn(
                name: "EncryptedOnlineBankingPassword",
                table: "CreditCardItems");

            migrationBuilder.DropColumn(
                name: "EncryptedPin",
                table: "CreditCardItems");

            migrationBuilder.DropColumn(
                name: "OnlineBankingPasswordAuthTag",
                table: "CreditCardItems");

            migrationBuilder.DropColumn(
                name: "OnlineBankingPasswordNonce",
                table: "CreditCardItems");

            migrationBuilder.DropColumn(
                name: "PinAuthTag",
                table: "CreditCardItems");

            migrationBuilder.DropColumn(
                name: "PinNonce",
                table: "CreditCardItems");
        }
    }
}
