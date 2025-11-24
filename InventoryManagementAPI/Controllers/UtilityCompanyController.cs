using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/utility/[controller]")]
    [Authorize]
    public class UtilityCompanyController : ControllerBase
    {
        private readonly UtilityDbContext _context;
        private readonly ILogger<UtilityCompanyController> _logger;
        private readonly IWebHostEnvironment _environment;

        public UtilityCompanyController(
            UtilityDbContext context,
            ILogger<UtilityCompanyController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CompanyProfileResponse>>>> GetCompanyProfiles()
        {
            try
            {
                var companies = await _context.CompanyProfiles
                    .Select(c => new CompanyProfileResponse
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
                        DefaultTaxRate = c.DefaultTaxRate,
                        InPDV = c.InPDV,
                        FiscalizationEnabled = c.FiscalizationEnabled,
                        HasCertificate = !string.IsNullOrEmpty(c.FiscalizationCertificatePath),
                        FiscalizationOib = c.FiscalizationOib,
                        AutoFiscalize = c.AutoFiscalize,
                        FiscalizationOperatorOib = c.FiscalizationOperatorOib,
                        Description = c.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<CompanyProfileResponse>>
                {
                    Success = true,
                    Message = "Company profiles retrieved successfully",
                    Data = companies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company profiles");
                return StatusCode(500, new ApiResponse<List<CompanyProfileResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving company profiles"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> GetCompanyProfile(int id)
        {
            try
            {
                var company = await _context.CompanyProfiles.FindAsync(id);

                if (company == null)
                {
                    return NotFound(new ApiResponse<CompanyProfileResponse>
                    {
                        Success = false,
                        Message = "Company profile not found"
                    });
                }

                var response = new CompanyProfileResponse
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    Address = company.Address,
                    Oib = company.Oib,
                    Email = company.Email,
                    Phone = company.Phone,
                    BankAccount = company.BankAccount,
                    Website = company.Website,
                    LogoUrl = company.LogoUrl,
                    InvoiceParam1 = company.InvoiceParam1,
                    InvoiceParam2 = company.InvoiceParam2,
                    OfferParam1 = company.OfferParam1,
                    OfferParam2 = company.OfferParam2,
                    DefaultTaxRate = company.DefaultTaxRate,
                    InPDV = company.InPDV,
                    FiscalizationEnabled = company.FiscalizationEnabled,
                    HasCertificate = !string.IsNullOrEmpty(company.FiscalizationCertificatePath),
                    FiscalizationOib = company.FiscalizationOib,
                    AutoFiscalize = company.AutoFiscalize,
                    FiscalizationOperatorOib = company.FiscalizationOperatorOib,
                    Description = company.Description
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
                _logger.LogError(ex, "Error retrieving company profile {Id}", id);
                return StatusCode(500, new ApiResponse<CompanyProfileResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the company profile"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> CreateCompanyProfile(UpdateCompanyProfileRequest request)
        {
            try
            {
                var company = new CompanyProfile
                {
                    CompanyName = request.CompanyName,
                    Address = request.Address ?? string.Empty,
                    Oib = request.Oib ?? string.Empty,
                    Email = request.Email ?? string.Empty,
                    Phone = request.Phone ?? string.Empty,
                    BankAccount = request.BankAccount ?? string.Empty,
                    Website = request.Website ?? string.Empty,
                    InvoiceParam1 = request.InvoiceParam1,
                    InvoiceParam2 = request.InvoiceParam2,
                    OfferParam1 = request.OfferParam1,
                    OfferParam2 = request.OfferParam2,
                    DefaultTaxRate = request.DefaultTaxRate,
                    UserId = 1 // For utility system, use a default user ID or get from JWT
                };

                _context.CompanyProfiles.Add(company);
                await _context.SaveChangesAsync();

                var response = new CompanyProfileResponse
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    Address = company.Address,
                    Oib = company.Oib,
                    Email = company.Email,
                    Phone = company.Phone,
                    BankAccount = company.BankAccount,
                    Website = company.Website,
                    InvoiceParam1 = company.InvoiceParam1,
                    InvoiceParam2 = company.InvoiceParam2,
                    OfferParam1 = company.OfferParam1,
                    OfferParam2 = company.OfferParam2,
                    DefaultTaxRate = company.DefaultTaxRate,
                    InPDV = company.InPDV,
                    FiscalizationEnabled = company.FiscalizationEnabled,
                    HasCertificate = false,
                    AutoFiscalize = company.AutoFiscalize
                };

                return CreatedAtAction(nameof(GetCompanyProfile), new { id = company.Id }, new ApiResponse<CompanyProfileResponse>
                {
                    Success = true,
                    Message = "Company profile created successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company profile");
                return StatusCode(500, new ApiResponse<CompanyProfileResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating the company profile"
                });
            }
        }

        [HttpPost("{id}/certificate")]
        public async Task<ActionResult<ApiResponse<object>>> UploadCertificate(int id, IFormFile certificate, [FromForm] string? password = null)
        {
            try
            {
                var company = await _context.CompanyProfiles.FindAsync(id);
                if (company == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Company profile not found"
                    });
                }

                if (certificate == null || certificate.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No certificate file provided"
                    });
                }

                if (!certificate.FileName.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Only .pfx certificate files are allowed"
                    });
                }

                // Create certificates directory
                var certificatesDir = Path.Combine(_environment.WebRootPath, "certificates");
                if (!Directory.Exists(certificatesDir))
                {
                    Directory.CreateDirectory(certificatesDir);
                }

                // Generate unique filename
                var fileName = $"cert_{company.Id}_{Guid.NewGuid().ToString("N")[..8]}.pfx";
                var filePath = Path.Combine(certificatesDir, fileName);

                // Save certificate file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await certificate.CopyToAsync(fileStream);
                }

                // Test the certificate with the provided password (or empty)
                var testPassword = password ?? string.Empty;

                try
                {
                    var testCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        filePath,
                        testPassword,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable);

                    if (!testCert.HasPrivateKey)
                    {
                        System.IO.File.Delete(filePath); // Clean up
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Certificate does not contain a private key"
                        });
                    }

                    _logger.LogInformation("Certificate test successful: Subject={Subject}, HasPrivateKey={HasPrivateKey}",
                        testCert.Subject, testCert.HasPrivateKey);
                }
                catch (System.Security.Cryptography.CryptographicException ex)
                {
                    System.IO.File.Delete(filePath); // Clean up
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Certificate password is incorrect or certificate is invalid: {ex.Message}"
                    });
                }

                // Update company profile
                company.FiscalizationCertificatePath = Path.Combine("certificates", fileName);
                company.FiscalizationCertificatePassword = testPassword; // Store the working password (even if empty)
                company.FiscalizationEnabled = true;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Certificate uploaded and verified successfully",
                    Data = new
                    {
                        CertificatePath = company.FiscalizationCertificatePath,
                        FiscalizationEnabled = company.FiscalizationEnabled,
                        HasPassword = !string.IsNullOrEmpty(testPassword)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading certificate for company {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while uploading the certificate"
                });
            }
        }

        [HttpPost("{id}/test-certificate")]
        public async Task<ActionResult<ApiResponse<object>>> TestCertificate(int id)
        {
            try
            {
                var company = await _context.CompanyProfiles.FindAsync(id);
                if (company == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Company profile not found"
                    });
                }

                if (string.IsNullOrEmpty(company.FiscalizationCertificatePath))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No certificate uploaded"
                    });
                }

                var fullPath = Path.Combine(_environment.WebRootPath, company.FiscalizationCertificatePath);

                if (!System.IO.File.Exists(fullPath))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Certificate file not found on disk"
                    });
                }

                try
                {
                    var password = company.FiscalizationCertificatePassword ?? string.Empty;
                    var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        fullPath,
                        password,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable);

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Certificate loaded successfully",
                        Data = new
                        {
                            Subject = cert.Subject,
                            Issuer = cert.Issuer,
                            HasPrivateKey = cert.HasPrivateKey,
                            NotBefore = cert.NotBefore,
                            NotAfter = cert.NotAfter,
                            SerialNumber = cert.SerialNumber,
                            HasPassword = !string.IsNullOrEmpty(company.FiscalizationCertificatePassword)
                        }
                    });
                }
                catch (System.Security.Cryptography.CryptographicException ex)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Certificate test failed: {ex.Message}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing certificate for company {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while testing the certificate"
                });
            }
        }

        [HttpPut("{id}/fiscalization")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateFiscalizationSettings(int id, [FromBody] FiscalizationSettingsRequest request)
        {
            try
            {
                var company = await _context.CompanyProfiles.FindAsync(id);
                if (company == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Company profile not found"
                    });
                }

                company.FiscalizationEnabled = request.FiscalizationEnabled;
                company.FiscalizationOib = request.FiscalizationOib;
                company.FiscalizationOperatorOib = request.FiscalizationOperatorOib;
                company.AutoFiscalize = request.AutoFiscalize;
                company.InvoiceParam1 = request.InvoiceParam1 ?? company.InvoiceParam1;
                company.InvoiceParam2 = request.InvoiceParam2 ?? company.InvoiceParam2;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Fiscalization settings updated successfully",
                    Data = new
                    {
                        FiscalizationEnabled = company.FiscalizationEnabled,
                        FiscalizationOib = company.FiscalizationOib,
                        FiscalizationOperatorOib = company.FiscalizationOperatorOib,
                        AutoFiscalize = company.AutoFiscalize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fiscalization settings for company {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating fiscalization settings"
                });
            }
        }

        /// <summary>
        /// Configure mojE-Račun credentials (username/password/softwareId)
        /// </summary>
        [HttpPut("{id}/moje-racun")]
        public async Task<ActionResult<ApiResponse<object>>> ConfigureMojeRacun(int id, [FromBody] MojeRacunConfigRequest request)
        {
            try
            {
                var company = await _context.CompanyProfiles.FindAsync(id);
                if (company == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Company profile not found"
                    });
                }

                // Update mojE-Račun settings
                company.MojeRacunEnabled = request.Enabled;
                company.MojeRacunEnvironment = request.Environment ?? "test";
                
                // Store credentials (⚠️ WARNING: In production, encrypt these!)
                if (!string.IsNullOrEmpty(request.Username))
                {
                    company.MojeRacunClientId = request.Username;
                }

                if (!string.IsNullOrEmpty(request.Password))
                {
                    // ⚠️ TODO: Encrypt password before storing
                    company.MojeRacunClientSecret = request.Password;
                    _logger.LogWarning("Password stored in plain text! Implement encryption for production!");
                }

                if (!string.IsNullOrEmpty(request.SoftwareId))
                {
                    // Store SoftwareId in MojeRacunApiKey field
                    company.MojeRacunApiKey = request.SoftwareId;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "mojE-Račun configuration updated successfully",
                    Data = new
                    {
                        Enabled = company.MojeRacunEnabled,
                        Environment = company.MojeRacunEnvironment,
                        Username = company.MojeRacunClientId,
                        SoftwareId = company.MojeRacunApiKey,
                        HasPassword = !string.IsNullOrEmpty(company.MojeRacunClientSecret)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring mojE-Račun for company {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while configuring mojE-Račun"
                });
            }
        }

        /// <summary>
        /// Get mojE-Račun configuration status (without exposing password)
        /// </summary>
        [HttpGet("{id}/moje-racun")]
        public async Task<ActionResult<ApiResponse<object>>> GetMojeRacunConfig(int id)
        {
            try
            {
                var company = await _context.CompanyProfiles.FindAsync(id);
                if (company == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Company profile not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "mojE-Račun configuration retrieved",
                    Data = new
                    {
                        Enabled = company.MojeRacunEnabled,
                        Environment = company.MojeRacunEnvironment ?? "test",
                        Username = company.MojeRacunClientId,
                        SoftwareId = company.MojeRacunApiKey,
                        HasPassword = !string.IsNullOrEmpty(company.MojeRacunClientSecret),
                        Configured = !string.IsNullOrEmpty(company.MojeRacunClientId) && 
                                   !string.IsNullOrEmpty(company.MojeRacunClientSecret) &&
                                   !string.IsNullOrEmpty(company.MojeRacunApiKey)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mojE-Račun config for company {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }
    }

    public class FiscalizationSettingsRequest
    {
        public bool FiscalizationEnabled { get; set; }

        [StringLength(11)]
        public string? FiscalizationOib { get; set; }

        [StringLength(11)]
        public string? FiscalizationOperatorOib { get; set; }

        public bool AutoFiscalize { get; set; }

        [StringLength(50)]
        public string? InvoiceParam1 { get; set; }

        [StringLength(50)]
        public string? InvoiceParam2 { get; set; }
    }

    public class MojeRacunConfigRequest
    {
        public bool Enabled { get; set; } = true;

        [StringLength(20)]
        public string Environment { get; set; } = "test"; // "test" or "production"

        [Required]
        [StringLength(200)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// SoftwareId (required by mojE-Račun API)
        /// Example: "Test-001" for demo/testing
        /// </summary>
        [Required]
        [StringLength(100)]
        public string SoftwareId { get; set; } = string.Empty;
    }
}