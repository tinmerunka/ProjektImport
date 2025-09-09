namespace InventoryManagementAPI.DTOs
{
    using System.ComponentModel.DataAnnotations;

    namespace InventoryManagementAPI.DTOs
    {
        public class LoginRequest
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;
            public int? CompanyId { get; set; }
        }

        public class RegisterRequest
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
            public string Password { get; set; } = string.Empty;

            public string Role { get; set; } = "User";

            // Company information fields
            public string? CompanyName { get; set; }
            public string? CompanyAddress { get; set; }
            public string? CompanyOib { get; set; }
        }

        public class AuthResponse
        {
            public string Token { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool RequiresCompanySelection { get; set; } // Add this
            public List<CompanyOption> AvailableCompanies { get; set; } // Add this
        }
        public class CompanyOption
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
