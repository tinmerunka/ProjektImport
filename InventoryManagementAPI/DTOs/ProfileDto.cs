using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace InventoryManagementAPI.DTOs
{
    public class UserProfileResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<CompanyProfileResponse> Companies { get; set; } = new();
    }

    public class UpdateUserProfileRequest
    {
        [StringLength(50)]
        public string? Username { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6)]
        public string? NewPassword { get; set; }
    }


    public class UpdateCompanyProfileRequest
    {
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Oib { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? BankAccount { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        public string? LogoUrl { get; set; }

        [StringLength(10)]
        public string? InvoicePrefix { get; set; }

        [StringLength(10)]
        public string? OfferPrefix { get; set; }

        [Range(0, 100)]
        public decimal DefaultTaxRate { get; set; } = 25.0m;
    }
}