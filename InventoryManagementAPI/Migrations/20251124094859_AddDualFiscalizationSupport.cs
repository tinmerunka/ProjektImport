using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektImport.Migrations
{
    /// <inheritdoc />
    public partial class AddDualFiscalizationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalizationMethod",
                table: "UtilityInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunInvoiceId",
                table: "UtilityInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunPdfUrl",
                table: "UtilityInvoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunQrCodeUrl",
                table: "UtilityInvoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunStatus",
                table: "UtilityInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MojeRacunSubmittedAt",
                table: "UtilityInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KpdCode",
                table: "UtilityInvoiceItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxCategoryCode",
                table: "UtilityInvoiceItems",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "UtilityInvoiceItems",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunApiKey",
                table: "CompanyProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunCertificatePassword",
                table: "CompanyProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunCertificatePath",
                table: "CompanyProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunClientId",
                table: "CompanyProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunClientSecret",
                table: "CompanyProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MojeRacunEnabled",
                table: "CompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MojeRacunEnvironment",
                table: "CompanyProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalizationMethod",
                table: "UtilityInvoices");

            migrationBuilder.DropColumn(
                name: "MojeRacunInvoiceId",
                table: "UtilityInvoices");

            migrationBuilder.DropColumn(
                name: "MojeRacunPdfUrl",
                table: "UtilityInvoices");

            migrationBuilder.DropColumn(
                name: "MojeRacunQrCodeUrl",
                table: "UtilityInvoices");

            migrationBuilder.DropColumn(
                name: "MojeRacunStatus",
                table: "UtilityInvoices");

            migrationBuilder.DropColumn(
                name: "MojeRacunSubmittedAt",
                table: "UtilityInvoices");

            migrationBuilder.DropColumn(
                name: "KpdCode",
                table: "UtilityInvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxCategoryCode",
                table: "UtilityInvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "UtilityInvoiceItems");

            migrationBuilder.DropColumn(
                name: "MojeRacunApiKey",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "MojeRacunCertificatePassword",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "MojeRacunCertificatePath",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "MojeRacunClientId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "MojeRacunClientSecret",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "MojeRacunEnabled",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "MojeRacunEnvironment",
                table: "CompanyProfiles");
        }
    }
}
