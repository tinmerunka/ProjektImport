using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.DTOs
{
    /// Response from mojE-Raèun fiscalization
    public class MojeRacunResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        /// Invoice ElectronicId from mojE-Raèun system
        public string? InvoiceId { get; set; }

        /// Status: In preparation, In validation, Sent, Delivered, Canceled, Unsuccessful
        public string? Status { get; set; }

        /// When the invoice was submitted to mojE-Raèun
        public DateTime? SubmittedAt { get; set; }

        /// Raw XML/JSON response from mojE-Raèun API
        public string? RawResponse { get; set; }
    }

    /// Request to fiscalize with method selection
    public class FiscalizeUtilityRequest
    {
        [Required]
        public string Method { get; set; } = "fina"; // "fina" or "moje-racun"

        public int? CompanyId { get; set; }
    }

    /// Filter for querying mojE-Raèun outbox
    public class OutboxQueryFilter
    {
        /// Filter by specific ElectronicId (invoice ID)
        public long? ElectronicId { get; set; }

        /// Filter by status:
        /// 10=In preparation, 20=In validation, 30=Sent, 40=Delivered, 45=Canceled, 50=Unsuccessful
        public int? StatusId { get; set; }

        /// Filter by invoice year
        public int? InvoiceYear { get; set; }

        /// Filter by invoice number
        public string? InvoiceNumber { get; set; }

        /// Start date filter (inclusive)
        public DateTime? From { get; set; }

        /// End date filter (inclusive)
        public DateTime? To { get; set; }
    }

    /// Request for querying outbox from controller
    public class OutboxQueryRequest
    {
        public int? CompanyId { get; set; }
        public long? ElectronicId { get; set; }
        public int? StatusId { get; set; }
        
        public int? InvoiceYear { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    /// Invoice header from mojE-Raèun outbox query
    public class OutboxInvoiceHeader
    {
        public string ElectronicId { get; set; } = string.Empty;
        public string DocumentNr { get; set; } = string.Empty;
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;

        /// Status ID:
        /// - 10 = In preparation (Document uploaded, pending validation)
        /// - 20 = In validation (Uploaded, validating recipient data)
        /// - 30 = Sent (Signed, timestamped, email sent to customer)
        /// - 40 = Delivered (Customer accepted and downloaded)
        /// - 45 = Canceled (Sender canceled, customer can't download anymore)
        /// - 50 = Unsuccessful (Customer didn't download in 5 days)
        public int StatusId { get; set; }

        /// Status name from mojE-Raèun system:
        /// - "In preparation" / "U pripremi"
        /// - "In validation" / "U validaciji"
        /// - "Sent" / "Poslan"
        /// - "Delivered" / "Dostavljen"
        /// - "Canceled" / "Otkazan"
        /// - "Unsuccessful" / "Neuspješan"
        public string StatusName { get; set; } = string.Empty;

        public string RecipientBusinessNumber { get; set; } = string.Empty;
        public string? RecipientBusinessUnit { get; set; }
        public string RecipientBusinessName { get; set; } = string.Empty;

        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime? Sent { get; set; }
        public DateTime? Delivered { get; set; }
    }
}
