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

        // ========================================
        // mojE-Račun 2.0 FIELDS
        // ========================================
        
        /// <summary>
        /// KPD Code (Klasifikacija proizvoda i usluga) - Required for mojE-Račun
        /// Default: 35.30.11 (Steam and hot water supply)
        /// </summary>
        [StringLength(20)]
        public string KpdCode { get; set; } = "35.30.11";

        /// <summary>
        /// VAT Rate for this specific item (can differ per item)
        /// ✅ UPDATED: Changed default from 13% to 5% for utility services
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 5.00m; // Default 5% for utilities

        /// <summary>
        /// VAT Category Code for UBL (S=Standard, Z=Zero, E=Exempt)
        /// </summary>
        [StringLength(2)]
        public string TaxCategoryCode { get; set; } = "S"; // Standard rate
    }
}