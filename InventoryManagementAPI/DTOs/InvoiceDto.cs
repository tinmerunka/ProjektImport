using InventoryManagementAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.DTOs
{
    public class CreateInvoiceRequest
    {
        [Required]
        public InvoiceType Type { get; set; } = InvoiceType.Invoice;

        [Required]
        public int CustomerId { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string IssueLocation { get; set; } = "Zagreb"; // Default location
        public decimal PaidAmount { get; set; }
        public string Currency { get; set; } = "EUR"; // Default currency
        public string PaymentMethod { get; set; } = "Transakcijski račun"; // Default method

        [StringLength(5)]
        public string? PaymentMethodCode { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public List<CreateInvoiceItemRequest> Items { get; set; } = new List<CreateInvoiceItemRequest>();

        [StringLength(50)]
        public string? CustomInvoiceNumber { get; set; }

    }

    public class CreateInvoiceItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0.001, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? OverridePrice { get; set; }

        [Range(0, 100)]
        public decimal? OverrideTaxRate { get; set; }

        // New discount field
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        // New override fields for product details
        [StringLength(200)]
        public string? OverrideProductName { get; set; }

        [StringLength(500)]
        public string? OverrideProductDescription { get; set; }
    }

    public class InvoiceResponse
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string IssueLocation { get; set; } = "Zagreb";


        // Customer info
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerOib { get; set; } = string.Empty;

        // Company info
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string CompanyOib { get; set; } = string.Empty;

        // Financial
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxRate { get; set; }
        public string TaxReason { get; set; } = "Manji porez";
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string PaymentMethod { get; set; } = "Transakcijski račun";
        public string? PaymentMethodCode { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Fiskalizacija
        public string FiscalizationStatus { get; set; } = "not_required";
        public string? Jir { get; set; }
        public string? Zki { get; set; }
        public DateTime? FiscalizedAt { get; set; }
        public string? FiscalizationError { get; set; }

        public List<InvoiceItemResponse> Items { get; set; } = new List<InvoiceItemResponse>();
    }

    public class InvoiceItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public string ProductKpdCode { get; set; } = string.Empty; // Add this
        public string? ProductDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal DiscountPercentage { get; set; } // Add this
        public decimal DiscountAmount { get; set; } // Add this
        public decimal TaxRate { get; set; }
        public decimal LineTotal { get; set; }
        public decimal LineTaxAmount { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class InvoiceListResponse
    {
        public List<InvoiceSummaryResponse> Invoices { get; set; } = new List<InvoiceSummaryResponse>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class InvoiceSummaryResponse
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime IssueDate { get; set; }
        public string IssueLocation { get; set; } = "Zagreb";
        public DateTime? DueDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Currency { get; set; } = "EUR";
        public string PaymentMethod { get; set; } = "Transakcijski račun";
        public string? PaymentMethodCode { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool Fiscalized { get; set; }
        public string? Jir { get; set; }
    }

    public class UpdateInvoiceStatusRequest
    {
        [Required]
        public InvoiceStatus Status { get; set; }
    }

    public class FiscalizeInvoiceRequest
    {
        [StringLength(50)]
        public string? Zki { get; set; }

        [StringLength(50)]
        public string? Jir { get; set; }

        [StringLength(500)]
        public string? FiscalisationMessage { get; set; }
    }
}