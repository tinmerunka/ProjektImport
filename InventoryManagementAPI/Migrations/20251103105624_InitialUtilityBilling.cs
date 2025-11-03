using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektImport.Migrations
{
    /// <inheritdoc />
    public partial class InitialUtilityBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UtilityInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Building = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerOib = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceTypeHot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceTypeHeating = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DebtText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConsumptionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FiscalizationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Jir = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Zki = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FiscalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiscalizationError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UtilityConsumptionData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilityInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParameterValue = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    ParameterOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityConsumptionData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityConsumptionData_UtilityInvoices_UtilityInvoiceId",
                        column: x => x.UtilityInvoiceId,
                        principalTable: "UtilityInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilityInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilityInvoiceId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ItemOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityInvoiceItems_UtilityInvoices_UtilityInvoiceId",
                        column: x => x.UtilityInvoiceId,
                        principalTable: "UtilityInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityConsumptionData_UtilityInvoiceId",
                table: "UtilityConsumptionData",
                column: "UtilityInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoiceItems_UtilityInvoiceId",
                table: "UtilityInvoiceItems",
                column: "UtilityInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoices_Building",
                table: "UtilityInvoices",
                column: "Building");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoices_CustomerCode",
                table: "UtilityInvoices",
                column: "CustomerCode");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoices_InvoiceNumber",
                table: "UtilityInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoices_Period",
                table: "UtilityInvoices",
                column: "Period");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UtilityConsumptionData");

            migrationBuilder.DropTable(
                name: "UtilityInvoiceItems");

            migrationBuilder.DropTable(
                name: "UtilityInvoices");
        }
    }
}
