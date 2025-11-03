using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementAPI.Models
{
    public class UtilityConsumptionData
    {
        public int Id { get; set; }

        public int UtilityInvoiceId { get; set; }
        public UtilityInvoice UtilityInvoice { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string ParameterName { get; set; } = string.Empty; // TXT_TAB1-10

        [Column(TypeName = "decimal(18,3)")]
        public decimal? ParameterValue { get; set; } // NUM_TAB1-10

        public int ParameterOrder { get; set; } // 1-10 to maintain order
    }
}