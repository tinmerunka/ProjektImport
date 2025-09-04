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
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CustomerResponse>>>> GetCustomers([FromQuery] string? searchTerm = null)
        {
            try
            {
                var query = _context.Customers.AsQueryable();

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
                var customer = await _context.Customers.FindAsync(id);

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
                // Check if customer with same OIB already exists (if OIB provided)
                if (!string.IsNullOrEmpty(request.Oib))
                {
                    if (await _context.Customers.AnyAsync(c => c.Oib == request.Oib))
                    {
                        return BadRequest(new ApiResponse<CustomerResponse>
                        {
                            Success = false,
                            Message = "Customer with this OIB already exists"
                        });
                    }
                }

                var customer = new Customer
                {
                    Name = request.Name,
                    Address = request.Address,
                    Oib = request.Oib,
                    Email = request.Email,
                    Phone = request.Phone,
                    IsCompany = request.IsCompany
                };

                _context.Customers.Add(customer);
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

                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, new ApiResponse<CustomerResponse>
                {
                    Success = true,
                    Message = "Customer created successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CustomerResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating customer"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CustomerResponse>>> UpdateCustomer(int id, CreateCustomerRequest request)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);

                if (customer == null)
                {
                    return NotFound(new ApiResponse<CustomerResponse>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                // Check if OIB already exists for another customer
                if (!string.IsNullOrEmpty(request.Oib))
                {
                    if (await _context.Customers.AnyAsync(c => c.Oib == request.Oib && c.Id != id))
                    {
                        return BadRequest(new ApiResponse<CustomerResponse>
                        {
                            Success = false,
                            Message = "Another customer with this OIB already exists"
                        });
                    }
                }

                customer.Name = request.Name;
                customer.Address = request.Address;
                customer.Oib = request.Oib;
                customer.Email = request.Email;
                customer.Phone = request.Phone;
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
                var customer = await _context.Customers.FindAsync(id);

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