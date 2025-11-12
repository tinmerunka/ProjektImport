using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public enum ImportBatchStatus
    {
        InProgress,
        Completed,
        Failed,
        PartiallyCompleted
    }

    public class ImportBatch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(36)]
        public string BatchId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? ImportedBy { get; set; }

        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public int SkippedRecords { get; set; }

        public ImportBatchStatus Status { get; set; } = ImportBatchStatus.InProgress;

        public string? ErrorLog { get; set; } // JSON string

        public long ImportDurationMs { get; set; }

        public long FileSize { get; set; }

        // Navigation property
        public ICollection<UtilityInvoice> Invoices { get; set; } = new List<UtilityInvoice>();
    }
}