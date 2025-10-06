using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class ZaFiskalizaciju : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalisationMessage",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Fiscalized",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FiscalizedAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Jir",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodCode",
                table: "Invoices",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Zki",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InPDV",
                table: "CompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalisationMessage",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Fiscalized",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FiscalizedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Jir",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentMethodCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Zki",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InPDV",
                table: "CompanyProfiles");
        }
    }
}
