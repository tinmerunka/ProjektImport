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
        }

        public class RegisterRequest
        {
            [Required]
            [StringLength(50, MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;

            public string Role { get; set; } = "User";
        }

        public class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
        }

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
