using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductModelWithTaxAndUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomerTaxId",
                table: "Invoices",
                newName: "CustomerOib");

            migrationBuilder.RenameColumn(
                name: "CompanyTaxId",
                table: "Invoices",
                newName: "CompanyOib");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomerOib",
                table: "Invoices",
                newName: "CustomerTaxId");

            migrationBuilder.RenameColumn(
                name: "CompanyOib",
                table: "Invoices",
                newName: "CompanyTaxId");
        }
    }
}
