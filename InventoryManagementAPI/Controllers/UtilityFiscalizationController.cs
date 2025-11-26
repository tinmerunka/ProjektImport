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
    [Route("api/[controller]")]
    [Authorize]
    public class UtilityFiscalizationController : ControllerBase
    {
        private readonly UtilityDbContext _context;
        private readonly IFiscalizationService _fiscalizationService;
        private readonly IMojeRacunService _mojeRacunService;
        private readonly ILogger<UtilityFiscalizationController> _logger;

        // ✅ Fiscalization date limit (30 days for FINA/CIS)
        private static readonly TimeSpan MaxInvoiceAgeForFina = TimeSpan.FromDays(30);

        public UtilityFiscalizationController(
            UtilityDbContext context,
            IFiscalizationService fiscalizationService,
            IMojeRacunService mojeRacunService,
            ILogger<UtilityFiscalizationController> logger)
        {
            _context = context;
            _fiscalizationService = fiscalizationService;
            _mojeRacunService = mojeRacunService;
            _logger = logger;
        }

        /// <summary>
        /// Fiscalize invoice using FINA 1.0 (CIS) method
        /// </summary>
        [HttpPost("{id}/fiscalize-fina")]
        public async Task<ActionResult<ApiResponse<object>>> FiscalizeWithFina(
            int id, 
            [FromBody] FiscalizeUtilityWithCompanyRequest? request = null)
        {
            try
            {
                var utilityInvoice = await _context.UtilityInvoices
                    .Include(u => u.Items)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (utilityInvoice == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Račun nije pronađen"
                    });
                }

                if (utilityInvoice.FiscalizationStatus == "fiscalized")
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Račun je već fiskaliziran"
                    });
                }

                // ✅ CHECK: Invoice age limit for FINA
                var invoiceAge = DateTime.Now - utilityInvoice.IssueDate;
                if (invoiceAge > MaxInvoiceAgeForFina)
                {
                    var errorMessage = $"Račun od {utilityInvoice.IssueDate:dd.MM.yyyy} je prestar za FINA fiskalizaciju. " +
                        $"Račun je star {invoiceAge.Days} dana. Maksimalna dopuštena starost je {MaxInvoiceAgeForFina.Days} dana.";

                    _logger.LogWarning("Cannot fiscalize invoice {InvoiceNumber} with FINA: {Message}",
                        utilityInvoice.InvoiceNumber, errorMessage);

                    utilityInvoice.FiscalizationStatus = "too_old";
                    utilityInvoice.FiscalizationError = errorMessage;
                    utilityInvoice.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = errorMessage
                    });
                }

                // Get company profile
                var company = await GetCompanyForFiscalization(request?.CompanyId, fiscalizationType: "fina");
                if (company == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nije pronađen profil tvrtke s omogućenom FINA fiskalizacijom"
                    });
                }

                // Convert and fiscalize
                var invoice = ConvertUtilityInvoiceToInvoice(utilityInvoice);
                var result = await _fiscalizationService.FiscalizeInvoiceAsync(invoice, company);

                // Update invoice
                if (result.Success)
                {
                    utilityInvoice.FiscalizationStatus = "fiscalized";
                    utilityInvoice.FiscalizationMethod = "fina";
                    utilityInvoice.Jir = result.Jir;
                    utilityInvoice.Zki = result.Zki;
                    utilityInvoice.FiscalizedAt = DateTime.UtcNow;
                    utilityInvoice.FiscalizationError = null;
                }
                else
                {
                    utilityInvoice.FiscalizationStatus = "error";
                    utilityInvoice.FiscalizationError = result.Message;
                }

                utilityInvoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = result.Success,
                    Message = result.Success 
                        ? "Račun uspješno fiskaliziran preko FINA sustava" 
                        : $"Fiskalizacija neuspješna: {result.Message}",
                    Data = new
                    {
                        InvoiceId = id,
                        Method = "fina",
                        FiscalizationStatus = utilityInvoice.FiscalizationStatus,
                        Jir = utilityInvoice.Jir,
                        Zki = utilityInvoice.Zki,
                        FiscalizedAt = utilityInvoice.FiscalizedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fiscalizing with FINA for invoice {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Došlo je do greške prilikom fiskalizacije"
                });
            }
        }

        /// <summary>
        /// Fiscalize invoice using mojE-Račun 2.0 (UBL) method
        /// </summary>
        [HttpPost("{id}/fiscalize-moje-racun")]
        public async Task<ActionResult<ApiResponse<object>>> FiscalizeWithMojeRacun(
            int id,
            [FromBody] FiscalizeUtilityWithCompanyRequest? request = null)
        {
            try
            {
                var utilityInvoice = await _context.UtilityInvoices
                    .Include(u => u.Items)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (utilityInvoice == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Račun nije pronađen"
                    });
                }

                if (utilityInvoice.FiscalizationStatus == "fiscalized")
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Račun je već fiskaliziran"
                    });
                }

                // Get company profile
                var company = await GetCompanyForFiscalization(request?.CompanyId, fiscalizationType: "moje-racun");
                if (company == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nije pronađen profil tvrtke s omogućenim mojE-Račun sustavom"
                    });
                }

                // Fiscalize via mojE-Račun
                var result = await _mojeRacunService.SubmitInvoiceAsync(utilityInvoice, company);

                // Update invoice
                if (result.Success)
                {
                    utilityInvoice.FiscalizationStatus = "fiscalized";
                    utilityInvoice.FiscalizationMethod = "moje-racun";
                    utilityInvoice.MojeRacunInvoiceId = result.InvoiceId;
                    utilityInvoice.MojeRacunStatus = result.Status;
                    utilityInvoice.MojeRacunSubmittedAt = result.SubmittedAt;
                    utilityInvoice.FiscalizedAt = DateTime.UtcNow;
                    utilityInvoice.FiscalizationError = null;
                }
                else
                {
                    utilityInvoice.FiscalizationStatus = "error";
                    utilityInvoice.FiscalizationError = result.Message;
                }

                utilityInvoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = result.Success,
                    Message = result.Success
                        ? "Račun uspješno poslan u mojE-Račun sustav"
                        : $"Slanje neuspješno: {result.Message}",
                    Data = new
                    {
                        InvoiceId = id,
                        Method = "moje-racun",
                        FiscalizationStatus = utilityInvoice.FiscalizationStatus,
                        MojeRacunInvoiceId = utilityInvoice.MojeRacunInvoiceId,
                        Status = utilityInvoice.MojeRacunStatus,
                        SubmittedAt = utilityInvoice.MojeRacunSubmittedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fiscalizing with mojE-Račun for invoice {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Došlo je do greške prilikom slanja u mojE-Račun"
                });
            }
        }

        /// <summary>
        /// Get available fiscalization methods for a company
        /// </summary>
        [HttpGet("methods")]
        public async Task<ActionResult<ApiResponse<object>>> GetAvailableMethods([FromQuery] int? companyId = null)
        {
            try
            {
                CompanyProfile? company;
                if (companyId.HasValue)
                {
                    company = await _context.CompanyProfiles.FindAsync(companyId.Value);
                }
                else
                {
                    company = await _context.CompanyProfiles.FirstOrDefaultAsync();
                }

                if (company == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Company profile not found"
                    });
                }

                var methods = new List<object>();

                if (company.FiscalizationEnabled && !string.IsNullOrEmpty(company.FiscalizationCertificatePath))
                {
                    methods.Add(new
                    {
                        Id = "fina",
                        Name = "Fiskalizacija 1.0 (FINA/CIS)",
                        Description = "Klasični sustav fiskalizacije",
                        Available = true,
                        Configured = true,
                        MaxAgeDays = MaxInvoiceAgeForFina.Days,
                        Features = new[] { "JIR kod", "ZKI kod", "Trenutna fiskalizacija" }
                    });
                }

                var mojeRacunConfigured = !string.IsNullOrEmpty(company.MojeRacunClientId) && 
                                         !string.IsNullOrEmpty(company.MojeRacunClientSecret);

                if (company.MojeRacunEnabled)
                {
                    methods.Add(new
                    {
                        Id = "moje-racun",
                        Name = "mojE-Račun 2.0",
                        Description = "Novi sustav e-računa Porezne uprave",
                        Available = true,
                        Configured = mojeRacunConfigured,
                        Environment = company.MojeRacunEnvironment ?? "test",
                        AuthMethod = "username/password",
                        MaxAgeDays = (int?)null,
                        Features = new[] { "UBL format", "Bez vremenskog ograničenja", "Automatska dostava", "QR kod", "PDF generiranje" }
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Found {methods.Count} available fiscalization method(s)",
                    Data = new
                    {
                        CompanyId = company.Id,
                        CompanyName = company.CompanyName,
                        AvailableMethods = methods
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available methods");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Test mojE-Račun connection with configured credentials
        /// </summary>
        [HttpPost("test-moje-racun")]
        public async Task<ActionResult<ApiResponse<object>>> TestMojeRacunConnection(
            [FromBody] FiscalizeUtilityWithCompanyRequest? request = null)
        {
            try
            {
                var company = await GetCompanyForFiscalization(request?.CompanyId, fiscalizationType: "moje-racun");
                
                if (company == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nije pronađen profil tvrtke s omogućenim mojE-Račun sustavom"
                    });
                }

                if (string.IsNullOrEmpty(company.MojeRacunClientId) || string.IsNullOrEmpty(company.MojeRacunClientSecret))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "mojE-Račun credentials (username/password) are not configured"
                    });
                }

                // Test connection
                var result = await _mojeRacunService.TestConnectionAsync(company);

                return Ok(new ApiResponse<object>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = new
                    {
                        CompanyId = company.Id,
                        CompanyName = company.CompanyName,
                        Environment = company.MojeRacunEnvironment,
                        Username = company.MojeRacunClientId,
                        ConnectionStatus = result.Status,
                        TestedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing mojE-Račun connection");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Connection test failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Query mojE-Račun outbox to check invoice statuses
        /// Returns list of sent invoices with their current status
        /// </summary>
        [HttpPost("query-outbox")]
        public async Task<ActionResult<ApiResponse<object>>> QueryMojeRacunOutbox(
            [FromBody] OutboxQueryRequest? request = null)
        {
            try
            {
                var company = await GetCompanyForFiscalization(request?.CompanyId, fiscalizationType: "moje-racun");
                
                if (company == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nije pronađen profil tvrtke s omogućenim mojE-Račun sustavom"
                    });
                }

                // Build filter
                var filter = new OutboxQueryFilter();
                
                if (request?.ElectronicId.HasValue == true)
                    filter.ElectronicId = request.ElectronicId.Value;
                    
                if (request?.StatusId.HasValue == true)
                    filter.StatusId = request.StatusId.Value;
                    
                if (request?.InvoiceYear.HasValue == true)
                    filter.InvoiceYear = request.InvoiceYear.Value;
                    
                if (!string.IsNullOrEmpty(request?.InvoiceNumber))
                    filter.InvoiceNumber = request.InvoiceNumber;
                    
                if (request?.From.HasValue == true)
                    filter.From = request.From.Value;
                    
                if (request?.To.HasValue == true)
                    filter.To = request.To.Value;

                // Query outbox
                var headers = await _mojeRacunService.QueryOutboxAsync(company, filter);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Retrieved {headers.Count} invoice(s) from outbox",
                    Data = new
                    {
                        CompanyId = company.Id,
                        CompanyName = company.CompanyName,
                        Environment = company.MojeRacunEnvironment,
                        TotalResults = headers.Count,
                        Invoices = headers.Select(h => new
                        {
                            h.ElectronicId,
                            h.DocumentNr,
                            h.StatusId,
                            h.StatusName,
                            h.RecipientBusinessName,
                            h.RecipientBusinessNumber,
                            h.Created,
                            h.Updated,
                            h.Sent,
                            h.Delivered
                        }).ToList(),
                        StatusLegend = new
                        {
                            _10 = "In preparation (U pripremi) - Document uploaded, pending validation",
                            _20 = "In validation (U validaciji) - Validating recipient company data",
                            _30 = "Sent (Poslan) - Signed, timestamped, email sent to customer",
                            _40 = "Delivered (Dostavljen) - Customer accepted and downloaded",
                            _45 = "Canceled (Otkazan) - Sender canceled, customer can't download",
                            _50 = "Unsuccessful (Neuspješan) - Customer didn't download in 5 days"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying mojE-Račun outbox");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Query failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Update invoice statuses by querying mojE-Račun outbox
        /// Automatically updates local database with latest statuses from mojE-Račun
        /// </summary>
        [HttpPost("sync-statuses")]
        public async Task<ActionResult<ApiResponse<object>>> SyncInvoiceStatuses(
            [FromBody] FiscalizeUtilityWithCompanyRequest? request = null)
        {
            try
            {
                var company = await GetCompanyForFiscalization(request?.CompanyId, fiscalizationType: "moje-racun");
                
                if (company == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nije pronađen profil tvrtke s omogućenim mojE-Račun sustavom"
                    });
                }

                // Get all fiscalized invoices via mojE-RaČun
                var localInvoices = await _context.UtilityInvoices
                    .Where(u => u.FiscalizationMethod == "moje-racun" && 
                                u.FiscalizationStatus == "fiscalized" &&
                                !string.IsNullOrEmpty(u.MojeRacunInvoiceId))
                    .ToListAsync();

                if (!localInvoices.Any())
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "No mojE-Račun invoices found to sync",
                        Data = new { UpdatedCount = 0 }
                    });
                }

                // Query outbox for recent invoices (last 30 days)
                var filter = new OutboxQueryFilter
                {
                    From = DateTime.Now.AddDays(-30),
                    To = DateTime.Now
                };

                var outboxHeaders = await _mojeRacunService.QueryOutboxAsync(company, filter);

                int updatedCount = 0;
                var updates = new List<object>();

                foreach (var localInvoice in localInvoices)
                {
                    // Find matching invoice in outbox
                    var outboxHeader = outboxHeaders.FirstOrDefault(h => 
                        h.ElectronicId == localInvoice.MojeRacunInvoiceId);

                    if (outboxHeader != null)
                    {
                        var oldStatus = localInvoice.MojeRacunStatus;
                        var newStatus = outboxHeader.StatusName;

                        // Update if status changed
                        if (oldStatus != newStatus)
                        {
                            localInvoice.MojeRacunStatus = newStatus;
                            localInvoice.UpdatedAt = DateTime.UtcNow;
                            updatedCount++;

                            updates.Add(new
                            {
                                InvoiceId = localInvoice.Id,
                                InvoiceNumber = localInvoice.InvoiceNumber,
                                ElectronicId = localInvoice.MojeRacunInvoiceId,
                                OldStatus = oldStatus,
                                NewStatus = newStatus,
                                Delivered = outboxHeader.Delivered
                            });

                            _logger.LogInformation("Updated invoice {InvoiceNumber}: {OldStatus} → {NewStatus}",
                                localInvoice.InvoiceNumber, oldStatus, newStatus);
                        }
                    }
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Sinkronizirano {localInvoices.Count} računa, {updatedCount} statusa ažurirano",
                    Data = new
                    {
                        TotalInvoices = localInvoices.Count,
                        UpdatedCount = updatedCount,
                        Updates = updates
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing invoice statuses");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Sync failed: {ex.Message}"
                });
            }
        }

        [HttpPost("fiscalize-batch")]
        public async Task<ActionResult<ApiResponse<object>>> FiscalizeBatchUtilityInvoices([FromBody] List<int> invoiceIds)
        {
            try
            {
                if (!invoiceIds.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No invoice IDs provided"
                    });
                }

                var company = await _context.CompanyProfiles
                    .Where(c => c.FiscalizationEnabled)
                    .FirstOrDefaultAsync();

                if (company == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No company profile found for fiscalization"
                    });
                }

                var utilityInvoices = await _context.UtilityInvoices
                    .Include(u => u.Items)
                    .Where(u => invoiceIds.Contains(u.Id) && u.FiscalizationStatus != "fiscalized")
                    .ToListAsync();

                var results = new List<object>();
                int successCount = 0;
                int errorCount = 0;
                int tooOldCount = 0;

                foreach (var utilityInvoice in utilityInvoices)
                {
                    try
                    {
                        // ✅ CHECK: Invoice age for each invoice in batch
                        var invoiceAge = DateTime.Now - utilityInvoice.IssueDate;
                        if (invoiceAge > MaxInvoiceAgeForFina)
                        {
                            var errorMessage = $"Invoice is {invoiceAge.Days} days old (max: {MaxInvoiceAgeForFina.Days} days)";

                            utilityInvoice.FiscalizationStatus = "too_old";
                            utilityInvoice.FiscalizationError = errorMessage;
                            utilityInvoice.UpdatedAt = DateTime.UtcNow;
                            tooOldCount++;

                            results.Add(new
                            {
                                InvoiceId = utilityInvoice.Id,
                                InvoiceNumber = utilityInvoice.InvoiceNumber,
                                InvoiceDate = utilityInvoice.IssueDate.ToString("yyyy-MM-dd"),
                                Success = false,
                                Message = errorMessage,
                                Status = "too_old",
                                Jir = (string?)null
                            });

                            continue; // Skip to next invoice
                        }

                        var invoice = ConvertUtilityInvoiceToInvoice(utilityInvoice);
                        var fiscalizationResult = await _fiscalizationService.FiscalizeInvoiceAsync(invoice, company);

                        if (fiscalizationResult.Success)
                        {
                            utilityInvoice.FiscalizationStatus = "fiscalized";
                            utilityInvoice.Jir = fiscalizationResult.Jir;
                            utilityInvoice.Zki = fiscalizationResult.Zki;
                            utilityInvoice.FiscalizedAt = DateTime.UtcNow;
                            utilityInvoice.FiscalizationError = null;
                            successCount++;
                        }
                        else
                        {
                            utilityInvoice.FiscalizationStatus = "error";
                            utilityInvoice.FiscalizationError = fiscalizationResult.Message;
                            errorCount++;
                        }

                        utilityInvoice.UpdatedAt = DateTime.UtcNow;

                        results.Add(new
                        {
                            InvoiceId = utilityInvoice.Id,
                            InvoiceNumber = utilityInvoice.InvoiceNumber,
                            InvoiceDate = utilityInvoice.IssueDate.ToString("yyyy-MM-dd"),
                            Success = fiscalizationResult.Success,
                            Message = fiscalizationResult.Message,
                            Status = utilityInvoice.FiscalizationStatus,
                            Jir = utilityInvoice.Jir
                        });

                        // Add small delay between requests to avoid overwhelming FINA
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fiscalizing utility invoice {Id}", utilityInvoice.Id);

                        utilityInvoice.FiscalizationStatus = "error";
                        utilityInvoice.FiscalizationError = ex.Message;
                        utilityInvoice.UpdatedAt = DateTime.UtcNow;
                        errorCount++;

                        results.Add(new
                        {
                            InvoiceId = utilityInvoice.Id,
                            InvoiceNumber = utilityInvoice.InvoiceNumber,
                            InvoiceDate = utilityInvoice.IssueDate.ToString("yyyy-MM-dd"),
                            Success = false,
                            Message = ex.Message,
                            Status = "error",
                            Jir = (string?)null
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Batch fiscalization completed. {successCount} successful, {errorCount} errors, {tooOldCount} too old",
                    Data = new
                    {
                        TotalProcessed = utilityInvoices.Count,
                        SuccessCount = successCount,
                        ErrorCount = errorCount,
                        TooOldCount = tooOldCount,
                        MaxAgeDays = MaxInvoiceAgeForFina.Days,
                        Results = results
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch fiscalization");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during batch fiscalization"
                });
            }
        }

        [HttpPost("fiscalize-all")]
        public async Task<ActionResult<ApiResponse<object>>> FiscalizeAllPendingUtilityInvoices()
        {
            try
            {
                var pendingInvoiceIds = await _context.UtilityInvoices
                    .Where(u => u.FiscalizationStatus == "not_required")
                    .Select(u => u.Id)
                    .ToListAsync();

                if (!pendingInvoiceIds.Any())
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "No pending invoices to fiscalize",
                        Data = new { TotalProcessed = 0 }
                    });
                }

                // Call the batch fiscalization method
                return await FiscalizeBatchUtilityInvoices(pendingInvoiceIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fiscalizing all pending utility invoices");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while fiscalizing all pending invoices"
                });
            }
        }

        private async Task<CompanyProfile?> GetCompanyForFiscalization(int? companyId, string fiscalizationType)
        {
            CompanyProfile? company;

            if (companyId.HasValue)
            {
                company = await _context.CompanyProfiles.FindAsync(companyId.Value);
            }
            else
            {
                if (fiscalizationType == "fina")
                {
                    company = await _context.CompanyProfiles
                        .Where(c => c.FiscalizationEnabled && !string.IsNullOrEmpty(c.FiscalizationCertificatePath))
                        .FirstOrDefaultAsync();
                }
                else // moje-racun
                {
                    company = await _context.CompanyProfiles
                        .Where(c => c.MojeRacunEnabled)
                        .FirstOrDefaultAsync();
                }
            }

            return company;
        }

        private Invoice ConvertUtilityInvoiceToInvoice(UtilityInvoice utilityInvoice)
        {
            _logger.LogInformation("Converting invoice {InvoiceNumber} for FINA fiscalization", 
                utilityInvoice.InvoiceNumber);

            string? customerOib = null;
            if (!string.IsNullOrEmpty(utilityInvoice.CustomerOib) &&
                utilityInvoice.CustomerOib != "0" &&
                utilityInvoice.CustomerOib.Length == 11)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(utilityInvoice.CustomerOib, @"^\d{11}$"))
                {
                    customerOib = utilityInvoice.CustomerOib;
                }
            }

            decimal totalAmount = utilityInvoice.TotalAmount;
            decimal vatAmount = utilityInvoice.VatAmount;
            decimal subTotal = utilityInvoice.SubTotal;

            var calculatedTotal = subTotal + vatAmount;
            if (Math.Abs(calculatedTotal - totalAmount) > 0.01m)
            {
                _logger.LogWarning("Adjusting financial values for invoice {InvoiceNumber}", 
                    utilityInvoice.InvoiceNumber);
                
                if (vatAmount > 0)
                {
                    subTotal = Math.Round(totalAmount / 1.05m, 2);
                    vatAmount = totalAmount - subTotal;
                }
                else
                {
                    subTotal = totalAmount;
                    vatAmount = 0;
                }
            }

            decimal taxRate = 0.00m;
            if (vatAmount > 0 && subTotal > 0)
            {
                var calculatedRate = (vatAmount / subTotal) * 100;
                if (calculatedRate <= 2.5m) taxRate = 0.00m;
                else if (calculatedRate <= 7.5m) taxRate = 5.00m;
                else if (calculatedRate <= 19m) taxRate = 13.00m;
                else taxRate = 25.00m;
            }

            return new Invoice
            {
                Id = utilityInvoice.Id,
                InvoiceNumber = utilityInvoice.InvoiceNumber,
                Type = InvoiceType.Invoice,
                Status = InvoiceStatus.Paid,
                Currency = "EUR",
                IssueLocation = utilityInvoice.Building,
                IssueDate = utilityInvoice.IssueDate,
                DueDate = utilityInvoice.DueDate,
                DeliveryDate = utilityInvoice.IssueDate,
                CustomerId = 0,
                CustomerName = utilityInvoice.CustomerName,
                CustomerAddress = utilityInvoice.CustomerAddress,
                CustomerOib = customerOib,
                CompanyName = "Utility Company",
                CompanyAddress = "",
                CompanyOib = "",
                SubTotal = subTotal,
                TaxAmount = vatAmount,
                TotalAmount = totalAmount,
                TaxRate = taxRate,
                PaidAmount = totalAmount,
                RemainingAmount = 0,
                PaymentMethod = "Transakcijski račun",
                PaymentMethodCode = "T",
                Notes = utilityInvoice.DebtText,
                CreatedAt = utilityInvoice.CreatedAt,
                FiscalizationStatus = utilityInvoice.FiscalizationStatus,
                Items = new List<InvoiceItem>()
            };
        }
    }
}