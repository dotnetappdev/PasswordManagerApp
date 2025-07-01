using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ADDCOLLECTIONS2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCollectionId",
                table: "Collections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Collections",
                keyColumn: "Id",
                keyValue: 1,
                column: "ParentCollectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Collections",
                keyColumn: "Id",
                keyValue: 2,
                column: "ParentCollectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Collections",
                keyColumn: "Id",
                keyValue: 3,
                column: "ParentCollectionId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_ParentCollectionId",
                table: "Collections",
                column: "ParentCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_Collections_ParentCollectionId",
                table: "Collections",
                column: "ParentCollectionId",
                principalTable: "Collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collections_Collections_ParentCollectionId",
                table: "Collections");

            migrationBuilder.DropIndex(
                name: "IX_Collections_ParentCollectionId",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "ParentCollectionId",
                table: "Collections");
        }
    }
}
