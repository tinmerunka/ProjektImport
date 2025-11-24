using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public class CompanyProfile
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string Oib { get; set; } = string.Empty; // OIB in Croatia

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string BankAccount { get; set; } = string.Empty;

        [StringLength(200)]
        public string Website { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        // Invoice settings
        [StringLength(50)]
        public string InvoiceParam1 { get; set; } = string.Empty;

        [StringLength(50)]
        public string InvoiceParam2 { get; set; } = string.Empty;
        
        public bool InPDV { get; set; } = true;
        
        [StringLength(50)]
        public string OfferParam1 { get; set; } = string.Empty;

        [StringLength(50)]
        public string OfferParam2 { get; set; } = string.Empty;

        public int LastInvoiceNumber { get; set; } = 0;

        public string? Description { get; set; } = string.Empty;

        public int LastOfferNumber { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DefaultTaxRate { get; set; } = 25.00m;
        
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // ========================================
        // FINA 1.0 (CIS) FISCALIZATION
        // ========================================
        
        public bool FiscalizationEnabled { get; set; } = false;

        [StringLength(500)]
        public string? FiscalizationCertificatePath { get; set; }

        [StringLength(500)]
        public string? FiscalizationCertificatePassword { get; set; }

        [StringLength(11)]
        public string? FiscalizationOib { get; set; }

        [StringLength(11)]
        public string? FiscalizationOperatorOib { get; set; }

        public bool AutoFiscalize { get; set; } = true;

        // ========================================
        // mojE-Račun 2.0 FISCALIZATION
        // ========================================
        
        /// <summary>
        /// Enable mojE-Račun fiscalization
        /// </summary>
        public bool MojeRacunEnabled { get; set; } = false;

        /// <summary>
        /// Path to mojE-Račun certificate (.pfx)
        /// </summary>
        [StringLength(500)]
        public string? MojeRacunCertificatePath { get; set; }

        /// <summary>
        /// Certificate password (encrypted in production)
        /// </summary>
        [StringLength(500)]
        public string? MojeRacunCertificatePassword { get; set; }

        /// <summary>
        /// Environment: "test" or "production"
        /// </summary>
        [StringLength(20)]
        public string? MojeRacunEnvironment { get; set; } = "test";

        /// <summary>
        /// API Key for mojE-Račun (if using API key auth instead of certificate)
        /// </summary>
        [StringLength(500)]
        public string? MojeRacunApiKey { get; set; }

        /// <summary>
        /// OAuth Client ID (if using OAuth)
        /// </summary>
        [StringLength(200)]
        public string? MojeRacunClientId { get; set; }

        /// <summary>
        /// OAuth Client Secret (encrypted in production)
        /// </summary>
        [StringLength(500)]
        public string? MojeRacunClientSecret { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}