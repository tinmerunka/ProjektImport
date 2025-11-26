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

        /// Base endpoint - Get ALL utility invoices (for import history and fallback)
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetUtilityInvoices(
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

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
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
                        CustomerOib = u.CustomerOib,
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        FiscalizationMethod = u.FiscalizationMethod,
                        Jir = u.Jir,
                        Zki = u.Zki,
                        MojeRacunInvoiceId = u.MojeRacunInvoiceId,
                        MojeRacunStatus = u.MojeRacunStatus,
                        FiscalizedAt = u.FiscalizedAt,
                        CreatedAt = u.CreatedAt,
                        ItemCount = u.Items.Count,
                        ConsumptionDataCount = u.ConsumptionData.Count
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Retrieved {invoices.Count} of {totalCount} utility invoices",
                    Data = new
                    {
                        Items = invoices,
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalRecords = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving utility invoices");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving utility invoices"
                });
            }
        }

        /// Get all utility invoices from all imports with filtering and pagination
        [HttpGet("all-invoices")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllUtilityInvoices(
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

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
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
                        CustomerOib = u.CustomerOib,
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        FiscalizationMethod = u.FiscalizationMethod,
                        Jir = u.Jir,
                        Zki = u.Zki,
                        MojeRacunInvoiceId = u.MojeRacunInvoiceId,
                        MojeRacunStatus = u.MojeRacunStatus,
                        FiscalizedAt = u.FiscalizedAt,
                        CreatedAt = u.CreatedAt,
                        ItemCount = u.Items.Count,
                        ConsumptionDataCount = u.ConsumptionData.Count
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Retrieved {invoices.Count} of {totalCount} utility invoices",
                    Data = new
                    {
                        Items = invoices,
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalRecords = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all utility invoices");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving utility invoices"
                });
            }
        }

        /// Get only the invoices from the latest CSV import
        [HttpGet("imported-invoices")]
        public async Task<ActionResult<ApiResponse<object>>> GetLatestImportedInvoices(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Find the latest import time (most recent CreatedAt timestamp)
                var latestImportTime = await _context.UtilityInvoices
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => u.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestImportTime == default)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "No imported invoices found",
                        Data = new
                        {
                            Items = new List<UtilityInvoiceListResponse>(),
                            PageNumber = page,
                            PageSize = pageSize,
                            TotalRecords = 0,
                            TotalPages = 0,
                            LatestImportDate = (DateTime?)null
                        }
                    });
                }

                // Consider invoices created within 5 minutes of the latest import as part of the same batch
                var importTimeThreshold = latestImportTime.AddMinutes(-5);

                var query = _context.UtilityInvoices
                    .Include(u => u.Items)
                    .Include(u => u.ConsumptionData)
                    .Where(u => u.CreatedAt >= importTimeThreshold)
                    .AsQueryable();

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
                        CustomerOib = u.CustomerOib,
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        FiscalizationMethod = u.FiscalizationMethod,
                        Jir = u.Jir,
                        Zki = u.Zki,
                        MojeRacunInvoiceId = u.MojeRacunInvoiceId,
                        MojeRacunStatus = u.MojeRacunStatus,
                        FiscalizedAt = u.FiscalizedAt,
                        CreatedAt = u.CreatedAt,
                        ItemCount = u.Items.Count,
                        ConsumptionDataCount = u.ConsumptionData.Count
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Retrieved {totalCount} invoices from latest import on {latestImportTime:yyyy-MM-dd HH:mm:ss}",
                    Data = new
                    {
                        Items = invoices,
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalRecords = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        LatestImportDate = latestImportTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest imported invoices");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the latest imported invoices"
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
                    
                    // Fiscalization fields
                    FiscalizationStatus = invoice.FiscalizationStatus,
                    FiscalizationMethod = invoice.FiscalizationMethod,
                    FiscalizedAt = invoice.FiscalizedAt,
                    FiscalizationError = invoice.FiscalizationError,
                    
                    // FINA 1.0 fields
                    Jir = invoice.Jir,
                    Zki = invoice.Zki,
                    
                    // mojE-Račun 2.0 fields
                    MojeRacunInvoiceId = invoice.MojeRacunInvoiceId,
                    MojeRacunSubmittedAt = invoice.MojeRacunSubmittedAt,
                    MojeRacunStatus = invoice.MojeRacunStatus,
                    
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
                        ItemOrder = i.ItemOrder,
                        KpdCode = i.KpdCode,
                        TaxRate = i.TaxRate,
                        TaxCategoryCode = i.TaxCategoryCode
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
                        TooOldCount = g.Count(u => u.FiscalizationStatus == "too_old"),
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
                        TooOldCount = 0,
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

        /// Get all invoices from a specific import batch
        [HttpGet("by-batch/{batchId}")]
        public async Task<ActionResult<ApiResponse<object>>> GetInvoicesByBatch(
            string batchId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortDirection = "desc")
        {
            try
            {
                // Verify batch exists
                var batchExists = await _context.ImportBatches
                    .AnyAsync(b => b.BatchId == batchId);

                if (!batchExists)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Import batch '{batchId}' not found"
                    });
                }

                var query = _context.UtilityInvoices
                    .Include(u => u.Items)
                    .Include(u => u.ConsumptionData)
                    .Where(u => u.ImportBatchId == batchId)
                    .AsQueryable();

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "invoicenumber" => sortDirection?.ToLower() == "asc"
                        ? query.OrderBy(u => u.InvoiceNumber)
                        : query.OrderByDescending(u => u.InvoiceNumber),
                    "customername" => sortDirection?.ToLower() == "asc"
                        ? query.OrderBy(u => u.CustomerName)
                        : query.OrderByDescending(u => u.CustomerName),
                    "totalamount" => sortDirection?.ToLower() == "asc"
                        ? query.OrderBy(u => u.TotalAmount)
                        : query.OrderByDescending(u => u.TotalAmount),
                    "issuedate" => sortDirection?.ToLower() == "asc"
                        ? query.OrderBy(u => u.IssueDate)
                        : query.OrderByDescending(u => u.IssueDate),
                    "fiscalizationstatus" => sortDirection?.ToLower() == "asc"
                        ? query.OrderBy(u => u.FiscalizationStatus)
                        : query.OrderByDescending(u => u.FiscalizationStatus),
                    _ => sortDirection?.ToLower() == "asc"
                        ? query.OrderBy(u => u.CreatedAt)
                        : query.OrderByDescending(u => u.CreatedAt)
                };

                var totalCount = await query.CountAsync();

                var invoices = await query
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
                        CustomerOib = u.CustomerOib,
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        FiscalizationMethod = u.FiscalizationMethod,
                        Jir = u.Jir,
                        Zki = u.Zki,
                        MojeRacunInvoiceId = u.MojeRacunInvoiceId,
                        MojeRacunStatus = u.MojeRacunStatus,
                        FiscalizedAt = u.FiscalizedAt,
                        CreatedAt = u.CreatedAt,
                        ItemCount = u.Items.Count,
                        ConsumptionDataCount = u.ConsumptionData.Count
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Retrieved {invoices.Count} of {totalCount} invoices from batch '{batchId}'",
                    Data = new
                    {
                        Items = invoices,
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalRecords = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        BatchId = batchId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for batch {BatchId}", batchId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving invoices"
                });
            }
        }

        /// Update an existing utility invoice (only if not fiscalized)
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UtilityInvoiceResponse>>> UpdateUtilityInvoice(
            int id,
            [FromBody] UpdateUtilityInvoiceRequest request)
        {
            try
            {
                // Fetch the invoice with its items
                var invoice = await _context.UtilityInvoices
                    .Include(u => u.Items)
                    .Include(u => u.ConsumptionData)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<UtilityInvoiceResponse>
                    {
                        Success = false,
                        Message = "Račun nije pronađen" // "Invoice not found"
                    });
                }

                // ✅ IMPORTANT: Prevent editing fiscalized invoices (Croatian fiscal law)
                if (invoice.FiscalizationStatus == "fiscalized")
                {
                    return BadRequest(new ApiResponse<UtilityInvoiceResponse>
                    {
                        Success = false,
                        Message = "Ne možete uređivati fiskalizirane račune. Fiskalizirani računi su zaštićeni zakonom."
                        // "Cannot edit fiscalized invoices. Fiscalized invoices are protected by law."
                    });
                }

                // Update customer information
                if (!string.IsNullOrWhiteSpace(request.CustomerName))
                    invoice.CustomerName = request.CustomerName;

                if (request.CustomerAddress != null)
                    invoice.CustomerAddress = request.CustomerAddress;

                if (request.CustomerCode != null)
                    invoice.CustomerCode = request.CustomerCode;

                if (request.CustomerOib != null)
                {
                    // Validate OIB format (11 digits)
                    if (!string.IsNullOrWhiteSpace(request.CustomerOib))
                    {
                        if (request.CustomerOib.Length != 11 || !System.Text.RegularExpressions.Regex.IsMatch(request.CustomerOib, @"^\d{11}$"))
                        {
                            return BadRequest(new ApiResponse<UtilityInvoiceResponse>
                            {
                                Success = false,
                                Message = "OIB mora imati točno 11 znamenki" // "OIB must have exactly 11 digits"
                            });
                        }
                    }
                    invoice.CustomerOib = request.CustomerOib;
                }

                if (request.PostalCode != null)
                    invoice.PostalCode = request.PostalCode;

                if (request.City != null)
                    invoice.City = request.City;

                // Update invoice information
                if (request.InvoiceNumber != null)
                {
                    // Check for duplicate invoice number
                    var duplicateExists = await _context.UtilityInvoices
                        .AnyAsync(u => u.InvoiceNumber == request.InvoiceNumber && u.Id != id);

                    if (duplicateExists)
                    {
                        return BadRequest(new ApiResponse<UtilityInvoiceResponse>
                        {
                            Success = false,
                            Message = $"Broj računa '{request.InvoiceNumber}' već postoji"
                            // "Invoice number already exists"
                        });
                    }
                    invoice.InvoiceNumber = request.InvoiceNumber;
                }

                if (request.Building != null)
                    invoice.Building = request.Building;

                if (request.Period != null)
                    invoice.Period = request.Period;

                if (request.IssueDate.HasValue)
                    invoice.IssueDate = request.IssueDate.Value;

                if (request.DueDate.HasValue)
                    invoice.DueDate = request.DueDate.Value;

                if (request.ValidityDate.HasValue)
                    invoice.ValidityDate = request.ValidityDate;

                if (request.BankAccount != null)
                    invoice.BankAccount = request.BankAccount;

                if (request.Model != null)
                    invoice.Model = request.Model;

                // Update service types
                if (request.ServiceTypeHot != null)
                    invoice.ServiceTypeHot = request.ServiceTypeHot;

                if (request.ServiceTypeHeating != null)
                    invoice.ServiceTypeHeating = request.ServiceTypeHeating;

                // Update financial information
                if (request.SubTotal.HasValue)
                    invoice.SubTotal = request.SubTotal.Value;

                if (request.VatAmount.HasValue)
                    invoice.VatAmount = request.VatAmount.Value;

                if (request.TotalAmount.HasValue)
                    invoice.TotalAmount = request.TotalAmount.Value;

                // Update additional information
                if (request.DebtText != null)
                    invoice.DebtText = request.DebtText;

                if (request.ConsumptionText != null)
                    invoice.ConsumptionText = request.ConsumptionText;

                // ✅ Handle invoice items (add/edit/remove)
                if (request.Items != null)
                {
                    // Get IDs of items in the request
                    var requestItemIds = request.Items
                        .Where(i => i.Id.HasValue)
                        .Select(i => i.Id.Value)
                        .ToList();

                    // Remove items that are no longer in the request
                    var itemsToRemove = invoice.Items
                        .Where(i => !requestItemIds.Contains(i.Id))
                        .ToList();

                    foreach (var item in itemsToRemove)
                    {
                        _context.UtilityInvoiceItems.Remove(item);
                    }

                    // Update existing items and add new items
                    foreach (var requestItem in request.Items)
                    {
                        if (requestItem.Id.HasValue)
                        {
                            // Update existing item
                            var existingItem = invoice.Items.FirstOrDefault(i => i.Id == requestItem.Id.Value);
                            if (existingItem != null)
                            {
                                existingItem.Description = requestItem.Description;
                                existingItem.Unit = requestItem.Unit;
                                existingItem.Quantity = requestItem.Quantity;
                                existingItem.UnitPrice = requestItem.UnitPrice;
                                existingItem.Amount = requestItem.Amount;
                                existingItem.ItemOrder = requestItem.ItemOrder;
                            }
                        }
                        else
                        {
                            // Add new item
                            var newItem = new UtilityInvoiceItem
                            {
                                UtilityInvoiceId = invoice.Id,
                                Description = requestItem.Description,
                                Unit = requestItem.Unit,
                                Quantity = requestItem.Quantity,
                                UnitPrice = requestItem.UnitPrice,
                                Amount = requestItem.Amount,
                                ItemOrder = requestItem.ItemOrder
                            };
                            invoice.Items.Add(newItem);
                        }
                    }
                }

                // Update timestamp
                invoice.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated utility invoice {Id} by user", id);

                // Return updated invoice
                var updatedInvoice = await _context.UtilityInvoices
                    .Include(u => u.Items.OrderBy(i => i.ItemOrder))
                    .Include(u => u.ConsumptionData.OrderBy(c => c.ParameterOrder))
                    .FirstOrDefaultAsync(u => u.Id == id);

                var response = new UtilityInvoiceResponse
                {
                    Id = updatedInvoice!.Id,
                    Building = updatedInvoice.Building,
                    Period = updatedInvoice.Period,
                    InvoiceNumber = updatedInvoice.InvoiceNumber,
                    Model = updatedInvoice.Model,
                    IssueDate = updatedInvoice.IssueDate,
                    DueDate = updatedInvoice.DueDate,
                    ValidityDate = updatedInvoice.ValidityDate,
                    BankAccount = updatedInvoice.BankAccount,
                    CustomerCode = updatedInvoice.CustomerCode,
                    CustomerName = updatedInvoice.CustomerName,
                    CustomerAddress = updatedInvoice.CustomerAddress,
                    PostalCode = updatedInvoice.PostalCode,
                    City = updatedInvoice.City,
                    CustomerOib = updatedInvoice.CustomerOib,
                    ServiceTypeHot = updatedInvoice.ServiceTypeHot,
                    ServiceTypeHeating = updatedInvoice.ServiceTypeHeating,
                    SubTotal = updatedInvoice.SubTotal,
                    VatAmount = updatedInvoice.VatAmount,
                    TotalAmount = updatedInvoice.TotalAmount,
                    DebtText = updatedInvoice.DebtText,
                    ConsumptionText = updatedInvoice.ConsumptionText,
                    FiscalizationStatus = updatedInvoice.FiscalizationStatus,
                    FiscalizationMethod = updatedInvoice.FiscalizationMethod,
                    FiscalizedAt = updatedInvoice.FiscalizedAt,
                    FiscalizationError = updatedInvoice.FiscalizationError,
                    CreatedAt = updatedInvoice.CreatedAt,
                    UpdatedAt = updatedInvoice.UpdatedAt,
                    Items = updatedInvoice.Items.Select(i => new UtilityInvoiceItemResponse
                    {
                        Id = i.Id,
                        Description = i.Description,
                        Unit = i.Unit,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Amount = i.Amount,
                        ItemOrder = i.ItemOrder,
                        KpdCode = i.KpdCode,
                        TaxRate = i.TaxRate,
                        TaxCategoryCode = i.TaxCategoryCode
                    }).ToList(),
                    ConsumptionData = updatedInvoice.ConsumptionData.Select(c => new UtilityConsumptionDataResponse
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
                    Message = "Račun uspješno ažuriran", // "Invoice updated successfully"
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating utility invoice {Id}", id);
                return StatusCode(500, new ApiResponse<UtilityInvoiceResponse>
                {
                    Success = false,
                    Message = "Došlo je do greške prilikom ažuriranja računa" // "An error occurred while updating the invoice"
                });
            }
        }

        /// <summary>
        /// Add a new item to an existing utility invoice (only if not fiscalized)
        /// </summary>
        [HttpPost("{id}/items")]
        public async Task<ActionResult<ApiResponse<UtilityInvoiceResponse>>> AddInvoiceItem(
            int id,
            [FromBody] AddUtilityInvoiceItemRequest request)
        {
            try
            {
                // Fetch the invoice with its items
                var invoice = await _context.UtilityInvoices
                    .Include(u => u.Items)
                    .Include(u => u.ConsumptionData)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<UtilityInvoiceResponse>
                    {
                        Success = false,
                        Message = "Račun nije pronađen"
                    });
                }

                // ✅ IMPORTANT: Prevent adding items to fiscalized invoices
                if (invoice.FiscalizationStatus == "fiscalized")
                {
                    return BadRequest(new ApiResponse<UtilityInvoiceResponse>
                    {
                        Success = false,
                        Message = "Ne možete dodavati stavke fiskaliziranom računu. Fiskalizirani računi su zaštićeni zakonom."
                    });
                }

                // Determine the next item order
                var maxOrder = invoice.Items.Any() 
                    ? invoice.Items.Max(i => i.ItemOrder) 
                    : 0;

                // Create new item
                var newItem = new UtilityInvoiceItem
                {
                    UtilityInvoiceId = invoice.Id,
                    Description = request.Description,
                    Unit = request.Unit,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice,
                    Amount = request.Amount,
                    ItemOrder = maxOrder + 1,
                    KpdCode = request.KpdCode ?? "35.30.11",
                    TaxRate = request.TaxRate,
                    TaxCategoryCode = request.TaxCategoryCode
                };

                // Add item to invoice
                invoice.Items.Add(newItem);

                // ✅ Optionally recalculate totals
                if (request.RecalculateTotals)
                {
                    var newSubTotal = invoice.Items.Sum(i => i.Amount);
                    var taxMultiplier = request.TaxRate / 100m;
                    var newVatAmount = newSubTotal * taxMultiplier;
                    var newTotalAmount = newSubTotal + newVatAmount;

                    invoice.SubTotal = newSubTotal;
                    invoice.VatAmount = newVatAmount;
                    invoice.TotalAmount = newTotalAmount;

                    _logger.LogInformation(
                        "Recalculated invoice {InvoiceNumber} totals: SubTotal={SubTotal}, VAT={VAT}, Total={Total}",
                        invoice.InvoiceNumber, newSubTotal, newVatAmount, newTotalAmount);
                }

                // Update timestamp
                invoice.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added new item to invoice {InvoiceNumber}: {Description}",
                    invoice.InvoiceNumber, request.Description);

                // Return updated invoice
                var updatedInvoice = await _context.UtilityInvoices
                    .Include(u => u.Items.OrderBy(i => i.ItemOrder))
                    .Include(u => u.ConsumptionData.OrderBy(c => c.ParameterOrder))
                    .FirstOrDefaultAsync(u => u.Id == id);

                var response = new UtilityInvoiceResponse
                {
                    Id = updatedInvoice!.Id,
                    Building = updatedInvoice.Building,
                    Period = updatedInvoice.Period,
                    InvoiceNumber = updatedInvoice.InvoiceNumber,
                    Model = updatedInvoice.Model,
                    IssueDate = updatedInvoice.IssueDate,
                    DueDate = updatedInvoice.DueDate,
                    ValidityDate = updatedInvoice.ValidityDate,
                    BankAccount = updatedInvoice.BankAccount,
                    CustomerCode = updatedInvoice.CustomerCode,
                    CustomerName = updatedInvoice.CustomerName,
                    CustomerAddress = updatedInvoice.CustomerAddress,
                    PostalCode = updatedInvoice.PostalCode,
                    City = updatedInvoice.City,
                    CustomerOib = updatedInvoice.CustomerOib,
                    ServiceTypeHot = updatedInvoice.ServiceTypeHot,
                    ServiceTypeHeating = updatedInvoice.ServiceTypeHeating,
                    SubTotal = updatedInvoice.SubTotal,
                    VatAmount = updatedInvoice.VatAmount,
                    TotalAmount = updatedInvoice.TotalAmount,
                    DebtText = updatedInvoice.DebtText,
                    ConsumptionText = updatedInvoice.ConsumptionText,
                    FiscalizationStatus = updatedInvoice.FiscalizationStatus,
                    FiscalizationMethod = updatedInvoice.FiscalizationMethod,
                    FiscalizedAt = updatedInvoice.FiscalizedAt,
                    FiscalizationError = updatedInvoice.FiscalizationError,
                    CreatedAt = updatedInvoice.CreatedAt,
                    UpdatedAt = updatedInvoice.UpdatedAt,
                    Items = updatedInvoice.Items.Select(i => new UtilityInvoiceItemResponse
                    {
                        Id = i.Id,
                        Description = i.Description,
                        Unit = i.Unit,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Amount = i.Amount,
                        ItemOrder = i.ItemOrder,
                        KpdCode = i.KpdCode,
                        TaxRate = i.TaxRate,
                        TaxCategoryCode = i.TaxCategoryCode
                    }).ToList(),
                    ConsumptionData = updatedInvoice.ConsumptionData.Select(c => new UtilityConsumptionDataResponse
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
                    Message = $"Stavka '{request.Description}' uspješno dodana računu",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to utility invoice {Id}", id);
                return StatusCode(500, new ApiResponse<UtilityInvoiceResponse>
                {
                    Success = false,
                    Message = "Došlo je do greške prilikom dodavanja stavke"
                });
            }
        }

        /// Export invoices to CSV with fiscalization details
        /// GET /api/UtilityInvoices/export-csv
        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportInvoicesToCsv(
    [FromQuery] string? batchId = null,
    [FromQuery] string? period = null,
    [FromQuery] string? building = null,
    [FromQuery] string? fiscalizationStatus = null,
    [FromQuery] string? fiscalizationMethod = null,
    [FromQuery] DateTime? dateFrom = null,
    [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var query = _context.UtilityInvoices
                    .Include(u => u.Items.OrderBy(i => i.ItemOrder))
                    .Include(u => u.ConsumptionData.OrderBy(c => c.ParameterOrder))
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(batchId))
                    query = query.Where(u => u.ImportBatchId == batchId);
                if (!string.IsNullOrEmpty(period))
                    query = query.Where(u => u.Period == period);
                if (!string.IsNullOrEmpty(building))
                    query = query.Where(u => u.Building.Contains(building));
                if (!string.IsNullOrEmpty(fiscalizationStatus))
                    query = query.Where(u => u.FiscalizationStatus == fiscalizationStatus);
                if (!string.IsNullOrEmpty(fiscalizationMethod))
                    query = query.Where(u => u.FiscalizationMethod == fiscalizationMethod);
                if (dateFrom.HasValue)
                    query = query.Where(u => u.IssueDate >= dateFrom.Value);
                if (dateTo.HasValue)
                    query = query.Where(u => u.IssueDate <= dateTo.Value);

                var invoices = await query.OrderBy(u => u.InvoiceNumber).ToListAsync();

                if (!invoices.Any())
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nema računa za izvoz"
                    });

                var csv = GenerateCsvContent(invoices);
                var fileName = $"FiskaliziraniRacuni_{DateTime.Now:dd.MM.yyyy}.csv";
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                var bom = System.Text.Encoding.UTF8.GetPreamble();
                var csvWithBom = bom.Concat(bytes).ToArray();

                _logger.LogInformation("Exported {Count} invoices to CSV", invoices.Count);
                return File(csvWithBom, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting invoices to CSV");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Došlo je do greške prilikom izvoza računa"
                });
            }
        }

        private string GenerateCsvContent(List<UtilityInvoice> invoices)
        {
            var sb = new System.Text.StringBuilder();

            // CSV Header - Original format with fiscalization fields added at the end
            sb.AppendLine(
                "ZGRADA,RAZDOBLJE,BRRN,MODEL,KKSIFRA,KKIME,KKADR,KKPTT,KKGRAD,KKOIB," +
                "DATISP,DATRN,DATVAL,RNIBAN,TIP_TV,TIP_GRI," +
                "OPIS1,JED1,KOL1,CIJ1,IZN1," +
                "OPIS2,JED2,KOL2,CIJ2,IZN2," +
                "OPIS3,JED3,KOL3,CIJ3,IZN3," +
                "OPIS4,JED4,KOL4,CIJ4,IZN4," +
                "OPIS5,JED5,KOL5,CIJ5,IZN5," +
                "UKUPNO_BEZ,PDV_1,REZ_1,SVEUKUP,DUG_TXT,POTROS_TXT," +
                "TXT_TAB1,NUM_TAB1,TXT_TAB2,NUM_TAB2,TXT_TAB3,NUM_TAB3,TXT_TAB4,NUM_TAB4,TXT_TAB5,NUM_TAB5," +
                "TXT_TAB6,NUM_TAB6,TXT_TAB7,NUM_TAB7,TXT_TAB8,NUM_TAB8,TXT_TAB9,NUM_TAB9,TXT_TAB10,NUM_TAB10," +
                "JIR,ZKI,ElectronicId"
            );

            // CSV Data
            foreach (var inv in invoices)
            {
                // Get up to 5 items (OPIS1-5, JED1-5, etc.)
                var items = inv.Items.OrderBy(i => i.ItemOrder).Take(5).ToList();

                // Get up to 10 consumption data parameters (TXT_TAB1-10, NUM_TAB1-10)
                var consumptionData = inv.ConsumptionData.OrderBy(c => c.ParameterOrder).Take(10).ToList();

                var formattedNumber = FormatInvoiceNumber(inv.InvoiceNumber);

                sb.Append($"{EscapeCsvField(inv.Building)},"); // ZGRADA
                sb.Append($"{EscapeCsvField(inv.Period)},"); // RAZDOBLJE
                sb.Append($"{formattedNumber},"); // BRRN
                sb.Append($"{EscapeCsvField(inv.Model)},"); // MODEL
                sb.Append($"{EscapeCsvField(inv.CustomerCode)},"); // KKSIFRA
                sb.Append($"{EscapeCsvField(inv.CustomerName)},"); // KKIME
                sb.Append($"{EscapeCsvField(inv.CustomerAddress)},"); // KKADR
                sb.Append($"{EscapeCsvField(inv.PostalCode)},"); // KKPTT
                sb.Append($"{EscapeCsvField(inv.City)},"); // KKGRAD
                sb.Append($"{EscapeCsvField(inv.CustomerOib)},"); // KKOIB
                sb.Append($"{inv.IssueDate:dd.MM.yyyy},"); // DATISP
                sb.Append($"{inv.DueDate:dd.MM.yyyy},"); // DATRN
                sb.Append($"{(inv.ValidityDate.HasValue ? inv.ValidityDate.Value.ToString("dd.MM.yyyy") : "")},"); // DATVAL
                sb.Append($"{EscapeCsvField(inv.BankAccount)},"); // RNIBAN
                sb.Append($"{EscapeCsvField(inv.ServiceTypeHot)},"); // TIP_TV
                sb.Append($"{EscapeCsvField(inv.ServiceTypeHeating)},"); // TIP_GRI

                // Items (up to 5: OPIS1-5, JED1-5, KOL1-5, CIJ1-5, IZN1-5)
                for (int i = 0; i < 5; i++)
                {
                    if (i < items.Count)
                    {
                        var item = items[i];
                        sb.Append($"{EscapeCsvField(item.Description)},"); // OPIS
                        sb.Append($"{EscapeCsvField(item.Unit)},"); // JED
                        sb.Append($"{item.Quantity:F3},"); // KOL
                        sb.Append($"{item.UnitPrice:F2},"); // CIJ
                        sb.Append($"{item.Amount:F2}"); // IZN
                    }
                    else
                    {
                        // Empty fields for missing items
                        sb.Append(",,0.000,0.00,0.00");
                    }
                    if (i < 4) sb.Append(","); // Add comma except after last item
                }

                sb.Append(","); // Separator before totals

                // Financial totals
                sb.Append($"{inv.SubTotal:F2},"); // UKUPNO_BEZ
                sb.Append($"{inv.VatAmount:F2},"); // PDV_1
                sb.Append($"0.00,"); // REZ_1 (reserved, always 0)
                sb.Append($"{inv.TotalAmount:F2},"); // SVEUKUP
                sb.Append($"{EscapeCsvField(inv.DebtText)},"); // DUG_TXT
                sb.Append($"{EscapeCsvField(inv.ConsumptionText)},"); // POTROS_TXT

                // Consumption data (up to 10: TXT_TAB1-10, NUM_TAB1-10)
                for (int i = 0; i < 10; i++)
                {
                    if (i < consumptionData.Count)
                    {
                        var data = consumptionData[i];
                        sb.Append($"{EscapeCsvField(data.ParameterName)},"); // TXT_TAB
                        sb.Append($"{(data.ParameterValue.HasValue ? data.ParameterValue.Value.ToString("F3") : "0.000")}"); // NUM_TAB
                    }
                    else
                    {
                        // Empty fields for missing consumption data
                        sb.Append(",0.000");
                    }
                    if (i < 9) sb.Append(","); // Add comma except after last parameter
                }

                sb.Append(","); // Separator before fiscalization fields

                // ✅ FISCALIZATION FIELDS (3 columns only)
                sb.Append($"{EscapeCsvField(inv.Jir ?? "")},"); // JIR (FINA only)
                sb.Append($"{EscapeCsvField(inv.Zki ?? "")},"); // ZKI (FINA only)
                sb.Append($"{EscapeCsvField(inv.MojeRacunInvoiceId ?? "")}"); // ElectronicId (mojE-Račun only)

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string FormatInvoiceNumber(string invoiceNumber)
        {
            if (string.IsNullOrEmpty(invoiceNumber)) return "";
            var parts = invoiceNumber.Split('-');
            // Extract middle part and format as {serial}/1/3
            return parts.Length >= 2 ? $"{parts[1].Trim()}/1/3" : $"{invoiceNumber}/1/3";
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return $"\"{field.Replace("\"", "\"\"")}\"";
            return field;
        }


    }
}