using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using InventoryManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompanyProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CompanyProfileController> _logger;
        private readonly IFiscalizationService _fiscalizationService;

        public CompanyProfileController(AppDbContext context, ILogger<CompanyProfileController> logger, IWebHostEnvironment environment, IFiscalizationService fiscalizationService)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _fiscalizationService = fiscalizationService;
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
                    InvoiceParam1 = profile.InvoiceParam1,
                    InvoiceParam2 = profile.InvoiceParam2,
                    OfferParam1 = profile.OfferParam1,
                    OfferParam2 = profile.OfferParam2,
                    LastInvoiceNumber = profile.LastInvoiceNumber,
                    LastOfferNumber = profile.LastOfferNumber,
                    DefaultTaxRate = profile.DefaultTaxRate,
                    InPDV = profile.InPDV,
                    FiscalizationEnabled = profile.FiscalizationEnabled,
                    HasCertificate = !string.IsNullOrEmpty(profile.FiscalizationCertificatePath),
                    FiscalizationOib = profile.FiscalizationOib,
                    AutoFiscalize = profile.AutoFiscalize,
                    FiscalizationOperatorOib = profile.FiscalizationOperatorOib,
                    Description = profile.Description
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
                    InvoiceParam1 = request.InvoiceParam1,
                    InvoiceParam2 = request.InvoiceParam2,
                    OfferParam1 = request.OfferParam1,
                    OfferParam2 = request.OfferParam2,
                    DefaultTaxRate = request.DefaultTaxRate,
                    InPDV = request.InPDV,
                    LogoUrl = request.LogoUrl,
                    Description = request.Description
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
                    InvoiceParam1 = profile.InvoiceParam1,
                    InvoiceParam2 = profile.InvoiceParam2,
                    OfferParam1 = profile.OfferParam1,
                    OfferParam2 = profile.OfferParam2,
                    LastInvoiceNumber = profile.LastInvoiceNumber,
                    LastOfferNumber = profile.LastOfferNumber,
                    DefaultTaxRate = profile.DefaultTaxRate,
                    InPDV = profile.InPDV,
                    Description = profile.Description
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
                profile.InvoiceParam1 = request.InvoiceParam1;
                profile.InvoiceParam2 = request.InvoiceParam2;
                profile.OfferParam1 = request.OfferParam1;
                profile.OfferParam2 = request.OfferParam2;
                profile.DefaultTaxRate = request.DefaultTaxRate;
                profile.InPDV = request.InPDV;
                profile.FiscalizationEnabled = request.FiscalizationEnabled;
                profile.AutoFiscalize = request.AutoFiscalize;
                profile.LogoUrl = request.LogoUrl;
                profile.Description = request.Description;
                profile.FiscalizationOib = request.Oib; 
                profile.FiscalizationOperatorOib = request.Oib; // Ili neki drugi OIB


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
                    InvoiceParam1 = profile.InvoiceParam1,
                    InvoiceParam2 = profile.InvoiceParam2,
                    OfferParam1 = profile.OfferParam1,
                    OfferParam2 = profile.OfferParam2,
                    LastInvoiceNumber = profile.LastInvoiceNumber,
                    LastOfferNumber = profile.LastOfferNumber,
                    DefaultTaxRate = profile.DefaultTaxRate,
                    InPDV = profile.InPDV,
                    FiscalizationEnabled = profile.FiscalizationEnabled,
                    AutoFiscalize = profile.AutoFiscalize,
                    FiscalizationOib = profile.FiscalizationOib,
                    FiscalizationOperatorOib = profile.FiscalizationOperatorOib,
                    Description = profile.Description
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
        // Controllers/CompanyProfilesController.cs

        private async Task<CompanyProfile?> GetSelectedCompanyAsync()
        {
            // Dohvati userId iz JWT tokena
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return null;

            // Dohvati CompanyProfile za tog usera
            var companyProfile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId.ToString() == userId);

            return companyProfile;
        }

        [HttpPost("upload-certificate")]
        public async Task<ActionResult<ApiResponse<object>>> UploadCertificate([FromForm] UploadCertificateRequest request)
        {
            try
            {
                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ne postoji tvrtka"
                    });
                }

                if (request.Certificate == null || request.Certificate.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Certifikat je obavezan"
                    });
                }

                // Validacija ekstenzije
                var extension = Path.GetExtension(request.Certificate.FileName).ToLowerInvariant();
                if (extension != ".pfx" && extension != ".p12")
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Dozvoljeni su samo .pfx ili .p12 certifikati"
                    });
                }

                // Validacija veličine (max 5MB)
                if (request.Certificate.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Certifikat ne smije biti veći od 5MB"
                    });
                }

                // Kreiraj folder za certifikate
                var certificatesFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "certificates", companyProfile.Id.ToString());
                Directory.CreateDirectory(certificatesFolder);

                // Generiraj unique filename
                var fileName = $"fiscal_cert_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(certificatesFolder, fileName);

                // Spremi certifikat
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Certificate.CopyToAsync(stream);
                }

                // Testiraj da li se certifikat može učitati
                try
                {
                    var testCert = new X509Certificate2(filePath, request.Password, X509KeyStorageFlags.Exportable);

                    if (!testCert.HasPrivateKey)
                    {
                        System.IO.File.Delete(filePath);
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Certifikat ne sadrži privatni ključ"
                        });
                    }

                    // Provjeri validnost
                    if (testCert.NotAfter < DateTime.Now)
                    {
                        System.IO.File.Delete(filePath);
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Certifikat je istekao"
                        });
                    }

                    _logger.LogInformation("Certificate loaded successfully. Subject: {Subject}, Valid until: {NotAfter}",
                        testCert.Subject, testCert.NotAfter);
                }
                catch (CryptographicException)
                {
                    System.IO.File.Delete(filePath);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Neispravna lozinka certifikata ili oštećen certifikat"
                    });
                }

                // Obriši stari certifikat ako postoji
                if (!string.IsNullOrEmpty(companyProfile.FiscalizationCertificatePath))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, companyProfile.FiscalizationCertificatePath);
                    if (System.IO.File.Exists(oldPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not delete old certificate");
                        }
                    }
                }

                // Spremi relativni path i password u bazu
                var relativePath = Path.Combine("certificates", companyProfile.Id.ToString(), fileName);
                companyProfile.FiscalizationCertificatePath = relativePath;
                companyProfile.FiscalizationCertificatePassword = request.Password; // TODO: Enkriptirati!

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Certifikat uspješno učitan",
                    Data = new { hasCertificate = true }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading certificate");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Greška prilikom učitavanja certifikata: {ex.Message}"
                });
            }
        }
    }
}