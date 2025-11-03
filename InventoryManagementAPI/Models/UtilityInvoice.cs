using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public class UtilityInvoice
    {
        public int Id { get; set; }

        // Building/Location Info
        [Required]
        [StringLength(200)]
        public string Building { get; set; } = string.Empty; // ZGRADA

        [Required]
        [StringLength(50)]
        public string Period { get; set; } = string.Empty; // RAZDOBLJE

        // Invoice Info
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty; // BRRN

        [StringLength(10)]
        public string Model { get; set; } = string.Empty; // MODEL

        public DateTime IssueDate { get; set; } // DATISP
        public DateTime DueDate { get; set; } // DATRN
        public DateTime? ValidityDate { get; set; } // DATVAL

        [StringLength(50)]
        public string BankAccount { get; set; } = string.Empty; // RNIBAN

        // Customer Info
        [Required]
        [StringLength(20)]
        public string CustomerCode { get; set; } = string.Empty; // KKSIFRA

        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty; // KKIME

        [StringLength(200)]
        public string CustomerAddress { get; set; } = string.Empty; // KKADR

        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty; // KKPTT

        [StringLength(100)]
        public string City { get; set; } = string.Empty; // KKGRAD

        [StringLength(20)]
        public string CustomerOib { get; set; } = string.Empty; // KKOIB

        // Service Type Info
        [StringLength(100)]
        public string ServiceTypeHot { get; set; } = string.Empty; // TIP_TV
        [StringLength(100)]
        public string ServiceTypeHeating { get; set; } = string.Empty; // TIP_GRI

        // Financial Totals
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; } // UKUPNO_BEZ

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; } // PDV_1

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } // SVEUKUP

        // Additional Info
        [StringLength(500)]
        public string DebtText { get; set; } = string.Empty; // DUG_TXT

        [StringLength(1000)]
        public string ConsumptionText { get; set; } = string.Empty; // POTROS_TXT

        // Fiscalization fields (reuse from existing)
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
        public ICollection<UtilityInvoiceItem> Items { get; set; } = new List<UtilityInvoiceItem>();
        public ICollection<UtilityConsumptionData> ConsumptionData { get; set; } = new List<UtilityConsumptionData>();
    }
}