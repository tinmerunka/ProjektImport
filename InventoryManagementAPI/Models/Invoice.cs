using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public enum InvoiceType
    {
        Invoice = 0,  // Račun
        Offer = 1     // Ponuda
    }

    public enum InvoiceStatus
    {
        Draft = 0,
        Fiscalized = 1,
        Paid = 2,
        Finalized = 3,
        Cancelled = 3
    }
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public InvoiceType Type { get; set; } = InvoiceType.Invoice;

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public string Currency { get; set; } = "EUR"; // Default currency
        public string IssueLocation { get; set; } = "Zagreb"; // Default location
        public DateTime IssueDate { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? DeliveryDate { get; set; }

        // Customer information (snapshot at time of invoice creation)
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(500)]
        public string CustomerAddress { get; set; } = string.Empty;

        [StringLength(50)]
        public string CustomerOib { get; set; } = string.Empty;

        // Company information (snapshot)
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string CompanyAddress { get; set; } = string.Empty;

        [StringLength(50)]
        public string CompanyOib { get; set; } = string.Empty;

        // Financial data
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }
        // Kombinirani razlog oslobođenja za cijeli račun (iz svih proizvoda s 0% PDV)
        // Ovo se automatski popunjava pri kreiranju računa
        [StringLength(1000)]
        public string? TaxExemptionSummary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        public string PaymentMethod { get; set; } = "Transakijski račun"; // Default method
        public string PaymentMethodCode { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Fiskalizacija
        [StringLength(50)]
        public string FiscalizationStatus { get; set; } = "not_required"; // not_required, pending, fiscalized, failed

        [StringLength(100)]
        public string? Jir { get; set; } // Jedinstveni identifikator računa

        [StringLength(100)]
        public string? Zki { get; set; } // Zaštitni kod izdavatelja

        public DateTime? FiscalizedAt { get; set; }

        [StringLength(1000)]
        public string? FiscalizationError { get; set; }

        // Navigation properties
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public int CompanyId { get; set; } 
        public CompanyProfile Company { get; set; }
    }

    public class InvoiceItem
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Product snapshot (in case product changes later)
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(50)]
        public string ProductSku { get; set; } = string.Empty;

        [StringLength(20)]
        public string ProductKpdCode { get; set; } = string.Empty; // Snapshot of KPD code

        [StringLength(500)]
        public string? ProductDescription { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        // Discount fields
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; } = 0; // 0-100%

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0; // Calculated discount amount

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; } // After discount

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTaxAmount { get; set; }

        [StringLength(20)]
        public string Unit { get; set; } = "kom"; // kom, kg, m2, etc.
    }
}
