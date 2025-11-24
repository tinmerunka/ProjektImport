using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.DTOs
{
    /// Response from mojE-Raèun fiscalization
    public class MojeRacunResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        
        /// Invoice UUID from mojE-Raèun system
        public string? InvoiceId { get; set; }
        
        /// QR code URL for the invoice
        public string? QrCodeUrl { get; set; }
        
        /// PDF download URL
        public string? PdfUrl { get; set; }
        
        /// Status: pending, accepted, rejected
        public string? Status { get; set; }
        
        public DateTime? SubmittedAt { get; set; }
        
        /// Raw XML/JSON response from mojE-Raèun
        public string? RawResponse { get; set; }
    }

    /// Request to fiscalize with method selection
    public class FiscalizeUtilityRequest
    {
        [Required]
        public string Method { get; set; } = "fina"; // "fina" or "moje-racun"
        
        public int? CompanyId { get; set; }
    }
}
