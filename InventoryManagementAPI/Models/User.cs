using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = "User"; // Default role

        public ICollection<CompanyProfile> Companies { get; set; } 
    }
}
