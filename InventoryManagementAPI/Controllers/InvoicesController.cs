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
    public class InvoicesController : CompanyBaseController
    {
        private readonly AppDbContext _context;

        public InvoicesController(AppDbContext context) : base(context)
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
                var companyId = GetSelectedCompanyId();
                if (!companyId.HasValue)
                {
                    return BadRequest(new ApiResponse<InvoiceResponse>
                    {
                        Success = false,
                        Message = "No company selected"
                    });
                }

                var invoice = await _context.Invoices
                    .Include(i => i.Items)
                    .Where(i => i.Id == id && i.CompanyId == companyId.Value) // Add company filtering
                    .FirstOrDefaultAsync();

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
                Console.WriteLine($"CreateInvoice called for CustomerId: {request.CustomerId}");
                Console.WriteLine($"Invoice items: {string.Join(", ", request.Items.Select(i => $"ProductId: {i.ProductId}"))}");

                // Get user's selected company
                var companyProfile = await GetSelectedCompanyAsync();
                Console.WriteLine($"Company profile: {companyProfile?.CompanyName ?? "NULL"} (ID: {companyProfile?.Id})");

                if (companyProfile == null)
                {
                    Console.WriteLine("No company profile found");
                    return BadRequest(new ApiResponse<InvoiceResponse>
                    {
                        Success = false,
                        Message = "No company selected or company not found"
                    });
                }

                // Get customer (ensure it belongs to the same company)
                var customer = await _context.Customers
                    .Where(c => c.Id == request.CustomerId && c.CompanyId == companyProfile.Id)
                    .FirstOrDefaultAsync();

                Console.WriteLine($"Customer found: {customer?.Name ?? "NULL"} (CompanyId: {customer?.CompanyId})");

                if (customer == null)
                {
                    // Check if customer exists but belongs to different company
                    var customerAnyCompany = await _context.Customers
                        .Where(c => c.Id == request.CustomerId)
                        .FirstOrDefaultAsync();

                    if (customerAnyCompany != null)
                    {
                        Console.WriteLine($"Customer exists but belongs to CompanyId: {customerAnyCompany.CompanyId}, user's CompanyId: {companyProfile.Id}");
                        return BadRequest(new ApiResponse<InvoiceResponse>
                        {
                            Success = false,
                            Message = $"Customer belongs to different company (Customer CompanyId: {customerAnyCompany.CompanyId}, Your CompanyId: {companyProfile.Id})"
                        });
                    }
                    else
                    {
                        Console.WriteLine("Customer doesn't exist at all");
                        return BadRequest(new ApiResponse<InvoiceResponse>
                        {
                            Success = false,
                            Message = "Customer not found"
                        });
                    }
                }

                // Generate invoice number
                string invoiceNumber;
                if (request.Type == InvoiceType.Offer)
                {
                    companyProfile.LastOfferNumber++;
                    var prefix = string.IsNullOrEmpty(companyProfile.OfferPrefix) ? "OFF" : companyProfile.OfferPrefix;
                    invoiceNumber = $"{prefix}-{DateTime.Now.Year}-{companyProfile.LastOfferNumber:D3}";
                }
                else
                {
                    companyProfile.LastInvoiceNumber++;
                    var prefix = string.IsNullOrEmpty(companyProfile.InvoicePrefix) ? "INV" : companyProfile.InvoicePrefix;
                    invoiceNumber = $"{prefix}-{DateTime.Now.Year}-{companyProfile.LastInvoiceNumber:D3}";
                }

                Console.WriteLine($"Generated invoice number: {invoiceNumber}");

                var dueDate = request.DueDate ?? DateTime.UtcNow.AddDays(30);

                // Create invoice with company ID
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
                    CompanyId = companyProfile.Id,
                    CompanyName = companyProfile.CompanyName,
                    CompanyAddress = companyProfile.Address,
                    CompanyOib = companyProfile.Oib,
                    Notes = request.Notes
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Invoice created with ID: {invoice.Id}");

                decimal subTotal = 0;
                decimal totalTaxAmount = 0;

                foreach (var itemRequest in request.Items)
                {
                    Console.WriteLine($"Processing item with ProductId: {itemRequest.ProductId}");

                    // Ensure product belongs to the same company
                    var product = await _context.Products
                        .Where(p => p.Id == itemRequest.ProductId && p.CompanyId == companyProfile.Id)
                        .FirstOrDefaultAsync();

                    Console.WriteLine($"Product found: {product?.Name ?? "NULL"} (CompanyId: {product?.CompanyId})");

                    if (product == null)
                    {
                        // Check if product exists but belongs to different company
                        var productAnyCompany = await _context.Products
                            .Where(p => p.Id == itemRequest.ProductId)
                            .FirstOrDefaultAsync();

                        if (productAnyCompany != null)
                        {
                            Console.WriteLine($"Product exists but belongs to CompanyId: {productAnyCompany.CompanyId}, user's CompanyId: {companyProfile.Id}");
                            return BadRequest(new ApiResponse<InvoiceResponse>
                            {
                                Success = false,
                                Message = $"Product {itemRequest.ProductId} belongs to different company (Product CompanyId: {productAnyCompany.CompanyId}, Your CompanyId: {companyProfile.Id})"
                            });
                        }
                        else
                        {
                            Console.WriteLine($"Product {itemRequest.ProductId} doesn't exist at all");
                            return BadRequest(new ApiResponse<InvoiceResponse>
                            {
                                Success = false,
                                Message = $"Product with ID {itemRequest.ProductId} not found"
                            });
                        }
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

                Console.WriteLine("Invoice creation completed successfully");

                // Return created invoice (rest of your existing code...)
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
                Console.WriteLine($"Error in CreateInvoice: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, new ApiResponse<InvoiceResponse>
                {
                    Success = false,
                    Message = $"An error occurred while creating invoice: {ex.Message}"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateInvoice(int id, CreateInvoiceRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

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
                        Message = "Račun nije pronađen"
                    });
                }

                // Only allow updates to draft invoices
                if (invoice.Status != InvoiceStatus.Draft)
                {
                    return BadRequest(new ApiResponse<InvoiceResponse>
                    {
                        Success = false,
                        Message = "Samo računi koji su 'draft' se mogu urediti"
                    });
                }

                // Update invoice type
                invoice.Type = request.Type;

                // Update customer information if customer changed
                if (request.CustomerId != invoice.CustomerId)
                {
                    var customer = await _context.Customers.FindAsync(request.CustomerId);
                    if (customer == null)
                    {
                        return BadRequest(new ApiResponse<InvoiceResponse>
                        {
                            Success = false,
                            Message = "Customer not found"
                        });
                    }

                    invoice.CustomerId = customer.Id;
                    invoice.CustomerName = customer.Name;
                    invoice.CustomerAddress = customer.Address;
                    invoice.CustomerOib = customer.Oib;
                }

                // Update due date and notes
                invoice.DueDate = request.DueDate ?? invoice.DueDate;
                invoice.Notes = request.Notes;

                // Update company information from current company profile
                var companyProfile = await _context.CompanyProfiles.FirstOrDefaultAsync();
                if (companyProfile != null)
                {
                    invoice.CompanyName = companyProfile.CompanyName;
                    invoice.CompanyAddress = companyProfile.Address;
                    invoice.CompanyOib = companyProfile.Oib;
                }

                // Update invoice number if type changed
                if (invoice.Type != request.Type)
                {
                    // Generate new invoice number based on type
                    companyProfile.LastInvoiceNumber++;
                    if (request.Type == InvoiceType.Offer)
                    {
                        invoice.InvoiceNumber = $"P-{DateTime.Now.Year}-{companyProfile.LastInvoiceNumber:D3}";
                    }
                    else
                    {
                        invoice.InvoiceNumber = $"R-{DateTime.Now.Year}-{companyProfile.LastInvoiceNumber:D3}";
                    }
                }

                // Remove existing invoice items
                _context.InvoiceItems.RemoveRange(invoice.Items);

                // Add new invoice items
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

                // Update financial totals
                invoice.SubTotal = subTotal;
                invoice.TaxAmount = totalTaxAmount;
                invoice.TotalAmount = subTotal + totalTaxAmount;
                invoice.TaxRate = subTotal > 0 ? (totalTaxAmount / subTotal) * 100 : 0;

                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return updated invoice with all details
                var updatedInvoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);

                var response = new InvoiceResponse
                {
                    Id = updatedInvoice.Id,
                    InvoiceNumber = updatedInvoice.InvoiceNumber,
                    Type = updatedInvoice.Type,
                    Status = updatedInvoice.Status,
                    IssueDate = updatedInvoice.IssueDate,
                    DueDate = updatedInvoice.DueDate,
                    CustomerId = updatedInvoice.CustomerId,
                    CustomerName = updatedInvoice.CustomerName,
                    CustomerAddress = updatedInvoice.CustomerAddress,
                    CustomerOib = updatedInvoice.CustomerOib,
                    CompanyName = updatedInvoice.CompanyName,
                    CompanyAddress = updatedInvoice.CompanyAddress,
                    CompanyOib = updatedInvoice.CompanyOib,
                    SubTotal = updatedInvoice.SubTotal,
                    TaxAmount = updatedInvoice.TaxAmount,
                    TotalAmount = updatedInvoice.TotalAmount,
                    TaxRate = updatedInvoice.TaxRate,
                    Notes = updatedInvoice.Notes,
                    CreatedAt = updatedInvoice.CreatedAt,
                    Items = updatedInvoice.Items.Select(item => new InvoiceItemResponse
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
                    Message = "Račun uspješno ažuriran",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<InvoiceResponse>
                {
                    Success = false,
                    Message = "Došlo je do greške prilikom ažuriranja računa"
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
                if (invoice.Status != InvoiceStatus.Draft )
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Poslani ili plaćeni računi se ne mogu obrisati"
                    });
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Račun uspješno obrisan"
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