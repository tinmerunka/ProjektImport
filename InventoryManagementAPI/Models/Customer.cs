using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string Oib { get; set; } = string.Empty;

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        public bool IsCompany { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public int CompanyId { get; set; } 
        public CompanyProfile Company { get; set; }
    }

}
