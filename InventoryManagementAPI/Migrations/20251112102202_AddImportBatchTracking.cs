using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektImport.Migrations
{
    /// <inheritdoc />
    public partial class AddImportBatchTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UtilityInvoices_Building",
                table: "UtilityInvoices");

            migrationBuilder.DropIndex(
                name: "IX_UtilityInvoices_CustomerCode",
                table: "UtilityInvoices");

            migrationBuilder.DropIndex(
                name: "IX_UtilityInvoices_InvoiceNumber",
                table: "UtilityInvoices");

            migrationBuilder.DropIndex(
                name: "IX_UtilityInvoices_Period",
                table: "UtilityInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            // ✅ 1. Create ImportBatches table FIRST
            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    SuccessfulRecords = table.Column<int>(type: "int", nullable: false),
                    FailedRecords = table.Column<int>(type: "int", nullable: false),
                    SkippedRecords = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorLog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImportDurationMs = table.Column<long>(type: "bigint", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.UniqueConstraint("AK_ImportBatches_BatchId", x => x.BatchId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_BatchId",
                table: "ImportBatches",
                column: "BatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_ImportedAt",
                table: "ImportBatches",
                column: "ImportedAt");

            // ✅ 2. Add column as NULLABLE first
            migrationBuilder.AddColumn<string>(
                name: "ImportBatchId",
                table: "UtilityInvoices",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true); // NULLABLE!

            // ✅ 3. Create batches for existing invoices and assign BatchIds
            migrationBuilder.Sql(@"
        IF EXISTS (SELECT 1 FROM UtilityInvoices)
        BEGIN
            DECLARE @newBatchId NVARCHAR(36) = LOWER(NEWID());
            DECLARE @count INT = (SELECT COUNT(*) FROM UtilityInvoices);
            
            INSERT INTO ImportBatches (BatchId, FileName, ImportedAt, TotalRecords, SuccessfulRecords, FailedRecords, SkippedRecords, Status, ImportDurationMs, FileSize, ImportedBy)
            VALUES (@newBatchId, 'Legacy Import', GETUTCDATE(), @count, @count, 0, 0, 'Completed', 0, 0, 'Migration');
            
            UPDATE UtilityInvoices SET ImportBatchId = @newBatchId;
        END
    ");

            // ✅ 4. Make column NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "ImportBatchId",
                table: "UtilityInvoices",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36,
                oldNullable: true);

            // ... rest of your migration (Customer, Product, Invoice tables)
            // Keep all the CreateTable statements for Customer, Product, Invoice, InvoiceItem

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Oib = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCompany = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customer_CompanyProfiles_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KpdCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TaxReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_CompanyProfiles_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssueLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CustomerOib = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CompanyOib = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TaxExemptionSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentMethodCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiscalizationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Jir = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Zki = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FiscalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiscalizationError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoice_CompanyProfiles_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoice_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductKpdCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItem_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceItem_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create all indexes
            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoices_CreatedAt",
                table: "UtilityInvoices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoices_FiscalizationStatus",
                table: "UtilityInvoices",
                column: "FiscalizationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoices_ImportBatchId",
                table: "UtilityInvoices",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityInvoices_InvoiceNumber",
                table: "UtilityInvoices",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_UserId",
                table: "CompanyProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CompanyId",
                table: "Customer",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_CompanyId",
                table: "Invoice",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_CustomerId",
                table: "Invoice",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItem_InvoiceId",
                table: "InvoiceItem",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItem_ProductId",
                table: "InvoiceItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CompanyId",
                table: "Product",
                column: "CompanyId");

            // Add foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_CompanyProfiles_Users_UserId",
                table: "CompanyProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ✅ 5. Add foreign key LAST (after data is populated)
            migrationBuilder.AddForeignKey(
                name: "FK_UtilityInvoices_ImportBatches_ImportBatchId",
                table: "UtilityInvoices",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "BatchId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyProfiles_Users_UserId",
                table: "CompanyProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UtilityInvoices_ImportBatches_ImportBatchId",
                table: "UtilityInvoices");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "InvoiceItem");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropIndex(
                name: "IX_UtilityInvoices_CreatedAt",
                table: "UtilityInvoices");

            migrationBuilder.DropIndex(
                name: "IX_UtilityInvoices_FiscalizationStatus",
                table: "UtilityInvoices");

            migrationBuilder.DropIndex(
                name: "IX_UtilityInvoices_ImportBatchId",
                table: "UtilityInvoices");

            migrationBuilder.DropIndex(
                name: "IX_UtilityInvoices_InvoiceNumber",
                table: "UtilityInvoices");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_UserId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "UtilityInvoices");

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
        }
    }
}
