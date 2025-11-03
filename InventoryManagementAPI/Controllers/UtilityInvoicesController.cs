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
    public class UtilityInvoicesController : ControllerBase
    {
        private readonly UtilityDbContext _context;
        private readonly ILogger<UtilityInvoicesController> _logger;

        public UtilityInvoicesController(UtilityDbContext context, ILogger<UtilityInvoicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UtilityInvoiceListResponse>>>> GetUtilityInvoices(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? period = null,
            [FromQuery] string? building = null,
            [FromQuery] string? fiscalizationStatus = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.UtilityInvoices
                    .Include(u => u.Items)
                    .Include(u => u.ConsumptionData)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u => u.CustomerName.Contains(searchTerm) ||
                                           u.CustomerCode.Contains(searchTerm) ||
                                           u.InvoiceNumber.Contains(searchTerm));
                }

                if (!string.IsNullOrEmpty(period))
                {
                    query = query.Where(u => u.Period == period);
                }

                if (!string.IsNullOrEmpty(building))
                {
                    query = query.Where(u => u.Building.Contains(building));
                }

                if (!string.IsNullOrEmpty(fiscalizationStatus))
                {
                    query = query.Where(u => u.FiscalizationStatus == fiscalizationStatus);
                }

                // Apply pagination
                var totalCount = await query.CountAsync();
                var invoices = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UtilityInvoiceListResponse
                    {
                        Id = u.Id,
                        Building = u.Building,
                        Period = u.Period,
                        InvoiceNumber = u.InvoiceNumber,
                        CustomerCode = u.CustomerCode,
                        CustomerName = u.CustomerName,
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        CreatedAt = u.CreatedAt,
                        ItemCount = u.Items.Count,
                        ConsumptionDataCount = u.ConsumptionData.Count
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<UtilityInvoiceListResponse>>
                {
                    Success = true,
                    Message = $"Retrieved {invoices.Count} of {totalCount} utility invoices",
                    Data = invoices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving utility invoices");
                return StatusCode(500, new ApiResponse<List<UtilityInvoiceListResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving utility invoices"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UtilityInvoiceResponse>>> GetUtilityInvoice(int id)
        {
            try
            {
                var invoice = await _context.UtilityInvoices
                    .Include(u => u.Items.OrderBy(i => i.ItemOrder))
                    .Include(u => u.ConsumptionData.OrderBy(c => c.ParameterOrder))
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<UtilityInvoiceResponse>
                    {
                        Success = false,
                        Message = "Utility invoice not found"
                    });
                }

                var response = new UtilityInvoiceResponse
                {
                    Id = invoice.Id,
                    Building = invoice.Building,
                    Period = invoice.Period,
                    InvoiceNumber = invoice.InvoiceNumber,
                    Model = invoice.Model,
                    IssueDate = invoice.IssueDate,
                    DueDate = invoice.DueDate,
                    ValidityDate = invoice.ValidityDate,
                    BankAccount = invoice.BankAccount,
                    CustomerCode = invoice.CustomerCode,
                    CustomerName = invoice.CustomerName,
                    CustomerAddress = invoice.CustomerAddress,
                    PostalCode = invoice.PostalCode,
                    City = invoice.City,
                    CustomerOib = invoice.CustomerOib,
                    ServiceTypeHot = invoice.ServiceTypeHot,
                    ServiceTypeHeating = invoice.ServiceTypeHeating,
                    SubTotal = invoice.SubTotal,
                    VatAmount = invoice.VatAmount,
                    TotalAmount = invoice.TotalAmount,
                    DebtText = invoice.DebtText,
                    ConsumptionText = invoice.ConsumptionText,
                    FiscalizationStatus = invoice.FiscalizationStatus,
                    Jir = invoice.Jir,
                    Zki = invoice.Zki,
                    FiscalizedAt = invoice.FiscalizedAt,
                    FiscalizationError = invoice.FiscalizationError,
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt,
                    Items = invoice.Items.Select(i => new UtilityInvoiceItemResponse
                    {
                        Id = i.Id,
                        Description = i.Description,
                        Unit = i.Unit,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Amount = i.Amount,
                        ItemOrder = i.ItemOrder
                    }).ToList(),
                    ConsumptionData = invoice.ConsumptionData.Select(c => new UtilityConsumptionDataResponse
                    {
                        Id = c.Id,
                        ParameterName = c.ParameterName,
                        ParameterValue = c.ParameterValue,
                        ParameterOrder = c.ParameterOrder
                    }).ToList()
                };

                return Ok(new ApiResponse<UtilityInvoiceResponse>
                {
                    Success = true,
                    Message = "Utility invoice retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving utility invoice {Id}", id);
                return StatusCode(500, new ApiResponse<UtilityInvoiceResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the utility invoice"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUtilityInvoice(int id)
        {
            try
            {
                var invoice = await _context.UtilityInvoices.FindAsync(id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Utility invoice not found"
                    });
                }

                // Prevent deletion of fiscalized invoices
                if (invoice.FiscalizationStatus == "fiscalized")
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Cannot delete fiscalized invoices"
                    });
                }

                _context.UtilityInvoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Utility invoice deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting utility invoice {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the utility invoice"
                });
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<object>>> GetUtilityInvoicesSummary()
        {
            try
            {
                var summary = await _context.UtilityInvoices
                    .GroupBy(u => 1)
                    .Select(g => new
                    {
                        TotalInvoices = g.Count(),
                        TotalAmount = g.Sum(u => u.TotalAmount),
                        FiscalizedCount = g.Count(u => u.FiscalizationStatus == "fiscalized"),
                        PendingCount = g.Count(u => u.FiscalizationStatus == "not_required"),
                        ErrorCount = g.Count(u => u.FiscalizationStatus == "error"),
                        Periods = g.Select(u => u.Period).Distinct().ToList(),
                        Buildings = g.Select(u => u.Building).Distinct().ToList()
                    })
                    .FirstOrDefaultAsync();

                if (summary == null)
                {
                    summary = new
                    {
                        TotalInvoices = 0,
                        TotalAmount = 0m,
                        FiscalizedCount = 0,
                        PendingCount = 0,
                        ErrorCount = 0,
                        Periods = new List<string>(),
                        Buildings = new List<string>()
                    };
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Summary retrieved successfully",
                    Data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving utility invoices summary");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the summary"
                });
            }
        }
    }
}