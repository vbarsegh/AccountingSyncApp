using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure_Layer.Migrations
{
    /// <inheritdoc />
    public partial class FixInvoiceRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuoteDate",
                table: "Quotes",
                newName: "ExpiryDate");

            migrationBuilder.RenameColumn(
                name: "QuickBooksId",
                table: "Quotes",
                newName: "CustomerXeroId");

            migrationBuilder.RenameColumn(
                name: "CustomerName",
                table: "Quotes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "QuickBooksId",
                table: "Invoices",
                newName: "CustomerXeroId");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "Invoices",
                newName: "DueDate");

            migrationBuilder.AddColumn<bool>(
                name: "SyncedToXero",
                table: "Quotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SyncedToXero",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncedToXero",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SyncedToXero",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "ExpiryDate",
                table: "Quotes",
                newName: "QuoteDate");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Quotes",
                newName: "CustomerName");

            migrationBuilder.RenameColumn(
                name: "CustomerXeroId",
                table: "Quotes",
                newName: "QuickBooksId");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "Invoices",
                newName: "InvoiceDate");

            migrationBuilder.RenameColumn(
                name: "CustomerXeroId",
                table: "Invoices",
                newName: "QuickBooksId");
        }
    }
}
