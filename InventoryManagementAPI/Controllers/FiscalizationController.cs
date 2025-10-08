using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using InventoryManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;



namespace InventoryManagementAPI.Controllers
{
        // Controllers/FiscalizationController.cs

        [ApiController]
        [Route("api/[controller]")]
        [Authorize]
        public class FiscalizationController : ControllerBase
        {
            private readonly AppDbContext _context;
            private readonly IFiscalizationService _fiscalizationService;
            private readonly ILogger<FiscalizationController> _logger;
            private readonly IWebHostEnvironment _environment;

            public FiscalizationController(
                AppDbContext context,
                IFiscalizationService fiscalizationService,
                ILogger<FiscalizationController> logger,
                IWebHostEnvironment environment)
            {
                _context = context;
                _fiscalizationService = fiscalizationService;
                _logger = logger;
                _environment = environment;
            }

            /// <summary>
            /// Upload certifikata za fiskalizaciju
            /// </summary>
            [HttpPost("certificate")]
            public async Task<IActionResult> UploadCertificate([FromForm] UploadCertificateRequest request)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var company = await _context.CompanyProfiles
                    .FirstOrDefaultAsync(c => c.UserId.ToString() == userId);
                if (company == null)
                    return NotFound("Company profile not found");

                if (request.Certificate == null || request.Certificate.Length == 0)
                    return BadRequest("Certificate file is required");

                if (!request.Certificate.FileName.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Only .pfx files are allowed");

                try
                {
                    // Kreiraj folder za certifikate ako ne postoji
                    var certFolder = Path.Combine(_environment.WebRootPath, "certificates", userId);
                    Directory.CreateDirectory(certFolder);

                    // Spremi certifikat
                    var fileName = $"fiscal_cert_{DateTime.Now:yyyyMMddHHmmss}.pfx";
                    var filePath = Path.Combine(certFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.Certificate.CopyToAsync(stream);
                    }

                    // Testiraj da li se certifikat može učitati
                    try
                    {
                        var testCert = new X509Certificate2(filePath, request.Password);
                        if (!testCert.HasPrivateKey)
                        {
                            System.IO.File.Delete(filePath);
                            return BadRequest("Certificate does not contain a private key");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.Delete(filePath);
                        return BadRequest($"Invalid certificate or password: {ex.Message}");
                    }

                    // Obriši stari certifikat ako postoji
                    if (!string.IsNullOrEmpty(company.FiscalizationCertificatePath))
                    {
                        var oldPath = Path.Combine(_environment.WebRootPath, company.FiscalizationCertificatePath);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    // Spremi u bazu (relativni path)
                    company.FiscalizationCertificatePath = Path.Combine("certificates", userId, fileName);
                    company.FiscalizationCertificatePassword = request.Password; // TODO: Enkriptirati!

                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Certificate uploaded successfully" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading certificate");
                    return StatusCode(500, "Error uploading certificate");
                }
            }

            /// <summary>
            /// Update fiskalizacijskih postavki
            /// </summary>
            [HttpPut("settings")]
            public async Task<IActionResult> UpdateSettings([FromBody] UpdateFiscalizationSettingsRequest request)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var company = await _context.CompanyProfiles
                    .FirstOrDefaultAsync(c => c.UserId.ToString() == userId);

                if (company == null)
                    return NotFound("Company profile not found");

                company.FiscalizationEnabled = request.FiscalizationEnabled;
                company.FiscalizationOib = request.FiscalizationOib;
                company.FiscalizationOperatorOib = request.FiscalizationOperatorOib;
                company.AutoFiscalize = request.AutoFiscalize;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Settings updated successfully" });
            }

            /// <summary>
            /// Get fiskalizacijske postavke
            /// </summary>
            [HttpGet("settings")]
            public async Task<IActionResult> GetSettings()
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var company = await _context.CompanyProfiles
                    .FirstOrDefaultAsync(c => c.UserId.ToString() == userId);

                if (company == null)
                    return NotFound("Company profile not found");

                return Ok(new
                {
                    fiscalizationEnabled = company.FiscalizationEnabled,
                    hasCertificate = !string.IsNullOrEmpty(company.FiscalizationCertificatePath),
                    fiscalizationOib = company.FiscalizationOib,
                    fiscalizationOperatorOib = company.FiscalizationOperatorOib,
                    autoFiscalize = company.AutoFiscalize
                });
            }

            /// <summary>
            /// Test FINA veze
            /// </summary>
            [HttpPost("test")]
            public async Task<IActionResult> TestConnection()
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var company = await _context.CompanyProfiles
                    .FirstOrDefaultAsync(c => c.UserId.ToString() == userId);

                if (company == null)
                    return NotFound("Company profile not found");

                if (!company.FiscalizationEnabled)
                    return BadRequest("Fiscalization is not enabled");

                if (string.IsNullOrEmpty(company.FiscalizationCertificatePath))
                    return BadRequest("Certificate not uploaded");

                try
                {
                    // Kreiraj test račun
                    var testInvoice = new Invoice
                    {
                        InvoiceNumber = "TEST/1/1",
                        IssueDate = DateTime.Now,
                        TotalAmount = 100.00m,
                        SubTotal = 80.00m,
                        TaxAmount = 20.00m,
                        TaxRate = 25.00m,
                        PaymentMethodCode = "G",
                        Items = new List<InvoiceItem>
                {
                    new InvoiceItem
                    {
                        ProductName = "Test proizvod",
                        Quantity = 1,
                        UnitPrice = 80.00m,
                        TaxRate = 25.00m,
                        LineTotal = 100.00m,
                        LineTaxAmount = 20.00m
                    }
                }
                    };

                    var result = await _fiscalizationService.FiscalizeInvoiceAsync(testInvoice, company);

                    if (result.Success)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Connection successful! JIR received.",
                            jir = result.Jir,
                            zki = result.Zki
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = result.Message,
                            error = result.RawResponse
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error testing FINA connection");
                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }
        }
}
