using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.DTOs
{
    public class CompanyProfileRequest
    {
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string Oib { get; set; } = string.Empty;

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string BankAccount { get; set; } = string.Empty;

        [StringLength(200)]
        public string Website { get; set; } = string.Empty;

        [StringLength(50)]
        public string InvoiceParam1 { get; set; } = string.Empty;

        [StringLength(50)]
        public string InvoiceParam2 { get; set; } = string.Empty;

        [StringLength(50)]
        public string OfferParam1 { get; set; } = string.Empty;

        [StringLength(50)]
        public string OfferParam2 { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal DefaultTaxRate { get; set; } = 25.00m;

        public bool FiscalizationEnabled { get; set; }
        public bool InPDV { get; set; }
        public bool AutoFiscalize { get; set; }
        public string? LogoUrl { get; set; }

        public string? Description { get; set; }
    }

    public class UpdateCompanyProfileRequest
    {
        [StringLength(200)]
        public string? CompanyName { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Oib { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? BankAccount { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? InvoiceParam1 { get; set; }

        [StringLength(50)]
        public string? InvoiceParam2 { get; set; }

        [StringLength(50)]
        public string? OfferParam1 { get; set; }

        [StringLength(50)]
        public string? OfferParam2 { get; set; }

        public decimal? DefaultTaxRate { get; set; }
        public bool? FiscalizationEnabled { get; set; }
        public bool? InPDV { get; set; }
        public bool? AutoFiscalize { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        
        // FINA Configuration
        public string? FiscalizationOib { get; set; }
        public string? FiscalizationOperatorOib { get; set; }
        
        // mojE-Račun Configuration
        public bool? MojeRacunEnabled { get; set; }
        public string? MojeRacunEnvironment { get; set; }
        public string? MojeRacunClientId { get; set; }
        public string? MojeRacunClientSecret { get; set; }
        public string? MojeRacunApiKey { get; set; }
    }

    public class CompanyProfileResponse
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Oib { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string InvoiceParam1 { get; set; } = string.Empty;
        public string InvoiceParam2 { get; set; } = string.Empty;
        public string OfferParam1 { get; set; } = string.Empty;
        public string OfferParam2 { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int LastInvoiceNumber { get; set; }
        public int LastOfferNumber { get; set; }
        public decimal DefaultTaxRate { get; set; }
        public bool InPDV { get; set; }
        
        // FINA Fiscalization
        public bool FiscalizationEnabled { get; set; }
        public bool HasCertificate { get; set; }
        public string? FiscalizationOib { get; set; }
        public bool AutoFiscalize { get; set; }
        public string? FiscalizationOperatorOib { get; set; }
        
        // mojE-Račun Configuration
        public bool MojeRacunEnabled { get; set; }
        public string? MojeRacunEnvironment { get; set; }
        public string? MojeRacunClientId { get; set; }
        // Do NOT return MojeRacunClientSecret or MojeRacunApiKey for security
        public bool HasMojeRacunCredentials { get; set; }
    }

    public class UpdateFiscalizationSettingsRequest
    {
        public bool FiscalizationEnabled { get; set; }
        public string? FiscalizationOib { get; set; }
        public string? FiscalizationOperatorOib { get; set; }
        public bool AutoFiscalize { get; set; }
    }

    public class UploadCertificateRequest
    {
        [Required]
        public IFormFile Certificate { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }
    
    // FINA Fiscalization Response (for old Invoice model)
    public class FiscalizationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Jir { get; set; }
        public string? Zki { get; set; }
        public DateTime? FiscalizedAt { get; set; }
        public string? RawResponse { get; set; }
    }
}