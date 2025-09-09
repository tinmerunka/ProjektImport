using InventoryManagementAPI.Data;
using InventoryManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InventoryManagementAPI.Controllers
{
    [Authorize]
    public abstract class CompanyBaseController : ControllerBase
    {
        protected readonly AppDbContext _context;

        protected CompanyBaseController(AppDbContext context)
        {
            _context = context;
        }

        protected int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        protected int? GetSelectedCompanyId()
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            return int.TryParse(companyIdClaim, out int companyId) ? companyId : null;
        }

        protected async Task<CompanyProfile> GetSelectedCompanyAsync()
        {
            var companyId = GetSelectedCompanyId();
            var userId = GetUserId();

            if (!companyId.HasValue) return null;

            // Ensure company belongs to the user
            return await _context.CompanyProfiles
                .Where(c => c.Id == companyId.Value && c.User.Id == userId)
                .FirstOrDefaultAsync();
        }

        protected async Task<List<CompanyProfile>> GetUserCompaniesAsync()
        {
            var userId = GetUserId();
            return await _context.CompanyProfiles
                .Where(c => c.User.Id == userId)
                .ToListAsync();
        }
    }
}
