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
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<ProductListResponse>>> GetProducts([FromQuery] ProductSearchRequest request)
        {
            try
            {
                var query = _context.Products.AsQueryable();

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
                        SKU = p.SKU
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
                return StatusCode(500, new ApiResponse<ProductListResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving products"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                var productResponse = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    SKU = product.SKU
                };

                return Ok(new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = productResponse
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the product"
                });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> CreateProduct(CreateProductRequest request)
        {
            try
            {
                // Check if SKU already exists
                if (await _context.Products.AnyAsync(p => p.SKU == request.SKU))
                {
                    return BadRequest(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "A product with this SKU already exists"
                    });
                }

                var product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Quantity = request.Quantity,
                    SKU = request.SKU
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var productResponse = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    SKU = product.SKU
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
                    new ApiResponse<ProductResponse>
                    {
                        Success = true,
                        Message = "Product created successfully",
                        Data = productResponse
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating the product"
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ProductResponse>>> UpdateProduct(int id, UpdateProductRequest request)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                // Check if SKU already exists (excluding current product)
                if (await _context.Products.AnyAsync(p => p.SKU == request.SKU && p.Id != id))
                {
                    return BadRequest(new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "A product with this SKU already exists"
                    });
                }

                product.Name = request.Name;
                product.Description = request.Description;
                product.Price = request.Price;
                product.Quantity = request.Quantity;
                product.SKU = request.SKU;

                await _context.SaveChangesAsync();

                var productResponse = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    SKU = product.SKU
                };

                return Ok(new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Product updated successfully",
                    Data = productResponse
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "An error occurred while updating the product"
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Product deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the product"
                });
            }
        }
    }
}