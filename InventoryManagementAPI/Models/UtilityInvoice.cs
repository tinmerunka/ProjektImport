using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public class UtilityInvoice
    {
        public int Id { get; set; }

        // Import Batch Tracking
        [Required]
        [StringLength(36)]
        public string? ImportBatchId { get; set; }
        // Building/Location Info
        [Required]
        [StringLength(200)]
        public string Building { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Period { get; set; } = string.Empty;

        // Invoice Info
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [StringLength(10)]
        public string Model { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ValidityDate { get; set; }

        [StringLength(50)]
        public string BankAccount { get; set; } = string.Empty;

        // Customer Info
        [Required]
        [StringLength(20)]
        public string CustomerCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(200)]
        public string CustomerAddress { get; set; } = string.Empty;

        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(20)]
        public string CustomerOib { get; set; } = string.Empty;

        // Service Type Info
        [StringLength(100)]
        public string ServiceTypeHot { get; set; } = string.Empty;
        [StringLength(100)]
        public string ServiceTypeHeating { get; set; } = string.Empty;

        // Financial Totals
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Additional Info
        [StringLength(500)]
        public string DebtText { get; set; } = string.Empty;

        [StringLength(1000)]
        public string ConsumptionText { get; set; } = string.Empty;

        // Fiscalization fields
        [StringLength(50)]
        public string FiscalizationStatus { get; set; } = "not_required";

        [StringLength(100)]
        public string? Jir { get; set; }

        [StringLength(100)]
        public string? Zki { get; set; }

        public DateTime? FiscalizedAt { get; set; }

        [StringLength(1000)]
        public string? FiscalizationError { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ImportBatch ImportBatch { get; set; } = null!;
        public ICollection<UtilityInvoiceItem> Items { get; set; } = new List<UtilityInvoiceItem>();
        public ICollection<UtilityConsumptionData> ConsumptionData { get; set; } = new List<UtilityConsumptionData>();
    }
}