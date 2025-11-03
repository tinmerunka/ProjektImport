using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public class UtilityInvoiceItem
    {
        public int Id { get; set; }

        public int UtilityInvoiceId { get; set; }
        public UtilityInvoice UtilityInvoice { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty; // OPIS1-5

        [StringLength(20)]
        public string Unit { get; set; } = string.Empty; // JED1-5

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; } // KOL1-5

        [Column(TypeName = "decimal(18,5)")]
        public decimal UnitPrice { get; set; } // CIJ1-5

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // IZN1-5

        public int ItemOrder { get; set; } // 1-5 to maintain order
    }
}