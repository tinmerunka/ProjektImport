using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using InventoryManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : CompanyBaseController
    {
        private readonly AppDbContext _context;
        private readonly IFiscalizationService _fiscalizationService;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IQRCodeService _qrCodeService;

        public InvoicesController(AppDbContext context, IFiscalizationService fiscalizationService, ILogger<InvoicesController> logger, IQRCodeService qrCodeService) : base(context)
        {
            _context = context;
            _fiscalizationService = fiscalizationService;
            _logger = logger;
            _qrCodeService = qrCodeService;
        }

        // Helper method to generate PaymentMethodCode based on PaymentMethod
        private string GeneratePaymentMethodCode(string paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod))
                return "O"; // Default to "Ostalo"

            var method = paymentMethod.ToLowerInvariant().Trim();

            return method switch
            {
                var x when x.Contains("transakcij") || x.Contains("bankovn") || x.Contains("žiro") || x.Contains("racun") => "T",
                var x when x.Contains("gotovina") || x.Contains("cash") => "G",
                var x when x.Contains("kartica") || x.Contains("card") || x.Contains("visa") || x.Contains("mastercard") => "K",
                _ => "O" // Ostalo - everything else
            };
        }

        [HttpGet("debug-jwt-detailed")]
        [AllowAnonymous]
        public ActionResult DebugJwtDetailed()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader))
            {
                return Ok(new { Error = "No Authorization header" });
            }

            var parts = authHeader.Split(' ');
            if (parts.Length != 2 || parts[0] != "Bearer")
            {
                return Ok(new
                {
                    Error = "Invalid Authorization header format",
                    Parts = parts,
                    Expected = "Bearer <token>"
                });
            }

            var token = parts[1];

            try
            {
                // Try to decode the JWT manually
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                var claims = jsonToken.Claims.ToDictionary(c => c.Type, c => c.Value);

                // Check expiration
                var exp = jsonToken.ValidTo;
                var now = DateTime.UtcNow;

                return Ok(new
                {
                    TokenValid = true,
                    TokenFormat = "Valid JWT",
                    Claims = claims,
                    ExpiresAt = exp,
                    CurrentTime = now,
                    IsExpired = now > exp,
                    TimeDifference = (exp - now).TotalMinutes,
                    TokenLength = token.Length,
                    TokenDots = token.Count(c => c == '.')
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    TokenValid = false,
                    Error = ex.Message,
                    TokenPreview = token.Substring(0, Math.Min(50, token.Length)) + "..."
                });
            }
        }

        [HttpGet("debug-auth-pipeline")]
        [AllowAnonymous]
        public ActionResult DebugAuthPipeline()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            var debugInfo = new
            {
                HasAuthHeader = !string.IsNullOrEmpty(authHeader),
                AuthHeader = authHeader,
                AuthHeaderLength = authHeader?.Length ?? 0,

                // Check if user is authenticated
                IsAuthenticated = User?.Identity?.IsAuthenticated ?? false,

                // Get all claims if authenticated
                Claims = User?.Claims?.ToDictionary(c => c.Type, c => c.Value) ?? new Dictionary<string, string>(),

                // Check request headers
                AllHeaders = Request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),

                // Check if this is reaching the controller
                ControllerReached = true
            };

            return Ok(debugInfo);
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
                        IssueLocation = i.IssueLocation,
                        DueDate = i.DueDate,
                        PaidAmount = i.PaidAmount,
                        RemainingAmount = i.TotalAmount - i.PaidAmount,
                        CreatedAt = i.CreatedAt,
                        DeliveryDate = i.DeliveryDate,
                        Currency = i.Currency,
                        PaymentMethod = i.PaymentMethod,
                        PaymentMethodCode = i.PaymentMethodCode,
                        Notes = i.Notes,
                        Jir = i.Jir
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
                    IssueLocation = invoice.IssueLocation,
                    DueDate = invoice.DueDate,
                    DeliveryDate = invoice.DeliveryDate,
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
                    PaidAmount = invoice.PaidAmount,
                    Currency = invoice.Currency,
                    PaymentMethod = invoice.PaymentMethod,
                    PaymentMethodCode = invoice.PaymentMethodCode,
                    RemainingAmount = invoice.TotalAmount - invoice.PaidAmount,
                    TaxRate = invoice.TaxRate,
                    Notes = invoice.Notes,
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt,
                    Zki = invoice.Zki,
                    Jir = invoice.Jir,
                    FiscalizationStatus = invoice.FiscalizationStatus,
                    FiscalizedAt = invoice.FiscalizedAt,
                    FiscalizationError = invoice.FiscalizationError,
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
                Console.WriteLine($"Custom invoice number: {request.CustomInvoiceNumber ?? "Auto-generated"}");

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
                            Message = $"Customer belongs to different company"
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

                // Generate or use custom invoice number
                string invoiceNumber;

                if (!string.IsNullOrWhiteSpace(request.CustomInvoiceNumber))
                {
                    // Use custom invoice number provided by user
                    invoiceNumber = request.CustomInvoiceNumber.Trim();

                    // Check if custom invoice number already exists in the same company
                    var existingInvoice = await _context.Invoices
                        .Where(i => i.InvoiceNumber == invoiceNumber && i.CompanyId == companyProfile.Id)
                        .FirstOrDefaultAsync();

                    if (existingInvoice != null)
                    {
                        return BadRequest(new ApiResponse<InvoiceResponse>
                        {
                            Success = false,
                            Message = $"Broj računa '{invoiceNumber}' već postoji. Molimo odaberite drugi broj."
                        });
                    }

                    Console.WriteLine($"Using custom invoice number: {invoiceNumber}");
                }
                else
                {
                    // Auto-generate invoice number based on type
                    if (request.Type == InvoiceType.Offer)
                    {
                        // Find the highest number from existing offers in this company
                        var lastOfferNumbers = await _context.Invoices
                            .Where(i => i.Type == InvoiceType.Offer &&
                    i.CompanyId == companyProfile.Id)
                            .Select(i => i.InvoiceNumber)
                            .ToListAsync();

                        var nextOfferNumber =
                    ExtractHighestNumber(lastOfferNumbers) + 1;

                        var param1 =
                    string.IsNullOrEmpty(companyProfile.OfferParam1) ? "P" :
                     companyProfile.OfferParam1;
                        var param2 =
                    string.IsNullOrEmpty(companyProfile.OfferParam2) ? "" :
                    companyProfile.OfferParam2;

                        if (string.IsNullOrEmpty(param2))
                        {
                            invoiceNumber = $"{nextOfferNumber}/{param1}";
                        }
                        else
                        {
                            invoiceNumber =
                    $"{nextOfferNumber}/{param1}/{param2}";
                        }
                    }
                    else
                    {
                        // Find the highest number from existing invoices in  
                        var lastInvoiceNumbers = await _context.Invoices
                            .Where(i => i.Type == InvoiceType.Invoice &&
                    i.CompanyId == companyProfile.Id)
                            .Select(i => i.InvoiceNumber)
                            .ToListAsync();

                        var nextInvoiceNumber =
                    ExtractHighestNumber(lastInvoiceNumbers) + 1;

                        var param1 =
                    string.IsNullOrEmpty(companyProfile.InvoiceParam1) ? "R"
                     : companyProfile.InvoiceParam1;
                        var param2 =
                    string.IsNullOrEmpty(companyProfile.InvoiceParam2) ? ""
                    : companyProfile.InvoiceParam2;

                        if (string.IsNullOrEmpty(param2))
                        {
                            invoiceNumber = $"{nextInvoiceNumber}/{param1}";
                        }
                        else
                        {
                            invoiceNumber =
                    $"{nextInvoiceNumber}/{param1}/{param2}";
                        }
                    }

                    Console.WriteLine($"Auto-generated invoice number: {invoiceNumber}");
                }

                // Handle dates properly
                var currentDateTime = DateTime.UtcNow;
                var issueDate = request.IssueDate == default ? currentDateTime : request.IssueDate;
                var dueDate = request.DueDate ?? currentDateTime.AddDays(30);

                Console.WriteLine($"Issue date: {issueDate}, Due date: {dueDate}");

                // Auto-generate PaymentMethodCode if not provided
                var paymentMethodCode = !string.IsNullOrEmpty(request.PaymentMethodCode)
                    ? request.PaymentMethodCode
                    : GeneratePaymentMethodCode(request.PaymentMethod);

                // Create invoice with company ID
                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    Type = request.Type,
                    IssueDate = issueDate,
                    IssueLocation = request.IssueLocation ?? string.Empty, // Ensure it's not null
                    Currency = request.Currency ?? "EUR", // Provide default if null
                    PaymentMethod = request.PaymentMethod ?? string.Empty, // Ensure it's not null
                    PaymentMethodCode = paymentMethodCode,
                    DueDate = dueDate,
                    DeliveryDate = request.DeliveryDate, // This can be null
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    CustomerAddress = customer.Address,
                    CustomerOib = customer.Oib,
                    CompanyId = companyProfile.Id,
                    CompanyName = companyProfile.CompanyName,
                    CompanyAddress = companyProfile.Address,
                    CompanyOib = companyProfile.Oib,
                    Notes = request.Notes,
                    CreatedAt = currentDateTime,
                    Status = InvoiceStatus.Draft,
                    PaidAmount = 0, // Initialize to 0
                    FiscalizationStatus = "",
                    FiscalizedAt = null,
                    Zki = null,
                    Jir = null,
                    FiscalizationError = null
                };

                await AddTaxExemptionSummary(invoice);

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Invoice created with ID: {invoice.Id}, PaymentMethodCode: {invoice.PaymentMethodCode}");

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
                                Message = $"Product {itemRequest.ProductId} belongs to different company"
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
                    IssueLocation = createdInvoice.IssueLocation,
                    DueDate = createdInvoice.DueDate,
                    DeliveryDate = createdInvoice.DeliveryDate,
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
                    PaidAmount = createdInvoice.PaidAmount,
                    Currency = createdInvoice.Currency,
                    PaymentMethod = createdInvoice.PaymentMethod,
                    PaymentMethodCode = createdInvoice.PaymentMethodCode,
                    RemainingAmount = createdInvoice.TotalAmount - createdInvoice.PaidAmount,
                    TaxRate = createdInvoice.TaxRate,
                    Notes = createdInvoice.Notes,
                    CreatedAt = createdInvoice.CreatedAt,
                    UpdatedAt = createdInvoice.UpdatedAt,
                    Zki = createdInvoice.Zki,
                    Jir = createdInvoice.Jir,
                    FiscalizationStatus = createdInvoice.FiscalizationStatus,
                    FiscalizedAt = createdInvoice.FiscalizedAt,
                    FiscalizationError = createdInvoice.FiscalizationError,
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

        private async Task AddTaxExemptionSummary(Invoice invoice)
        {
            var zeroTaxItems = invoice.Items.Where(i => i.TaxRate == 0).ToList();

            if (!zeroTaxItems.Any())
                return;

            var exemptionLines = new List<string>();
            exemptionLines.Add("=== RAZLOZI OSLOBOĐENJA PDV-a ===");
            exemptionLines.Add("");

            foreach (var item in zeroTaxItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);

                if (product != null)
                {
                    var reason = !string.IsNullOrWhiteSpace(product.TaxReason)
                        ? product.TaxReason
                        : "Razlog nije naveden";

                    exemptionLines.Add($"• {item.ProductName}");
                    exemptionLines.Add($"  {reason}");
                    exemptionLines.Add("");
                }
            }

            invoice.TaxExemptionSummary = string.Join("\n", exemptionLines);

            // Dodaj i u Notes za ispis
            var notesPrefix = string.Join("\n", exemptionLines);
            invoice.Notes = string.IsNullOrEmpty(invoice.Notes)
                ? notesPrefix
                : $"{notesPrefix}\n---\n\n{invoice.Notes}";
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateInvoice(int id, CreateInvoiceRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<InvoiceResponse>
                    {
                        Success = false,
                        Message = "No company selected or company not found"
                    });
                }

                var invoice = await _context.Invoices
                    .Include(i => i.Items)
                    .Where(i => i.Id == id && i.CompanyId == companyProfile.Id)
                    .FirstOrDefaultAsync();

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

                // Store original type to check if it changed
                var originalType = invoice.Type;

                // Update invoice type
                invoice.Type = request.Type;

                // Update customer information if customer changed
                if (request.CustomerId != invoice.CustomerId)
                {
                    var customer = await _context.Customers
                        .Where(c => c.Id == request.CustomerId && c.CompanyId == companyProfile.Id)
                        .FirstOrDefaultAsync();

                    if (customer == null)
                    {
                        return BadRequest(new ApiResponse<InvoiceResponse>
                        {
                            Success = false,
                            Message = "Customer not found or access denied"
                        });
                    }

                    invoice.CustomerId = customer.Id;
                    invoice.CustomerName = customer.Name;
                    invoice.CustomerAddress = customer.Address;
                    invoice.CustomerOib = customer.Oib;
                }

                // Update all properties including the missing ones
                var currentDateTime = DateTime.UtcNow;
                var issueDate = request.IssueDate == default ? invoice.IssueDate : request.IssueDate;

                invoice.DueDate = request.DueDate ?? invoice.DueDate;
                invoice.IssueDate = issueDate;
                invoice.IssueLocation = request.IssueLocation ?? invoice.IssueLocation;
                invoice.Currency = request.Currency ?? invoice.Currency;
                invoice.PaymentMethod = request.PaymentMethod ?? invoice.PaymentMethod;

                // Auto-generate PaymentMethodCode if not provided, or if PaymentMethod changed
                if (!string.IsNullOrEmpty(request.PaymentMethodCode))
                {
                    invoice.PaymentMethodCode = request.PaymentMethodCode;
                }
                else if (request.PaymentMethod != invoice.PaymentMethod)
                {
                    invoice.PaymentMethodCode = GeneratePaymentMethodCode(request.PaymentMethod);
                }

                invoice.Notes = request.Notes ?? invoice.Notes;
                invoice.PaidAmount = request.PaidAmount;
                invoice.DeliveryDate = request.DeliveryDate;
                invoice.UpdatedAt = currentDateTime;

                Console.WriteLine($"Updated properties - PaymentMethod: {invoice.PaymentMethod}, PaymentMethodCode: {invoice.PaymentMethodCode}");

                // Update company information from current company profile
                invoice.CompanyName = companyProfile.CompanyName;
                invoice.CompanyAddress = companyProfile.Address;
                invoice.CompanyOib = companyProfile.Oib;

                // Handle invoice number update (custom or auto-generated)
                string newInvoiceNumber;

                if (!string.IsNullOrWhiteSpace(request.CustomInvoiceNumber))
                {
                    // Use custom invoice number provided by user
                    newInvoiceNumber = request.CustomInvoiceNumber.Trim();

                    // Check if custom invoice number already exists in the same company (excluding current invoice)
                    var existingInvoice = await _context.Invoices
                        .Where(i => i.InvoiceNumber == newInvoiceNumber &&
                                   i.CompanyId == companyProfile.Id &&
                                   i.Id != id)
                        .FirstOrDefaultAsync();

                    if (existingInvoice != null)
                    {
                        return BadRequest(new ApiResponse<InvoiceResponse>
                        {
                            Success = false,
                            Message = $"Broj računa '{newInvoiceNumber}' već postoji. Molimo odaberite drugi broj."
                        });
                    }

                    Console.WriteLine($"Using custom invoice number: {newInvoiceNumber}");
                }
                else
                {
                    // Auto-generate invoice number if type changed
                    if (originalType != request.Type)
                    {
                        if (request.Type == InvoiceType.Offer)
                        {
                            var lastOfferNumbers = await _context.Invoices
                                .Where(i => i.Type == InvoiceType.Offer && i.CompanyId == companyProfile.Id && i.Id != id)
                                .Select(i => i.InvoiceNumber)
                                .ToListAsync();

                            var nextOfferNumber = ExtractHighestNumber(lastOfferNumbers) + 1;

                            var param1 = string.IsNullOrEmpty(companyProfile.OfferParam1) ? "P" : companyProfile.OfferParam1;
                            var param2 = string.IsNullOrEmpty(companyProfile.OfferParam2) ? "" : companyProfile.OfferParam2;

                            if (string.IsNullOrEmpty(param2))
                            {
                                newInvoiceNumber = $"{nextOfferNumber}/{param1}";
                            }
                            else
                            {
                                newInvoiceNumber = $"{nextOfferNumber}/{param1}/{param2}";
                            }
                        }
                        else
                        {
                            var lastInvoiceNumbers = await _context.Invoices
                                .Where(i => i.Type == InvoiceType.Invoice && i.CompanyId == companyProfile.Id && i.Id != id)
                                .Select(i => i.InvoiceNumber)
                                .ToListAsync();

                            var nextInvoiceNumber = ExtractHighestNumber(lastInvoiceNumbers) + 1;

                            var param1 = string.IsNullOrEmpty(companyProfile.InvoiceParam1) ? "R" : companyProfile.InvoiceParam1;
                            var param2 = string.IsNullOrEmpty(companyProfile.InvoiceParam2) ? "" : companyProfile.InvoiceParam2;

                            if (string.IsNullOrEmpty(param2))
                            {
                                newInvoiceNumber = $"{nextInvoiceNumber}/{param1}";
                            }
                            else
                            {
                                newInvoiceNumber = $"{nextInvoiceNumber}/{param1}/{param2}";
                            }
                        }

                        Console.WriteLine($"Auto-generated new invoice number due to type change: {newInvoiceNumber}");
                    }
                    else
                    {
                        // Keep existing invoice number if type didn't change and no custom number provided
                        newInvoiceNumber = invoice.InvoiceNumber;
                        Console.WriteLine($"Keeping existing invoice number: {newInvoiceNumber}");
                    }
                }

                // Update the invoice number
                invoice.InvoiceNumber = newInvoiceNumber;

                // Remove existing invoice items
                _context.InvoiceItems.RemoveRange(invoice.Items);

                // Add new invoice items
                decimal subTotal = 0;
                decimal totalTaxAmount = 0;

                foreach (var itemRequest in request.Items)
                {
                    var product = await _context.Products
                        .Where(p => p.Id == itemRequest.ProductId && p.CompanyId == companyProfile.Id)
                        .FirstOrDefaultAsync();

                    if (product == null)
                    {
                        return BadRequest(new ApiResponse<InvoiceResponse>
                        {
                            Success = false,
                            Message = $"Product with ID {itemRequest.ProductId} not found or access denied"
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
                invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;

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
                    IssueLocation = updatedInvoice.IssueLocation,
                    DueDate = updatedInvoice.DueDate,
                    DeliveryDate = updatedInvoice.DeliveryDate,
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
                    Currency = updatedInvoice.Currency,
                    PaymentMethod = updatedInvoice.PaymentMethod,
                    PaymentMethodCode = updatedInvoice.PaymentMethodCode,
                    PaidAmount = updatedInvoice.PaidAmount,
                    RemainingAmount = updatedInvoice.TotalAmount - updatedInvoice.PaidAmount,
                    TaxRate = updatedInvoice.TaxRate,
                    Notes = updatedInvoice.Notes,
                    CreatedAt = updatedInvoice.CreatedAt,
                    UpdatedAt = updatedInvoice.UpdatedAt,
                    Zki = updatedInvoice.Zki,
                    Jir = updatedInvoice.Jir,
                    FiscalizationStatus = updatedInvoice.FiscalizationStatus,
                    FiscalizedAt = updatedInvoice.FiscalizedAt,
                    FiscalizationError = updatedInvoice.FiscalizationError,
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
                Console.WriteLine($"Error in UpdateInvoice: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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

        /// Fiskaliziraj postojeći račun
        [HttpPost("{id}/fiscalize")]
        public async Task<IActionResult> FiscalizeInvoice(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Include(i => i.Company)
                .FirstOrDefaultAsync(i => i.Id == id && i.Company.UserId.ToString() == userId);

            if (invoice == null)
                return NotFound();

            if (!invoice.Company.FiscalizationEnabled)
                return BadRequest("Fiscalization not enabled for this company");

            if (invoice.FiscalizationStatus == "fiscalized")
                return BadRequest("Invoice already fiscalized");

            try
            {
                invoice.FiscalizationStatus = "pending";
                await _context.SaveChangesAsync();

                var result = await _fiscalizationService.FiscalizeInvoiceAsync(invoice, invoice.Company);

                if (result.Success)
                {
                    invoice.FiscalizationStatus = "fiscalized";
                    invoice.Jir = result.Jir;
                    invoice.Zki = result.Zki;
                    invoice.FiscalizedAt = result.FiscalizedAt;
                    invoice.FiscalizationError = null;
                }
                else
                {
                    invoice.FiscalizationStatus = "failed";
                    invoice.FiscalizationError = result.Message;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    jir = result.Jir,
                    zki = result.Zki,
                    fiscalizedAt = result.FiscalizedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fiscalizing invoice {InvoiceId}", id);

                invoice.FiscalizationStatus = "failed";
                invoice.FiscalizationError = ex.Message;
                await _context.SaveChangesAsync();

                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// Finaliziraj račun (i opciono fiskaliziraj)
        [HttpPost("{id}/finalize")]
        public async Task<IActionResult> FinalizeInvoice(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Include(i => i.Company)
                .FirstOrDefaultAsync(i => i.Id == id && i.Company.UserId.ToString() == userId);

            if (invoice == null)
                return NotFound();

            if (invoice.Status == InvoiceStatus.Finalized)
                return BadRequest("Invoice already finalized");

            invoice.Status = InvoiceStatus.Finalized;

            // Automatska fiskalizacija ako je omogućena
            if (invoice.Company.FiscalizationEnabled &&
                invoice.Company.AutoFiscalize &&
                !string.IsNullOrEmpty(invoice.Company.FiscalizationCertificatePath))
            {
                try
                {
                    invoice.FiscalizationStatus = "pending";
                    await _context.SaveChangesAsync();

                    var result = await _fiscalizationService.FiscalizeInvoiceAsync(invoice, invoice.Company);

                    if (result.Success)
                    {
                        invoice.FiscalizationStatus = "fiscalized";
                        invoice.Jir = result.Jir;
                        invoice.Zki = result.Zki;
                        invoice.FiscalizedAt = result.FiscalizedAt;
                        invoice.FiscalizationError = null;
                    }
                    else
                    {
                        invoice.FiscalizationStatus = "failed";
                        invoice.FiscalizationError = result.Message;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error auto-fiscalizing invoice {InvoiceId}", id);
                    invoice.FiscalizationStatus = "failed";
                    invoice.FiscalizationError = ex.Message;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(invoice);
        }

        /// Get QR kod za fiskalizirani račun
        [HttpGet("{id}/qr-code")]
        public async Task<IActionResult> GetInvoiceQRCode(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.Company.UserId.ToString() == userId);

            if (invoice == null)
                return NotFound();

            if (string.IsNullOrEmpty(invoice.Jir))
                return BadRequest("Invoice is not fiscalized");

            try
            {
                var qrBytes = _qrCodeService.GenerateFiscalQRCode(invoice.Jir);
                return File(qrBytes, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error generating QR code: {ex.Message}" });
            }
        }

        /// Get QR kod kao Base64 string
        [HttpGet("{id}/qr-code-base64")]
        public async Task<IActionResult> GetInvoiceQRCodeBase64(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.Company.UserId.ToString() == userId);

            if (invoice == null)
                return NotFound();

            if (string.IsNullOrEmpty(invoice.Jir))
                return BadRequest("Invoice is not fiscalized");

            try
            {
                var qrBase64 = _qrCodeService.GenerateFiscalQRCodeBase64(invoice.Jir);
                return Ok(new { qrCode = $"data:image/png;base64,{qrBase64}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error generating QR code: {ex.Message}" });
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
                if (invoice.Status != 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Račun koji nije 'draft' se ne može izbrisati"
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

        private int ExtractHighestNumber(List<string>
  invoiceNumbers)
        {
            int highest = 0;

            foreach (var number in invoiceNumbers)
            {
                if (string.IsNullOrEmpty(number)) continue;

                var parts = number.Split('/');
                if (parts.Length > 0 && int.TryParse(parts[0],
        out int num))
                {
                    highest = Math.Max(highest, num);
                }
            }

            return highest;
        }
    }
}