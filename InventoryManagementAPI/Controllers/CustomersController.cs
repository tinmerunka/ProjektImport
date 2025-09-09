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
    public class CustomersController : CompanyBaseController
    {
        public CustomersController(AppDbContext context) : base(context)
        {
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CustomerResponse>>>> GetCustomers([FromQuery] string? searchTerm = null)
        {
            try
            {
                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<List<CustomerResponse>>
                    {
                        Success = false,
                        Message = "No valid company selected or company not found"
                    });
                }

                var query = _context.Customers
                    .Where(c => c.CompanyId == companyProfile.Id) // Filter by company
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => c.Name.Contains(searchTerm) ||
                                           c.Oib.Contains(searchTerm) ||
                                           c.Email.Contains(searchTerm));
                }

                var customers = await query
                    .OrderBy(c => c.Name)
                    .Select(c => new CustomerResponse
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Address = c.Address,
                        Oib = c.Oib,
                        Email = c.Email,
                        Phone = c.Phone,
                        IsCompany = c.IsCompany,
                        CreatedAt = c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<CustomerResponse>>
                {
                    Success = true,
                    Message = "Customers retrieved successfully",
                    Data = customers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<CustomerResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customers"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetCustomer(int id)
        {
            try
            {
                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<CustomerResponse>
                    {
                        Success = false,
                        Message = "No valid company selected or company not found"
                    });
                }

                var customer = await _context.Customers
                    .Where(c => c.Id == id && c.CompanyId == companyProfile.Id)
                    .FirstOrDefaultAsync();

                if (customer == null)
                {
                    return NotFound(new ApiResponse<CustomerResponse>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                var response = new CustomerResponse
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Address = customer.Address,
                    Oib = customer.Oib,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    IsCompany = customer.IsCompany,
                    CreatedAt = customer.CreatedAt
                };

                return Ok(new ApiResponse<CustomerResponse>
                {
                    Success = true,
                    Message = "Customer retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CustomerResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customer"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CustomerResponse>>> CreateCustomer(CreateCustomerRequest request)
        {
            try
            {
                Console.WriteLine($"CreateCustomer called with: {request.Name}");

                var companyProfile = await GetSelectedCompanyAsync();
                Console.WriteLine($"Company profile retrieved: {companyProfile?.CompanyName ?? "NULL"}");

                if (companyProfile == null)
                {
                    Console.WriteLine("No valid company found");
                    return BadRequest(new ApiResponse<CustomerResponse>
                    {
                        Success = false,
                        Message = "No valid company selected or company not found"
                    });
                }

                Console.WriteLine($"Using company ID: {companyProfile.Id}");

                // Check if customer with same OIB already exists within this company
                if (!string.IsNullOrEmpty(request.Oib))
                {
                    var existingCustomer = await _context.Customers
                        .AnyAsync(c => c.Oib == request.Oib && c.CompanyId == companyProfile.Id);

                    Console.WriteLine($"OIB check - exists: {existingCustomer}");

                    if (existingCustomer)
                    {
                        return BadRequest(new ApiResponse<CustomerResponse>
                        {
                            Success = false,
                            Message = "Customer with this OIB already exists"
                        });
                    }
                }

                Console.WriteLine("Creating customer object...");

                var customer = new Customer
                {
                    Name = request.Name,
                    Address = request.Address ?? string.Empty,
                    Oib = request.Oib ?? string.Empty,
                    Email = request.Email ?? string.Empty,
                    Phone = request.Phone ?? string.Empty,
                    IsCompany = request.IsCompany,
                    CompanyId = companyProfile.Id, // Set the company ID
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"Customer object created with CompanyId: {customer.CompanyId}");

                _context.Customers.Add(customer);

                Console.WriteLine("About to save changes...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"Changes saved, customer ID: {customer.Id}");

                var response = new CustomerResponse
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Address = customer.Address,
                    Oib = customer.Oib,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    IsCompany = customer.IsCompany,
                    CreatedAt = customer.CreatedAt
                };

                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, new ApiResponse<CustomerResponse>
                {
                    Success = true,
                    Message = "Customer created successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateCustomer: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    if (ex.InnerException.InnerException != null)
                    {
                        Console.WriteLine($"Inner inner exception: {ex.InnerException.InnerException.Message}");
                    }
                }

                return StatusCode(500, new ApiResponse<CustomerResponse>
                {
                    Success = false,
                    Message = $"An error occurred while creating customer: {ex.Message}"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CustomerResponse>>> UpdateCustomer(int id, CreateCustomerRequest request)
        {
            try
            {
                var companyProfile = await GetSelectedCompanyAsync();
                if (companyProfile == null)
                {
                    return BadRequest(new ApiResponse<CustomerResponse>
                    {
                        Success = false,
                        Message = "No valid company selected or company not found"
                    });
                }

                var customer = await _context.Customers
                    .Where(c => c.Id == id && c.CompanyId == companyProfile.Id)
                    .FirstOrDefaultAsync();

                if (customer == null)
                {
                    return NotFound(new ApiResponse<CustomerResponse>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                // Check if OIB already exists for another customer within this company
                if (!string.IsNullOrEmpty(request.Oib))
                {
                    if (await _context.Customers.AnyAsync(c => c.Oib == request.Oib && c.Id != id && c.CompanyId == companyProfile.Id))
                    {
                        return BadRequest(new ApiResponse<CustomerResponse>
                        {
                            Success = false,
                            Message = "Another customer with this OIB already exists"
                        });
                    }
                }

                customer.Name = request.Name;
                customer.Address = request.Address ?? string.Empty;
                customer.Oib = request.Oib ?? string.Empty;
                customer.Email = request.Email ?? string.Empty;
                customer.Phone = request.Phone ?? string.Empty;
                customer.IsCompany = request.IsCompany;

                await _context.SaveChangesAsync();

                var response = new CustomerResponse
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Address = customer.Address,
                    Oib = customer.Oib,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    IsCompany = customer.IsCompany,
                    CreatedAt = customer.CreatedAt
                };

                return Ok(new ApiResponse<CustomerResponse>
                {
                    Success = true,
                    Message = "Customer updated successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CustomerResponse>
                {
                    Success = false,
                    Message = "An error occurred while updating customer"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteCustomer(int id)
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

                var customer = await _context.Customers
                    .Where(c => c.Id == id && c.CompanyId == companyProfile.Id)
                    .FirstOrDefaultAsync();

                if (customer == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                // Check if customer has invoices
                if (await _context.Invoices.AnyAsync(i => i.CustomerId == id))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Cannot delete customer with existing invoices"
                    });
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Customer deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting customer"
                });
            }
        }
    }
}