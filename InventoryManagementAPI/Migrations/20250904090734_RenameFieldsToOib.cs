using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameFieldsToOib : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaxId",
                table: "Customers",
                newName: "Oib");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_TaxId",
                table: "Customers",
                newName: "IX_Customers_Oib");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Oib",
                table: "Customers",
                newName: "TaxId");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_Oib",
                table: "Customers",
                newName: "IX_Customers_TaxId");
        }
    }
}
