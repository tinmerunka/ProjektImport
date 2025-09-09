using BCrypt.Net;
using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using InventoryManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
        {
            try
            {
                // Validate model state first
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = string.Join("; ", errors)
                    });
                }

                // Additional password validation
                if (!IsPasswordValid(request.Password, out string passwordError))
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = passwordError
                    });
                }

                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Username already exists"
                    });
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Email already exists"
                    });
                }

                // Hash the password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    Role = request.Role ?? "User"
                };

                Console.WriteLine("Adding user to context...");
                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // This will generate the user ID
                Console.WriteLine($"User saved with ID: {user.Id}");

                // Create default company for the user
                var defaultCompany = new CompanyProfile
                {
                    CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? $"{request.Username}'s Company" : request.CompanyName,
                    Address = request.CompanyAddress ?? string.Empty,
                    Oib = request.CompanyOib ?? string.Empty,
                    Email = string.Empty,
                    Phone = string.Empty,
                    BankAccount = string.Empty, // This was missing and might be required
                    Website = string.Empty,
                    InvoicePrefix = "INV-",
                    OfferPrefix = "OFF-",
                    LastInvoiceNumber = 0,
                    LastOfferNumber = 0,
                    DefaultTaxRate = 25.0m,
                    UserId = user.Id // Set the foreign key
                };

                Console.WriteLine($"Adding company to context with UserId: {defaultCompany.UserId}...");
                _context.CompanyProfiles.Add(defaultCompany);

                try
                {
                    await _context.SaveChangesAsync(); // Save the company
                    Console.WriteLine($"Company saved with ID: {defaultCompany.Id}");
                }
                catch (Exception companyEx)
                {
                    Console.WriteLine($"Company save error: {companyEx.Message}");
                    Console.WriteLine($"Inner exception: {companyEx.InnerException?.Message}");
                    throw;
                }

                // Generate JWT token with the new company
                var token = _jwtService.GenerateToken(user, defaultCompany.Id);

                var authResponse = new AuthResponse
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    RequiresCompanySelection = false
                };

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "User registered successfully",
                    Data = authResponse
                });
            }
            catch (Exception ex)
            {
                // Log the actual exception for debugging
                Console.WriteLine($"Registration error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = $"An error occurred during registration: {ex.Message}"
                });
            }
        }

        private static bool IsPasswordValid(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Password is required";
                return false;
            }

            if (password.Length < 6)
            {
                errorMessage = "Password must be at least 6 characters long";
                return false;
            }

            if (password.Length > 100)
            {
                errorMessage = "Password must not exceed 100 characters";
                return false;
            }

            // Optional: Add more complex password requirements
            if (!password.Any(char.IsDigit))
            {
                errorMessage = "Password must contain at least one digit";
                return false;
            }

            if (!password.Any(char.IsLetter))
            {
                errorMessage = "Password must contain at least one letter";
                return false;
            }

            return true;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
        {
            try
            {
                // Find user with their companies
                var user = await _context.Users
                    .Include(u => u.Companies)
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                // Check if user has companies (this should not happen anymore with the updated registration)
                if (!user.Companies.Any())
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "No companies assigned to this user. Please contact administrator."
                    });
                }

                // If user has multiple companies and no company selected, require selection
                if (user.Companies.Count > 1 && !request.CompanyId.HasValue)
                {
                    return Ok(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Please select a company",
                        Data = new AuthResponse
                        {
                            RequiresCompanySelection = true,
                            AvailableCompanies = user.Companies.Select(c => new CompanyOption
                            {
                                Id = c.Id,
                                Name = c.CompanyName
                            }).ToList()
                        }
                    });
                }

                // Determine which company to use
                int selectedCompanyId;
                if (request.CompanyId.HasValue)
                {
                    // Validate selected company belongs to user
                    if (!user.Companies.Any(c => c.Id == request.CompanyId.Value))
                    {
                        return BadRequest(new ApiResponse<AuthResponse>
                        {
                            Success = false,
                            Message = "Invalid company selection"
                        });
                    }
                    selectedCompanyId = request.CompanyId.Value;
                }
                else
                {
                    // Use first company if only one exists
                    selectedCompanyId = user.Companies.First().Id;
                }

                // Generate JWT token with company context
                var token = _jwtService.GenerateToken(user, selectedCompanyId);

                var authResponse = new AuthResponse
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    RequiresCompanySelection = false
                };

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = authResponse
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }
        [HttpPost("switch-company")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> SwitchCompany([FromBody] int companyId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized();
                }

                var user = await _context.Users
                    .Include(u => u.Companies)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || !user.Companies.Any(c => c.Id == companyId))
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Company not found or access denied"
                    });
                }

                var newToken = _jwtService.GenerateToken(user, companyId);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Company switched successfully",
                    Data = new AuthResponse
                    {
                        Token = newToken,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An error occurred while switching company"
                });
            }
        }
    }
}