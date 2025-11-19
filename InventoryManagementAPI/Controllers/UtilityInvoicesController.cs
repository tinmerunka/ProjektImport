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

        /// <summary>
        /// Base endpoint - Get ALL utility invoices (for import history and fallback)
        /// </summary>
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
                        CustomerOib = u.CustomerOib, // ✅ ADDED
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        Jir = u.Jir,
                        Zki = u.Zki,
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

        /// <summary>
        /// Get all utility invoices from all imports with filtering and pagination
        /// </summary>
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
                        CustomerOib = u.CustomerOib, // ✅ ADDED
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        Jir = u.Jir,
                        Zki = u.Zki,
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

        /// <summary>
        /// Get only the invoices from the latest CSV import
        /// </summary>
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
                        CustomerOib = u.CustomerOib, // ✅ ADDED
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        Jir = u.Jir,
                        Zki = u.Zki,
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

        /// <summary>
        /// Get all invoices from a specific import batch
        /// </summary>
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
                        CustomerOib = u.CustomerOib, // ✅ ADDED
                        IssueDate = u.IssueDate,
                        DueDate = u.DueDate,
                        TotalAmount = u.TotalAmount,
                        FiscalizationStatus = u.FiscalizationStatus,
                        Jir = u.Jir,
                        Zki = u.Zki,
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
                    Jir = updatedInvoice.Jir,
                    Zki = updatedInvoice.Zki,
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
                        ItemOrder = i.ItemOrder
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
    }
}