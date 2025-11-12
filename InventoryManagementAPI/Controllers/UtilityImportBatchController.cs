using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/utility/import")]
    [Authorize]
    public class UtilityImportBatchController : ControllerBase
    {
        private readonly UtilityDbContext _context;
        private readonly ILogger<UtilityImportBatchController> _logger;

        public UtilityImportBatchController(UtilityDbContext context, ILogger<UtilityImportBatchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all import batches with pagination
        /// </summary>
        [HttpGet("batches")]
        public async Task<ActionResult<ApiResponse<object>>> GetImportBatches(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.ImportBatches
                    .Include(b => b.Invoices)
                    .AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ImportBatchStatus>(status, true, out var batchStatus))
                {
                    query = query.Where(b => b.Status == batchStatus);
                }

                var totalCount = await query.CountAsync();

                var batches = await query
                    .OrderByDescending(b => b.ImportedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new ImportBatchListResponse
                    {
                        Id = b.Id,
                        BatchId = b.BatchId,
                        FileName = b.FileName,
                        ImportedAt = b.ImportedAt,
                        TotalRecords = b.TotalRecords,
                        SuccessfulRecords = b.SuccessfulRecords,
                        FailedRecords = b.FailedRecords,
                        Status = b.Status.ToString(),
                        InvoiceCount = b.Invoices.Count
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Retrieved {batches.Count} of {totalCount} import batches",
                    Data = new
                    {
                        Items = batches,
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalRecords = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving import batches");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving import batches"
                });
            }
        }

        /// <summary>
        /// Get single batch details with error log
        /// </summary>
        [HttpGet("batches/{batchId}")]
        public async Task<ActionResult<ApiResponse<ImportBatchResponse>>> GetImportBatch(string batchId)
        {
            try
            {
                var batch = await _context.ImportBatches
                    .Include(b => b.Invoices)
                    .FirstOrDefaultAsync(b => b.BatchId == batchId);

                if (batch == null)
                {
                    return NotFound(new ApiResponse<ImportBatchResponse>
                    {
                        Success = false,
                        Message = "Import batch not found"
                    });
                }

                var response = new ImportBatchResponse
                {
                    Id = batch.Id,
                    BatchId = batch.BatchId,
                    FileName = batch.FileName,
                    ImportedAt = batch.ImportedAt,
                    ImportedBy = batch.ImportedBy,
                    TotalRecords = batch.TotalRecords,
                    SuccessfulRecords = batch.SuccessfulRecords,
                    FailedRecords = batch.FailedRecords,
                    SkippedRecords = batch.SkippedRecords,
                    Status = batch.Status.ToString(),
                    ImportDurationMs = batch.ImportDurationMs,
                    FileSize = batch.FileSize,
                    InvoiceCount = batch.Invoices.Count
                };

                return Ok(new ApiResponse<ImportBatchResponse>
                {
                    Success = true,
                    Message = "Import batch retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving import batch {BatchId}", batchId);
                return StatusCode(500, new ApiResponse<ImportBatchResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the import batch"
                });
            }
        }

        /// <summary>
        /// Get the most recent import batch
        /// </summary>
        [HttpGet("batches/latest")]
        public async Task<ActionResult<ApiResponse<ImportBatchResponse>>> GetLatestBatch()
        {
            try
            {
                var batch = await _context.ImportBatches
                    .Include(b => b.Invoices)
                    .OrderByDescending(b => b.ImportedAt)
                    .FirstOrDefaultAsync();

                if (batch == null)
                {
                    return NotFound(new ApiResponse<ImportBatchResponse>
                    {
                        Success = false,
                        Message = "No import batches found"
                    });
                }

                var response = new ImportBatchResponse
                {
                    Id = batch.Id,
                    BatchId = batch.BatchId,
                    FileName = batch.FileName,
                    ImportedAt = batch.ImportedAt,
                    ImportedBy = batch.ImportedBy,
                    TotalRecords = batch.TotalRecords,
                    SuccessfulRecords = batch.SuccessfulRecords,
                    FailedRecords = batch.FailedRecords,
                    SkippedRecords = batch.SkippedRecords,
                    Status = batch.Status.ToString(),
                    ImportDurationMs = batch.ImportDurationMs,
                    FileSize = batch.FileSize,
                    InvoiceCount = batch.Invoices.Count
                };

                return Ok(new ApiResponse<ImportBatchResponse>
                {
                    Success = true,
                    Message = "Latest import batch retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest import batch");
                return StatusCode(500, new ApiResponse<ImportBatchResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the latest import batch"
                });
            }
        }

        /// <summary>
        /// Get import statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<ImportStatsResponse>>> GetImportStats()
        {
            try
            {
                var stats = await _context.ImportBatches
                    .GroupBy(b => 1)
                    .Select(g => new ImportStatsResponse
                    {
                        TotalBatches = g.Count(),
                        TotalInvoices = g.Sum(b => b.SuccessfulRecords),
                        TotalSuccessful = g.Sum(b => b.SuccessfulRecords),
                        TotalFailed = g.Sum(b => b.FailedRecords),
                        OverallSuccessRate = g.Sum(b => b.TotalRecords) > 0
                            ? (decimal)g.Sum(b => b.SuccessfulRecords) / g.Sum(b => b.TotalRecords) * 100
                            : 0,
                        LastImportDate = g.Max(b => b.ImportedAt),
                        AverageImportDurationMs = (long)g.Average(b => b.ImportDurationMs)
                    })
                    .FirstOrDefaultAsync();

                if (stats == null)
                {
                    stats = new ImportStatsResponse
                    {
                        TotalBatches = 0,
                        TotalInvoices = 0,
                        TotalSuccessful = 0,
                        TotalFailed = 0,
                        OverallSuccessRate = 0,
                        LastImportDate = null,
                        AverageImportDurationMs = 0
                    };
                }

                // Get fiscalization stats
                var fiscalStats = await _context.UtilityInvoices
                    .GroupBy(i => 1)
                    .Select(g => new
                    {
                        Fiscalized = g.Count(i => i.FiscalizationStatus == "fiscalized"),
                        Pending = g.Count(i => i.FiscalizationStatus == "not_required")
                    })
                    .FirstOrDefaultAsync();

                stats.FiscalizedInvoices = fiscalStats?.Fiscalized ?? 0;
                stats.PendingInvoices = fiscalStats?.Pending ?? 0;

                return Ok(new ApiResponse<ImportStatsResponse>
                {
                    Success = true,
                    Message = "Import statistics retrieved successfully",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving import statistics");
                return StatusCode(500, new ApiResponse<ImportStatsResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving statistics"
                });
            }
        }

        /// <summary>
        /// Delete an import batch and all associated invoices (only if not fiscalized)
        /// </summary>
        [HttpDelete("batches/{batchId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteImportBatch(string batchId)
        {
            try
            {
                var batch = await _context.ImportBatches
                    .Include(b => b.Invoices)
                    .FirstOrDefaultAsync(b => b.BatchId == batchId);

                if (batch == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Import batch not found"
                    });
                }

                // Check if any invoice in the batch is fiscalized
                var hasFiscalizedInvoices = batch.Invoices.Any(i => i.FiscalizationStatus == "fiscalized");
                if (hasFiscalizedInvoices)
                {
                    var fiscalizedCount = batch.Invoices.Count(i => i.FiscalizationStatus == "fiscalized");
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Cannot delete batch. It contains {fiscalizedCount} fiscalized invoice(s). Fiscalized invoices cannot be deleted per Croatian fiscal law."
                    });
                }

                var invoiceCount = batch.Invoices.Count;

                // Delete all invoices in the batch (cascade will handle items and consumption data)
                _context.UtilityInvoices.RemoveRange(batch.Invoices);

                // Delete the batch
                _context.ImportBatches.Remove(batch);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted import batch {BatchId} with {InvoiceCount} invoices", batchId, invoiceCount);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Import batch deleted successfully. {invoiceCount} invoice(s) removed.",
                    Data = new
                    {
                        BatchId = batchId,
                        DeletedInvoices = invoiceCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting import batch {BatchId}", batchId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the import batch"
                });
            }
        }
    }
}