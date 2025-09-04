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
        Sent = 1,
        Paid = 2,
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

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

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

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
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

        [StringLength(500)]
        public string? ProductDescription { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTaxAmount { get; set; }

        [StringLength(20)]
        public string Unit { get; set; } = "kom"; // kom, kg, m2, etc.
    }
}
