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
        public decimal PaidAmount { get; set; }

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
    }

    public class InvoiceResponse
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? DueDate { get; set; }

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
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<InvoiceItemResponse> Items { get; set; } = new List<InvoiceItemResponse>();
    }

    public class InvoiceItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public string? ProductDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
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
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    public class UpdateInvoiceStatusRequest
    {
        [Required]
        public InvoiceStatus Status { get; set; }
    }
}
