using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    /// <summary>
    /// Minimal Invoice model for FINA CIS fiscalization compatibility
    /// This is a legacy model used only for converting UtilityInvoice to FINA format
    /// For new invoicing, use UtilityInvoice with mojE-Raèun integration
    /// </summary>
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public InvoiceType Type { get; set; }
        
        public InvoiceStatus Status { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "EUR";

        [StringLength(200)]
        public string? IssueLocation { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? DeliveryDate { get; set; }

        // Customer Info
        public int CustomerId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(500)]
        public string CustomerAddress { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CustomerOib { get; set; }

        // Company Info (for fiscalization)
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string CompanyAddress { get; set; } = string.Empty;

        [StringLength(20)]
        public string CompanyOib { get; set; } = string.Empty;

        // Financial
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        // Payment
        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        [StringLength(10)]
        public string? PaymentMethodCode { get; set; }

        public string? Notes { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Fiscalization (FINA CIS)
        [StringLength(50)]
        public string FiscalizationStatus { get; set; } = "not_required";

        // Items (not used for utility invoices)
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    public enum InvoiceType
    {
        Invoice = 0,
        Offer = 1,
        ProformaInvoice = 2
    }

    public enum InvoiceStatus
    {
        Draft = 0,
        Sent = 1,
        Paid = 2,
        Overdue = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Minimal InvoiceItem for FINA compatibility
    /// </summary>
    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,5)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
    }
}
