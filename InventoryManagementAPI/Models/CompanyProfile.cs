using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public class CompanyProfile
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string Oib { get; set; } = string.Empty; // OIB in Croatia

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string BankAccount { get; set; } = string.Empty;

        [StringLength(200)]
        public string Website { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        // Invoice settings
        [StringLength(50)]
        public string InvoiceParam1 { get; set; } = string.Empty; // First user input

        [StringLength(50)]
        public string InvoiceParam2 { get; set; } = string.Empty; // Second user input

        [StringLength(50)]
        public string OfferParam1 { get; set; } = string.Empty; // First user input

        [StringLength(50)]
        public string OfferParam2 { get; set; } = string.Empty; // Second user input

        public int LastInvoiceNumber { get; set; } = 0;

        public string? Description { get; set; } = string.Empty;

        public int LastOfferNumber { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DefaultTaxRate { get; set; } = 25.00m; // 25% PDV for Croatia
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Product> Products { get; set; }
        public ICollection<Customer> Customers { get; set; }
        public ICollection<Invoice> Invoices { get; set; }
    }
}