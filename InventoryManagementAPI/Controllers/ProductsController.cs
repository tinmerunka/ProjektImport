using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : CompanyBaseController
    {
        public ProductsController(AppDbContext context) : base(context)
        {
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<ProductListResponse>>> GetProducts([FromQuery] ProductSearchRequest request)
        {
            try
            {
                var companyId = GetSelectedCompanyId();
                if (!companyId.HasValue)
                {
                    return BadRequest(new ApiResponse<ProductListResponse>
                    {
                        Success = false,
                        Message = "No company selected"
                    });
                }

                var query = _context.Products
                    .Where(p => p.CompanyId == companyId.Value) // Filter by company
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(p => p.Name.Contains(request.SearchTerm) ||
                                           p.Description.Contains(request.SearchTerm) ||
                                           p.SKU.Contains(request.SearchTerm));
                }

                // Apply price filters
                if (request.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= request.MinPrice.Value);
                }

                if (request.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= request.MaxPrice.Value);
                }

                // Apply sorting
                query = request.SortBy.ToLower() switch
                {
                    "price" => request.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Price)
                        : query.OrderBy(p => p.Price),
                    "quantity" => request.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Quantity)
                        : query.OrderBy(p => p.Quantity),
                    _ => request.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Name)
                        : query.OrderBy(p => p.Name)
                };

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var products = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(p => new ProductResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Quantity = p.Quantity,
                        SKU = p.SKU,
                        ImageUrl = p.ImageUrl,
                        TaxRate = p.TaxRate,
                        Unit = p.Unit
                    })
                    .ToListAsync();

                var response = new ProductListResponse
                {
                    Products = products,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };

                return Ok(new ApiResponse<ProductListResponse>
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetProducts: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new ApiResponse<ProductListResponse>
                {
                    Success = false,
                    Message = $"An error occurred while retrieving products: {ex.Message}"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> GetProduct(int id)
        {
            try
            {
                var companyId = GetSelectedCompanyId();
                if (!companyId.HasValue)
                {
                    return BadRequest(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "No company selected"
                    });
                }

                var product = await _context.Products
                    .Where(p => p.Id == id && p.CompanyId == companyId.Value)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Proizvod/usluga nije pronađena"
                    });
                }

                var productResponse = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    SKU = product.SKU,
                    ImageUrl = product.ImageUrl,
                    TaxRate = product.TaxRate,
                    Unit = product.Unit
                };

                return Ok(new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Uspješno hvatanje podataka",
                    Data = productResponse
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetProduct: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = $"Došlo je do greške prilikom hvatanja podataka: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> CreateProduct(CreateProductRequest request)
        {
            try
            {
                Console.WriteLine($"CreateProduct called with: {request.Name}");

                // Validate model state first
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    Console.WriteLine($"Model state invalid: {string.Join("; ", errors)}");

                    return BadRequest(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = string.Join("; ", errors)
                    });
                }

                // Use the GetSelectedCompanyAsync method to verify company exists
                var companyProfile = await GetSelectedCompanyAsync();
                Console.WriteLine($"Company profile retrieved: {companyProfile?.CompanyName ?? "NULL"}");

                if (companyProfile == null)
                {
                    Console.WriteLine("No valid company found");
                    return BadRequest(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Ne postoji tvrtka"
                    });
                }

                Console.WriteLine($"Using company ID: {companyProfile.Id}");

                // Check if SKU already exists within the company
                var existingProduct = await _context.Products
                    .AnyAsync(p => p.SKU == request.SKU && p.CompanyId == companyProfile.Id);

                Console.WriteLine($"SKU check - exists: {existingProduct}");

                if (existingProduct)
                {
                    return BadRequest(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Proizvod/usluga s ovom šifrom već postoji"
                    });
                }

                Console.WriteLine($"Creating product: {request.Name}");
                Console.WriteLine($"ImageUrl received: {request.ImageUrl}");

                var product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Quantity = request.Quantity,
                    SKU = request.SKU,
                    ImageUrl = request.ImageUrl,
                    TaxRate = request.TaxRate,
                    Unit = request.Unit,
                    CompanyId = companyProfile.Id, // Use the verified company ID
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"Product object created, adding to context");

                _context.Products.Add(product);

                Console.WriteLine($"About to save changes");
                await _context.SaveChangesAsync();
                Console.WriteLine($"Changes saved, product ID: {product.Id}");

                var productResponse = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    SKU = product.SKU,
                    ImageUrl = product.ImageUrl,
                    TaxRate = product.TaxRate,
                    Unit = product.Unit
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
                    new ApiResponse<ProductResponse>
                    {
                        Success = true,
                        Message = "Uspješno kreiran proizvod/usluga",
                        Data = productResponse
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateProduct: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = $"Došlo je do greške prilikom kreiranja: {ex.Message}"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> UpdateProduct(int id, UpdateProductRequest request)
        {
            try
            {
                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Ne postoji tvrtka"
                    });
                }

                var product = await _context.Products
                    .Where(p => p.Id == id && p.CompanyId == companyProfile.Id)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Proizvod/usluga nije pronađena"
                    });
                }

                // Check if SKU already exists (excluding current product, within company)
                if (await _context.Products.AnyAsync(p => p.SKU == request.SKU && p.Id != id && p.CompanyId == companyProfile.Id))
                {
                    return BadRequest(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Proizvod/usluga s ovom šifrom već postoji"
                    });
                }

                product.Name = request.Name;
                product.Description = request.Description;
                product.Price = request.Price;
                product.Quantity = request.Quantity;
                product.SKU = request.SKU;
                product.ImageUrl = request.ImageUrl;
                product.TaxRate = request.TaxRate;
                product.Unit = request.Unit;

                await _context.SaveChangesAsync();

                var productResponse = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    SKU = product.SKU,
                    ImageUrl = product.ImageUrl,
                    TaxRate = product.TaxRate,
                    Unit = product.Unit
                };

                return Ok(new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Proizvod/usluga uspjesno ažurirana",
                    Data = productResponse
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateProduct: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = $"Došlo je do pogreške prilikom ažuriranja proizvoda/usluge: {ex.Message}"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No valid company selected or company not found"
                    });
                }

                var product = await _context.Products
                    .Where(p => p.Id == id && p.CompanyId == companyProfile.Id)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Proizvod/usluga nije pronađena"
                    });
                }

                // Check if product is used in any invoice items
                var invoiceItems = await _context.InvoiceItems
                    .Where(ii => ii.ProductId == id)
                    .Include(ii => ii.Invoice)
                    .ToListAsync();

                if (invoiceItems.Any())
                {
                    // Separate invoices and offers
                    var invoices = invoiceItems.Where(ii => ii.Invoice.Type == InvoiceType.Invoice).ToList();
                    var offers = invoiceItems.Where(ii => ii.Invoice.Type == InvoiceType.Offer).ToList();

                    var errorMessages = new List<string>();

                    if (invoices.Any())
                    {
                        var invoiceNumbers = invoices.Select(ii => ii.Invoice.InvoiceNumber).Distinct().ToList();
                        errorMessages.Add($"računima: {string.Join(", ", invoiceNumbers)}");
                    }

                    if (offers.Any())
                    {
                        var offerNumbers = offers.Select(ii => ii.Invoice.InvoiceNumber).Distinct().ToList();
                        errorMessages.Add($"ponudama: {string.Join(", ", offerNumbers)}");
                    }

                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Proizvod/usluga se ne može obrisati jer se koristi u {string.Join(" i ", errorMessages)}"
                    });
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Uspješno obrisano"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteProduct: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Došlo je do greške prilikom brisanja proizvoda: {ex.Message}"
                });
            }
        }
    }
}