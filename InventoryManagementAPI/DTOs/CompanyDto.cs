using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.DTOs
{
    public class CompanyProfileRequest
    {
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string Oib { get; set; } = string.Empty;

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string BankAccount { get; set; } = string.Empty;

        [StringLength(200)]
        public string Website { get; set; } = string.Empty;

        [StringLength(50)]
        public string InvoiceParam1 { get; set; } = string.Empty;

        [StringLength(50)]
        public string InvoiceParam2 { get; set; } = string.Empty;

        [StringLength(50)]
        public string OfferParam1 { get; set; } = string.Empty;

        [StringLength(50)]
        public string OfferParam2 { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal DefaultTaxRate { get; set; } = 25.00m;
    }

    public class CompanyProfileResponse
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Oib { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string InvoiceParam1 { get; set; } = string.Empty;
        public string InvoiceParam2 { get; set; } = string.Empty;
        public string OfferParam1 { get; set; } = string.Empty;
        public string OfferParam2 { get; set; } = string.Empty;

        public int LastInvoiceNumber { get; set; }
        public int LastOfferNumber { get; set; }
        public decimal DefaultTaxRate { get; set; }
    }
}
