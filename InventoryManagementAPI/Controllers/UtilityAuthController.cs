using BCrypt.Net;
using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using InventoryManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/utility/[controller]")]
    public class UtilityAuthController : ControllerBase
    {
        private readonly UtilityDbContext _context;
        private readonly IJwtService _jwtService;

        public UtilityAuthController(UtilityDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
        {
            try
            {
                // Find user in utility database
                var user = await _context.Users
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

                // Generate JWT token (no company context needed for utility system)
                var token = _jwtService.GenerateToken(user);

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

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
        {
            try
            {
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

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

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
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = $"An error occurred during registration: {ex.Message}"
                });
            }
        }
    }
}