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
        private readonly ILogger<UtilityFiscalizationController> _logger;

        public UtilityFiscalizationController(
            UtilityDbContext context,
            IFiscalizationService fiscalizationService,
            ILogger<UtilityFiscalizationController> logger)
        {
            _context = context;
            _fiscalizationService = fiscalizationService;
            _logger = logger;
        }

        [HttpPost("{id}/fiscalize")]
        public async Task<ActionResult<ApiResponse<object>>> FiscalizeUtilityInvoice(int id, [FromBody] FiscalizeUtilityWithCompanyRequest? request = null)
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
                        Message = "Utility invoice not found"
                    });
                }

                if (utilityInvoice.FiscalizationStatus == "fiscalized")
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invoice is already fiscalized"
                    });
                }

                // Get company profile for fiscalization
                CompanyProfile? company;
                if (request?.CompanyId.HasValue == true)
                {
                    company = await _context.CompanyProfiles.FindAsync(request.CompanyId.Value);
                }
                else
                {
                    // Use first available company with fiscalization enabled
                    company = await _context.CompanyProfiles
                        .Where(c => c.FiscalizationEnabled)
                        .FirstOrDefaultAsync();
                }

                if (company == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No company profile found for fiscalization. Please create a company profile with fiscalization enabled first."
                    });
                }

                if (!company.FiscalizationEnabled)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Fiscalization is not enabled for the selected company profile."
                    });
                }

                if (string.IsNullOrEmpty(company.FiscalizationCertificatePath))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No fiscalization certificate found for the selected company. Please upload a certificate first."
                    });
                }

                // Convert UtilityInvoice to Invoice format for fiscalization service
                var invoice = ConvertUtilityInvoiceToInvoice(utilityInvoice);

                // Fiscalize the invoice
                var fiscalizationResult = await _fiscalizationService.FiscalizeInvoiceAsync(invoice, company);

                // Update utility invoice with fiscalization results
                if (fiscalizationResult.Success)
                {
                    utilityInvoice.FiscalizationStatus = "fiscalized";
                    utilityInvoice.Jir = fiscalizationResult.Jir;
                    utilityInvoice.Zki = fiscalizationResult.Zki;
                    utilityInvoice.FiscalizedAt = DateTime.UtcNow;
                    utilityInvoice.FiscalizationError = null;
                    utilityInvoice.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    utilityInvoice.FiscalizationStatus = "error";
                    utilityInvoice.FiscalizationError = fiscalizationResult.Message;
                    utilityInvoice.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = fiscalizationResult.Success,
                    Message = fiscalizationResult.Success
                        ? "Utility invoice fiscalized successfully"
                        : $"Fiscalization failed: {fiscalizationResult.Message}",
                    Data = new
                    {
                        InvoiceId = id,
                        CompanyUsed = company.CompanyName,
                        FiscalizationStatus = utilityInvoice.FiscalizationStatus,
                        Jir = utilityInvoice.Jir,
                        Zki = utilityInvoice.Zki,
                        FiscalizedAt = utilityInvoice.FiscalizedAt,
                        Error = utilityInvoice.FiscalizationError
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fiscalizing utility invoice {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while fiscalizing the invoice"
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

                var company = await _context.CompanyProfiles.FirstOrDefaultAsync();
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

                foreach (var utilityInvoice in utilityInvoices)
                {
                    try
                    {
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
                            Success = fiscalizationResult.Success,
                            Message = fiscalizationResult.Message,
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
                            Success = false,
                            Message = ex.Message,
                            Jir = (string?)null
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Batch fiscalization completed. {successCount} successful, {errorCount} errors",
                    Data = new
                    {
                        TotalProcessed = utilityInvoices.Count,
                        SuccessCount = successCount,
                        ErrorCount = errorCount,
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

        private Invoice ConvertUtilityInvoiceToInvoice(UtilityInvoice utilityInvoice)
        {
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
                CustomerId = 0, // Not needed for fiscalization
                CustomerName = utilityInvoice.CustomerName,
                CustomerAddress = utilityInvoice.CustomerAddress,
                CustomerOib = utilityInvoice.CustomerOib,
                CompanyName = "Utility Company", // Will be overridden by company profile
                CompanyAddress = "",
                CompanyOib = "",
                SubTotal = utilityInvoice.SubTotal,
                TaxAmount = utilityInvoice.VatAmount,
                TotalAmount = utilityInvoice.TotalAmount,
                TaxRate = utilityInvoice.VatAmount > 0 ? (utilityInvoice.VatAmount / utilityInvoice.SubTotal * 100) : 0,
                PaidAmount = utilityInvoice.TotalAmount,
                RemainingAmount = 0,
                PaymentMethod = "Bank Transfer",
                PaymentMethodCode = "G",
                Notes = utilityInvoice.DebtText,
                CreatedAt = utilityInvoice.CreatedAt,
                FiscalizationStatus = utilityInvoice.FiscalizationStatus,
                Items = utilityInvoice.Items.Select(item => new InvoiceItem
                {
                    ProductName = item.Description,
                    ProductSku = $"UTIL-{item.ItemOrder}",
                    ProductKpdCode = "3", // Services
                    ProductDescription = item.Description,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TaxRate = utilityInvoice.VatAmount > 0 ? (utilityInvoice.VatAmount / utilityInvoice.SubTotal * 100) : 0,
                    LineTotal = item.Amount,
                    LineTaxAmount = item.Amount * (utilityInvoice.VatAmount > 0 ? (utilityInvoice.VatAmount / utilityInvoice.SubTotal) : 0),
                    Unit = item.Unit
                }).ToList()
            };
        }
    }
}