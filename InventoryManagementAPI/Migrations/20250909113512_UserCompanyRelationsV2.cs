using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class UserCompanyRelationsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyProfiles_Users_UserId1",
                table: "CompanyProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_CompanyProfiles_CompanyId1",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_CompanyProfiles_CompanyId1",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_CompanyProfiles_CompanyId1",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId1",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CompanyId1",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId1",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_UserId1",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "CompanyProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "CompanyProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId1",
                table: "Products",
                column: "CompanyId1");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CompanyId1",
                table: "Invoices",
                column: "CompanyId1");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId1",
                table: "Customers",
                column: "CompanyId1");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_UserId1",
                table: "CompanyProfiles",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyProfiles_Users_UserId1",
                table: "CompanyProfiles",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_CompanyProfiles_CompanyId1",
                table: "Customers",
                column: "CompanyId1",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_CompanyProfiles_CompanyId1",
                table: "Invoices",
                column: "CompanyId1",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_CompanyProfiles_CompanyId1",
                table: "Products",
                column: "CompanyId1",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
