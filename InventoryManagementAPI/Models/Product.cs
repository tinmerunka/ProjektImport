using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        // New fields for invoicing
        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 25.00m; // Default 25% PDV

        [StringLength(20)]
        public string Unit { get; set; } = "kom"; // kom, kg, m², lit, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ImageUrl { get; set; } 

        // Navigation properties
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public int CompanyId { get; set; } 
        public CompanyProfile Company { get; set; }
    }
}