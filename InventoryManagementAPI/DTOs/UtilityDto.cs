using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.DTOs
{
    public class UtilityInvoiceResponse
    {
        public int Id { get; set; }
        public string Building { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ValidityDate { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string CustomerOib { get; set; } = string.Empty;
        public string ServiceTypeHot { get; set; } = string.Empty;
        public string ServiceTypeHeating { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string DebtText { get; set; } = string.Empty;
        public string ConsumptionText { get; set; } = string.Empty;
        
        // Fiscalization fields
        public string FiscalizationStatus { get; set; } = string.Empty;
        public string? FiscalizationMethod { get; set; } // "fina" or "moje-racun"
        public DateTime? FiscalizedAt { get; set; }
        public string? FiscalizationError { get; set; }
        
        // FINA 1.0 fields
        public string? Jir { get; set; }
        public string? Zki { get; set; }
        
        // mojE-Račun 2.0 fields
        public string? MojeRacunInvoiceId { get; set; }
        public DateTime? MojeRacunSubmittedAt { get; set; }
        public string? MojeRacunStatus { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<UtilityInvoiceItemResponse> Items { get; set; } = new();
        public List<UtilityConsumptionDataResponse> ConsumptionData { get; set; } = new();
    }

    public class UtilityInvoiceItemResponse
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public int ItemOrder { get; set; }
        
        // mojE-Račun fields
        public string KpdCode { get; set; } = string.Empty;
        public decimal TaxRate { get; set; }
        public string TaxCategoryCode { get; set; } = string.Empty;
    }

    public class UtilityConsumptionDataResponse
    {
        public int Id { get; set; }
        public string ParameterName { get; set; } = string.Empty;
        public decimal? ParameterValue { get; set; }
        public int ParameterOrder { get; set; }
    }

    public class ImportResult
    {
        public int ProcessedCount { get; set; }
        public int ImportedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class UtilityInvoiceListResponse
    {
        public int Id { get; set; }
        public string Building { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerOib { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string FiscalizationStatus { get; set; } = string.Empty;
        public string? FiscalizationMethod { get; set; } // "fina" or "moje-racun"
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
        public int ConsumptionDataCount { get; set; }
        
        // FINA 1.0
        public string? Jir { get; set; }
        public string? Zki { get; set; }
        
        // mojE-Račun 2.0
        public string? MojeRacunInvoiceId { get; set; }
        public string? MojeRacunStatus { get; set; }
        
        public DateTime? FiscalizedAt { get; set; }
    }

    public class FiscalizeUtilityInvoiceRequest
    {
        [Required]
        public int InvoiceId { get; set; }
    }

    public class FiscalizeUtilityWithCompanyRequest
    {
        public int? CompanyId { get; set; }
    }

    public class UtilityCompanyOption
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool FiscalizationEnabled { get; set; }
        public bool HasCertificate { get; set; }
        public bool MojeRacunEnabled { get; set; }
    }

    public class UpdateUtilityInvoiceRequest
    {
        // Customer Information
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerOib { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }

        // Invoice Information
        public string? InvoiceNumber { get; set; }
        public string? Building { get; set; }
        public string? Period { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ValidityDate { get; set; }
        public string? BankAccount { get; set; }
        public string? Model { get; set; }

        // Service Types
        public string? ServiceTypeHot { get; set; }
        public string? ServiceTypeHeating { get; set; }

        // Financial Information
        public decimal? SubTotal { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? TotalAmount { get; set; }

        // Additional Information
        public string? DebtText { get; set; }
        public string? ConsumptionText { get; set; }

        // Invoice Items - complete replacement
        public List<UpdateUtilityInvoiceItemRequest>? Items { get; set; }
    }

    public class UpdateUtilityInvoiceItemRequest
    {
        public int? Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public int ItemOrder { get; set; }
        
        // mojE-Račun fields (optional in update)
        public string? KpdCode { get; set; }
        public decimal? TaxRate { get; set; }
        public string? TaxCategoryCode { get; set; }
    }

    /// <summary>
    /// DTO for adding a new item to an existing utility invoice
    /// </summary>
    public class AddUtilityInvoiceItemRequest
    {
        [Required(ErrorMessage = "Opis stavke je obavezan")]
        [StringLength(500, ErrorMessage = "Opis može imati maksimalno 500 znakova")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jedinica mjere je obavezna")]
        [StringLength(20, ErrorMessage = "Jedinica mjere može imati maksimalno 20 znakova")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Količina je obavezna")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Količina mora biti veća od 0")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Jedinična cijena je obavezna")]
        [Range(0, double.MaxValue, ErrorMessage = "Jedinična cijena ne može biti negativna")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Iznos je obavezan")]
        [Range(0, double.MaxValue, ErrorMessage = "Iznos ne može biti negativan")]
        public decimal Amount { get; set; }

        /// <summary>
        /// KPD code for Croatian tax classification (required for mojE-Račun)
        /// Default: "35.30.11" (Heating and hot water supply)
        /// </summary>
        [StringLength(20)]
        public string? KpdCode { get; set; } = "35.30.11";

        /// <summary>
        /// Tax rate percentage (e.g., 5.00, 13.00, 25.00)
        /// </summary>
        [Range(0, 100, ErrorMessage = "Porezna stopa mora biti između 0 i 100")]
        public decimal TaxRate { get; set; } = 5.00m;

        /// <summary>
        /// Tax category code (S = Standard rate, Z = Zero rate, E = Exempt)
        /// </summary>
        [StringLength(5)]
        public string TaxCategoryCode { get; set; } = "S";

        /// <summary>
        /// If true, automatically recalculate invoice totals
        /// </summary>
        public bool RecalculateTotals { get; set; } = true;
    }
}