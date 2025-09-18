using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : CompanyBaseController
    {
        public ProfileController(AppDbContext context) : base(context)
        {
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<UserProfileResponse>>> GetProfile()
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users
                    .Include(u => u.Companies)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var response = new UserProfileResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    Companies = user.Companies.Select(c => new CompanyProfileResponse
                    {
                        Id = c.Id,
                        CompanyName = c.CompanyName,
                        Address = c.Address,
                        Oib = c.Oib,
                        Email = c.Email,
                        Phone = c.Phone,
                        BankAccount = c.BankAccount,
                        Website = c.Website,
                        LogoUrl = c.LogoUrl,
                        InvoiceParam1 = c.InvoiceParam1,
                        InvoiceParam2 = c.InvoiceParam2,
                        OfferParam1 = c.OfferParam1,
                        OfferParam2 = c.OfferParam2,
                        DefaultTaxRate = c.DefaultTaxRate
                    }).ToList()
                };

                return Ok(new ApiResponse<UserProfileResponse>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<UserProfileResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving profile"
                });
            }
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<UserProfileResponse>>> UpdateProfile(UpdateUserProfileRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = string.Join("; ", errors)
                    });
                }

                var userId = GetUserId();
                var user = await _context.Users
                    .Include(u => u.Companies)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Check if new username already exists (excluding current user)
                if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
                {
                    if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != userId))
                    {
                        return BadRequest(new ApiResponse<UserProfileResponse>
                        {
                            Success = false,
                            Message = "Username already exists"
                        });
                    }
                    user.Username = request.Username;
                }

                // Check if new email already exists (excluding current user)
                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId))
                    {
                        return BadRequest(new ApiResponse<UserProfileResponse>
                        {
                            Success = false,
                            Message = "Email already exists"
                        });
                    }
                    user.Email = request.Email;
                }

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(request.CurrentPassword) && !string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    // Verify current password
                    if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    {
                        return BadRequest(new ApiResponse<UserProfileResponse>
                        {
                            Success = false,
                            Message = "Current password is incorrect"
                        });
                    }

                    // Validate new password
                    if (!IsPasswordValid(request.NewPassword, out string passwordError))
                    {
                        return BadRequest(new ApiResponse<UserProfileResponse>
                        {
                            Success = false,
                            Message = passwordError
                        });
                    }

                    // With this line:
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                }

                await _context.SaveChangesAsync();

                var response = new UserProfileResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    Companies = user.Companies.Select(c => new CompanyProfileResponse
                    {
                        Id = c.Id,
                        CompanyName = c.CompanyName,
                        Address = c.Address,
                        Oib = c.Oib,
                        Email = c.Email,
                        Phone = c.Phone,
                        BankAccount = c.BankAccount,
                        Website = c.Website,
                        LogoUrl = c.LogoUrl,
                        InvoiceParam1 = c.InvoiceParam1,
                        InvoiceParam2 = c.InvoiceParam2,
                        OfferParam1 = c.OfferParam1,
                        OfferParam2 = c.OfferParam2,
                        DefaultTaxRate = c.DefaultTaxRate
                    }).ToList()
                };

                return Ok(new ApiResponse<UserProfileResponse>
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<UserProfileResponse>
                {
                    Success = false,
                    Message = "An error occurred while updating profile"
                });
            }
        }

        [HttpPut("company")]
        public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> UpdateCompanyProfile(UpdateCompanyProfileRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<CompanyProfileResponse>
                    {
                        Success = false,
                        Message = string.Join("; ", errors)
                    });
                }

                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<CompanyProfileResponse>
                    {
                        Success = false,
                        Message = "No valid company selected or company not found"
                    });
                }

                // Update company profile fields
                companyProfile.CompanyName = request.CompanyName;
                companyProfile.Address = request.Address ?? string.Empty;
                companyProfile.Oib = request.Oib ?? string.Empty;
                companyProfile.Email = request.Email ?? string.Empty;
                companyProfile.Phone = request.Phone ?? string.Empty;
                companyProfile.BankAccount = request.BankAccount ?? string.Empty;
                companyProfile.Website = request.Website ?? string.Empty;
                companyProfile.LogoUrl = request.LogoUrl;
                companyProfile.InvoiceParam1 = request.InvoiceParam1 ?? string.Empty;
                companyProfile.InvoiceParam2 = request.InvoiceParam2 ?? string.Empty;
                companyProfile.OfferParam1 = request.OfferParam1 ?? string.Empty;
                companyProfile.OfferParam2 = request.OfferParam2 ?? string.Empty;
                companyProfile.DefaultTaxRate = request.DefaultTaxRate;

                await _context.SaveChangesAsync();

                var response = new CompanyProfileResponse
                {
                    Id = companyProfile.Id,
                    CompanyName = companyProfile.CompanyName,
                    Address = companyProfile.Address,
                    Oib = companyProfile.Oib,
                    Email = companyProfile.Email,
                    Phone = companyProfile.Phone,
                    BankAccount = companyProfile.BankAccount,
                    Website = companyProfile.Website,
                    LogoUrl = companyProfile.LogoUrl,
                    InvoiceParam1 = companyProfile.InvoiceParam1,
                    InvoiceParam2 = companyProfile.InvoiceParam2,
                    OfferParam1 = companyProfile.OfferParam1,
                    OfferParam2 = companyProfile.OfferParam2,
                    DefaultTaxRate = companyProfile.DefaultTaxRate
                };

                return Ok(new ApiResponse<CompanyProfileResponse>
                {
                    Success = true,
                    Message = "Company profile updated successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CompanyProfileResponse>
                {
                    Success = false,
                    Message = "An error occurred while updating company profile"
                });
            }
        }

        [HttpGet("company")]
        public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> GetCompanyProfile()
        {
            try
            {
                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<CompanyProfileResponse>
                    {
                        Success = false,
                        Message = "No valid company selected or company not found"
                    });
                }

                var response = new CompanyProfileResponse
                {
                    Id = companyProfile.Id,
                    CompanyName = companyProfile.CompanyName,
                    Address = companyProfile.Address,
                    Oib = companyProfile.Oib,
                    Email = companyProfile.Email,
                    Phone = companyProfile.Phone,
                    BankAccount = companyProfile.BankAccount,
                    Website = companyProfile.Website,
                    LogoUrl = companyProfile.LogoUrl,
                    InvoiceParam1 = companyProfile.InvoiceParam1,
                    InvoiceParam2 = companyProfile.InvoiceParam2,
                    OfferParam1 = companyProfile.OfferParam1,
                    OfferParam2 = companyProfile.OfferParam2,
                    DefaultTaxRate = companyProfile.DefaultTaxRate
                };

                return Ok(new ApiResponse<CompanyProfileResponse>
                {
                    Success = true,
                    Message = "Company profile retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CompanyProfileResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving company profile"
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
    }
}