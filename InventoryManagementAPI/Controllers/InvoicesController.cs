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
    public class InvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InvoicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<InvoiceListResponse>>> GetInvoices(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] InvoiceType? type = null,
    [FromQuery] InvoiceStatus? status = null,
    [FromQuery] string? searchTerm = null)
        {
            try
            {
                var query = _context.Invoices.AsQueryable();

                // Apply filters
                if (type.HasValue)
                {
                    query = query.Where(i => i.Type == type.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(i => i.Status == status.Value);
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(i => i.InvoiceNumber.Contains(searchTerm) ||
                                           i.CustomerName.Contains(searchTerm));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination and get data
                var invoices = await query
                    .OrderByDescending(i => i.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(i => new InvoiceSummaryResponse
                    {
                        Id = i.Id,
                        InvoiceNumber = i.InvoiceNumber,
                        Type = i.Type,
                        Status = i.Status,
                        CustomerName = i.CustomerName,
                        TotalAmount = i.TotalAmount,
                        IssueDate = i.IssueDate,
                        DueDate = i.DueDate,
                        CreatedAt = i.CreatedAt
                    })
                    .ToListAsync();

                var response = new InvoiceListResponse
                {
                    Invoices = invoices,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(new ApiResponse<InvoiceListResponse>
                {
                    Success = true,
                    Message = "Invoices retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<InvoiceListResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving invoices"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<InvoiceResponse>
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                var response = new InvoiceResponse
                {
                    Id = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    Type = invoice.Type,
                    Status = invoice.Status,
                    IssueDate = invoice.IssueDate,
                    DueDate = invoice.DueDate,
                    CustomerId = invoice.CustomerId,
                    CustomerName = invoice.CustomerName,
                    CustomerAddress = invoice.CustomerAddress,
                    CustomerOib = invoice.CustomerOib,
                    CompanyName = invoice.CompanyName,
                    CompanyAddress = invoice.CompanyAddress,
                    CompanyOib = invoice.CompanyOib,
                    SubTotal = invoice.SubTotal,
                    TaxAmount = invoice.TaxAmount,
                    TotalAmount = invoice.TotalAmount,
                    TaxRate = invoice.TaxRate,
                    Notes = invoice.Notes,
                    CreatedAt = invoice.CreatedAt,
                    Items = invoice.Items.Select(item => new InvoiceItemResponse
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductSku = item.ProductSku,
                        ProductDescription = item.ProductDescription,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        TaxRate = item.TaxRate,
                        LineTotal = item.LineTotal,
                        LineTaxAmount = item.LineTaxAmount,
                        Unit = item.Unit
                    }).ToList()
                };

                return Ok(new ApiResponse<InvoiceResponse>
                {
                    Success = true,
                    Message = "Invoice retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<InvoiceResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving invoice"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<InvoiceResponse>>> CreateInvoice(CreateInvoiceRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get customer
                var customer = await _context.Customers.FindAsync(request.CustomerId);
                if (customer == null)
                {
                    return BadRequest(new ApiResponse<InvoiceResponse>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                // Get company profile
                var companyProfile = await _context.CompanyProfiles.FirstOrDefaultAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<InvoiceResponse>
                    {
                        Success = false,
                        Message = "Company profile not found. Please set up company profile first."
                    });
                }

                // Generate invoice number
                companyProfile.LastInvoiceNumber++;
                var invoiceNumber = $"{companyProfile.InvoicePrefix}-{DateTime.Now.Year}-{companyProfile.LastInvoiceNumber:D3}";

                // Set due date if not provided
                var dueDate = request.DueDate ?? DateTime.UtcNow.AddDays(30);

                // Create invoice
                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    Type = request.Type,
                    IssueDate = DateTime.UtcNow,
                    DueDate = dueDate,
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    CustomerAddress = customer.Address,
                    CustomerOib = customer.Oib,
                    CompanyName = companyProfile.CompanyName,
                    CompanyAddress = companyProfile.Address,
                    CompanyOib = companyProfile.Oib,
                    Notes = request.Notes
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync(); // Save to get invoice ID

                // Process invoice items
                decimal subTotal = 0;
                decimal totalTaxAmount = 0;

                foreach (var itemRequest in request.Items)
                {
                    var product = await _context.Products.FindAsync(itemRequest.ProductId);
                    if (product == null)
                    {
                        return BadRequest(new ApiResponse<InvoiceResponse>
                        {
                            Success = false,
                            Message = $"Product with ID {itemRequest.ProductId} not found"
                        });
                    }

                    var unitPrice = itemRequest.OverridePrice ?? product.Price;
                    var taxRate = itemRequest.OverrideTaxRate ?? product.TaxRate;
                    var lineTotal = unitPrice * itemRequest.Quantity;
                    var lineTaxAmount = lineTotal * (taxRate / 100);

                    var invoiceItem = new InvoiceItem
                    {
                        InvoiceId = invoice.Id,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductSku = product.SKU,
                        ProductDescription = product.Description,
                        UnitPrice = unitPrice,
                        Quantity = itemRequest.Quantity,
                        TaxRate = taxRate,
                        LineTotal = lineTotal,
                        LineTaxAmount = lineTaxAmount,
                        Unit = product.Unit
                    };

                    _context.InvoiceItems.Add(invoiceItem);

                    subTotal += lineTotal;
                    totalTaxAmount += lineTaxAmount;
                }

                // Update invoice totals
                invoice.SubTotal = subTotal;
                invoice.TaxAmount = totalTaxAmount;
                invoice.TotalAmount = subTotal + totalTaxAmount;
                invoice.TaxRate = subTotal > 0 ? (totalTaxAmount / subTotal) * 100 : 0;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return created invoice
                var createdInvoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);

                var response = new InvoiceResponse
                {
                    Id = createdInvoice.Id,
                    InvoiceNumber = createdInvoice.InvoiceNumber,
                    Type = createdInvoice.Type,
                    Status = createdInvoice.Status,
                    IssueDate = createdInvoice.IssueDate,
                    DueDate = createdInvoice.DueDate,
                    CustomerId = createdInvoice.CustomerId,
                    CustomerName = createdInvoice.CustomerName,
                    CustomerAddress = createdInvoice.CustomerAddress,
                    CustomerOib = createdInvoice.CustomerOib,
                    CompanyName = createdInvoice.CompanyName,
                    CompanyAddress = createdInvoice.CompanyAddress,
                    CompanyOib = createdInvoice.CompanyOib,
                    SubTotal = createdInvoice.SubTotal,
                    TaxAmount = createdInvoice.TaxAmount,
                    TotalAmount = createdInvoice.TotalAmount,
                    TaxRate = createdInvoice.TaxRate,
                    Notes = createdInvoice.Notes,
                    CreatedAt = createdInvoice.CreatedAt,
                    Items = createdInvoice.Items.Select(item => new InvoiceItemResponse
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductSku = item.ProductSku,
                        ProductDescription = item.ProductDescription,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        TaxRate = item.TaxRate,
                        LineTotal = item.LineTotal,
                        LineTaxAmount = item.LineTaxAmount,
                        Unit = item.Unit
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, new ApiResponse<InvoiceResponse>
                {
                    Success = true,
                    Message = "Invoice created successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<InvoiceResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating invoice"
                });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateInvoiceStatus(int id, UpdateInvoiceStatusRequest request)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                invoice.Status = request.Status;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Invoice status updated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating invoice status"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                // Only allow deletion of draft invoices
                if (invoice.Status != InvoiceStatus.Draft)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Only draft invoices can be deleted"
                    });
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Invoice deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting invoice"
                });
            }
        }
    }
}