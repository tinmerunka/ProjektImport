using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektImport.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProfileToUtility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Oib = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankAccount = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceParam1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceParam2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InPDV = table.Column<bool>(type: "bit", nullable: false),
                    OfferParam1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OfferParam2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastInvoiceNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastOfferNumber = table.Column<int>(type: "int", nullable: false),
                    DefaultTaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FiscalizationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    FiscalizationCertificatePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FiscalizationCertificatePassword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FiscalizationOib = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    FiscalizationOperatorOib = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    AutoFiscalize = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyProfiles");
        }
    }
}
