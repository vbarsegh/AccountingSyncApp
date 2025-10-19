using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure_Layer.Migrations
{
    /// <inheritdoc />
    public partial class InitClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_XeroTokensResponse",
                table: "XeroTokensResponse");

            migrationBuilder.RenameTable(
                name: "XeroTokensResponse",
                newName: "XeroTokenResponse");

            migrationBuilder.AddPrimaryKey(
                name: "PK_XeroTokenResponse",
                table: "XeroTokenResponse",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_XeroTokenResponse",
                table: "XeroTokenResponse");

            migrationBuilder.RenameTable(
                name: "XeroTokenResponse",
                newName: "XeroTokensResponse");

            migrationBuilder.AddPrimaryKey(
                name: "PK_XeroTokensResponse",
                table: "XeroTokensResponse",
                column: "Id");
        }
    }
}
