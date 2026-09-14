using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiInvoiceScanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "enable_invoice_archiving",
                table: "stores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "invoice_image_url",
                table: "purchases",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enable_invoice_archiving",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "invoice_image_url",
                table: "purchases");
        }
    }
}
