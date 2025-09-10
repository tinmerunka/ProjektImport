using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompanyProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyProfileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> GetCompanyProfile()
        {
            try
            {
                var profile = await _context.CompanyProfiles.FirstOrDefaultAsync();

                if (profile == null)
                {
                    return NotFound(new ApiResponse<CompanyProfileResponse>
                    {
                        Success = false,
                        Message = "Company profile not found. Create one first."
                    });
                }

                var response = new CompanyProfileResponse
                {
                    Id = profile.Id,
                    CompanyName = profile.CompanyName,
                    Address = profile.Address,
                    Oib = profile.Oib,
                    Email = profile.Email,
                    Phone = profile.Phone,
                    BankAccount = profile.BankAccount,
                    Website = profile.Website,
                    LogoUrl = profile.LogoUrl,
                    InvoicePrefix = profile.InvoicePrefix,
                    LastInvoiceNumber = profile.LastInvoiceNumber,
                    DefaultTaxRate = profile.DefaultTaxRate
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

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> CreateCompanyProfile(CompanyProfileRequest request)
        {
            try
            {
                // Check if profile already exists
                if (await _context.CompanyProfiles.AnyAsync())
                {
                    return BadRequest(new ApiResponse<CompanyProfileResponse>
                    {
                        Success = false,
                        Message = "Company profile already exists. Use PUT to update."
                    });
                }

                var profile = new CompanyProfile
                {
                    CompanyName = request.CompanyName,
                    Address = request.Address,
                    Oib = request.Oib,
                    Email = request.Email,
                    Phone = request.Phone,
                    BankAccount = request.BankAccount,
                    Website = request.Website,
                    InvoicePrefix = request.InvoicePrefix,
                    DefaultTaxRate = request.DefaultTaxRate
                };

                _context.CompanyProfiles.Add(profile);
                await _context.SaveChangesAsync();

                var response = new CompanyProfileResponse
                {
                    Id = profile.Id,
                    CompanyName = profile.CompanyName,
                    Address = profile.Address,
                    Oib = profile.Oib,
                    Email = profile.Email,
                    Phone = profile.Phone,
                    BankAccount = profile.BankAccount,
                    Website = profile.Website,
                    LogoUrl = profile.LogoUrl,
                    InvoicePrefix = profile.InvoicePrefix,
                    LastInvoiceNumber = profile.LastInvoiceNumber,
                    DefaultTaxRate = profile.DefaultTaxRate
                };

                return CreatedAtAction(nameof(GetCompanyProfile), new ApiResponse<CompanyProfileResponse>
                {
                    Success = true,
                    Message = "Company profile created successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CompanyProfileResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating company profile"
                });
            }
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> UpdateCompanyProfile(CompanyProfileRequest request)
        {
            try
            {
                var profile = await _context.CompanyProfiles.FirstOrDefaultAsync();

                if (profile == null)
                {
                    return NotFound(new ApiResponse<CompanyProfileResponse>
                    {
                        Success = false,
                        Message = "Company profile not found. Create one first."
                    });
                }

                profile.CompanyName = request.CompanyName;
                profile.Address = request.Address;
                profile.Oib = request.Oib;
                profile.Email = request.Email;
                profile.Phone = request.Phone;
                profile.BankAccount = request.BankAccount;
                profile.Website = request.Website;
                profile.InvoicePrefix = request.InvoicePrefix;
                profile.OfferPrefix = request.OfferPrefix;
                profile.DefaultTaxRate = request.DefaultTaxRate;

                await _context.SaveChangesAsync();

                var response = new CompanyProfileResponse
                {
                    Id = profile.Id,
                    CompanyName = profile.CompanyName,
                    Address = profile.Address,
                    Oib = profile.Oib,
                    Email = profile.Email,
                    Phone = profile.Phone,
                    BankAccount = profile.BankAccount,
                    Website = profile.Website,
                    LogoUrl = profile.LogoUrl,
                    InvoicePrefix = profile.InvoicePrefix,
                    OfferPrefix = profile.OfferPrefix,
                    LastInvoiceNumber = profile.LastInvoiceNumber,
                    DefaultTaxRate = profile.DefaultTaxRate
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
    }
}