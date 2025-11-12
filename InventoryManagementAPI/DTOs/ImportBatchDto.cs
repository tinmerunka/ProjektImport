namespace InventoryManagementAPI.DTOs
{
    public class ImportBatchResponse
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; }
        public string? ImportedBy { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public string Status { get; set; } = string.Empty;
        public long ImportDurationMs { get; set; }
        public long FileSize { get; set; }
        public int InvoiceCount { get; set; }
        public decimal SuccessRate => TotalRecords > 0 ? (decimal)SuccessfulRecords / TotalRecords * 100 : 0;
    }

    public class ImportBatchListResponse
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public string Status { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
    }

    public class ImportStatsResponse
    {
        public int TotalBatches { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalSuccessful { get; set; }
        public int TotalFailed { get; set; }
        public decimal OverallSuccessRate { get; set; }
        public DateTime? LastImportDate { get; set; }
        public long AverageImportDurationMs { get; set; }
        public int FiscalizedInvoices { get; set; }
        public int PendingInvoices { get; set; }
    }
}