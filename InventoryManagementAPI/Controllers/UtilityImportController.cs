using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/utility/[controller]")]
    [Authorize]
    public class UtilityImportController : ControllerBase
    {
        private readonly UtilityDbContext _context;
        private readonly ILogger<UtilityImportController> _logger;
        private readonly IWebHostEnvironment _environment;

        public UtilityImportController(
            UtilityDbContext context,
            ILogger<UtilityImportController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<object>>> UploadCsvFile(IFormFile file)
        {
            var stopwatch = Stopwatch.StartNew();
            string? batchId = null;
            ImportBatch? importBatch = null;
            var importErrors = new List<object>();

            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No file uploaded"
                    });
                }

                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Only CSV files are allowed"
                    });
                }

                // Create import batch record
                batchId = Guid.NewGuid().ToString();
                var username = User.Identity?.Name ?? "Unknown";

                importBatch = new ImportBatch
                {
                    BatchId = batchId,
                    FileName = file.FileName,
                    ImportedAt = DateTime.UtcNow,
                    ImportedBy = username,
                    FileSize = file.Length,
                    Status = ImportBatchStatus.InProgress,
                    TotalRecords = 0,
                    SuccessfulRecords = 0,
                    FailedRecords = 0,
                    SkippedRecords = 0
                };

                _context.ImportBatches.Add(importBatch);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Started import batch {BatchId} for file {FileName}", batchId, file.FileName);

                // Parse CSV
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                var csvContent = await reader.ReadToEndAsync();
                var lines = csvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                {
                    importBatch.Status = ImportBatchStatus.Failed;
                    importBatch.ErrorLog = JsonSerializer.Serialize(new[] { new { Error = "CSV file is empty or has no data rows" } });
                    await _context.SaveChangesAsync();

                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "CSV file is empty or has no data rows"
                    });
                }

                var headers = lines[0].Split(';');
                importBatch.TotalRecords = lines.Length - 1;
                await _context.SaveChangesAsync();

                int successCount = 0;
                int failedCount = 0;
                int skippedCount = 0;
                var importedInvoices = new List<UtilityInvoice>();

                // Process each line
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = lines[i].Split(';');
                        if (values.Length < headers.Length)
                        {
                            failedCount++;
                            importErrors.Add(new { Row = i + 1, Error = "Insufficient columns" });
                            continue;
                        }

                        var invoice = ParseCsvLineToInvoice(headers, values, batchId);

                        // Check for duplicates
                        var isDuplicate = await _context.UtilityInvoices
                            .AnyAsync(u => u.InvoiceNumber == invoice.InvoiceNumber);

                        if (isDuplicate)
                        {
                            skippedCount++;
                            importErrors.Add(new { Row = i + 1, InvoiceNumber = invoice.InvoiceNumber, Error = "Duplicate invoice number" });
                            continue;
                        }

                        importedInvoices.Add(invoice);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        importErrors.Add(new { Row = i + 1, Error = ex.Message });
                        _logger.LogError(ex, "Error parsing row {Row}", i + 1);
                    }
                }

                // Save all invoices
                if (importedInvoices.Any())
                {
                    await _context.UtilityInvoices.AddRangeAsync(importedInvoices);
                }

                // Update batch status
                stopwatch.Stop();
                importBatch.SuccessfulRecords = successCount;
                importBatch.FailedRecords = failedCount;
                importBatch.SkippedRecords = skippedCount;
                importBatch.ImportDurationMs = stopwatch.ElapsedMilliseconds;
                importBatch.Status = failedCount > 0
                    ? (successCount > 0 ? ImportBatchStatus.PartiallyCompleted : ImportBatchStatus.Failed)
                    : ImportBatchStatus.Completed;

                if (importErrors.Any())
                {
                    importBatch.ErrorLog = JsonSerializer.Serialize(importErrors);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Completed import batch {BatchId}: {Success} successful, {Failed} failed, {Skipped} skipped in {Duration}ms",
                    batchId, successCount, failedCount, skippedCount, stopwatch.ElapsedMilliseconds);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Import completed: {successCount} successful, {failedCount} failed, {skippedCount} skipped",
                    Data = new
                    {
                        BatchId = batchId,
                        FileName = file.FileName,
                        TotalRows = importBatch.TotalRecords,
                        SuccessfulImports = successCount,
                        FailedRows = failedCount,
                        SkippedRows = skippedCount,
                        ImportDurationMs = stopwatch.ElapsedMilliseconds,
                        Errors = importErrors,
                        Status = importBatch.Status.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CSV import");

                // Update batch status to failed
                if (importBatch != null)
                {
                    stopwatch.Stop();
                    importBatch.Status = ImportBatchStatus.Failed;
                    importBatch.ImportDurationMs = stopwatch.ElapsedMilliseconds;
                    importBatch.ErrorLog = JsonSerializer.Serialize(new[] { new { Error = ex.Message, StackTrace = ex.StackTrace } });
                    await _context.SaveChangesAsync();
                }

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"An error occurred during import: {ex.Message}",
                    Data = new { BatchId = batchId }
                });
            }
        }

        private UtilityInvoice ParseCsvLineToInvoice(string[] headers, string[] values, string batchId)
        {
            var invoice = new UtilityInvoice
            {
                ImportBatchId = batchId,
                CreatedAt = DateTime.UtcNow
            };

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                var header = headers[j].Trim();
                var value = values[j].Trim();

                switch (header)
                {
                    case "ZGRADA": invoice.Building = value; break;
                    case "RAZDOBLJE": invoice.Period = value; break;
                    case "BRRN": invoice.InvoiceNumber = value; break;
                    case "MODEL": invoice.Model = value; break;
                    case "DATISP":
                        invoice.IssueDate = ParseCroatianDate(value);
                        break;
                    case "DATRN":
                        invoice.DueDate = ParseCroatianDate(value);
                        break;
                    case "DATVAL":
                        if (!string.IsNullOrEmpty(value))
                            invoice.ValidityDate = ParseCroatianDate(value);
                        break;
                    case "RNIBAN": invoice.BankAccount = value; break;
                    case "KKSIFRA": invoice.CustomerCode = value; break;
                    case "KKIME": invoice.CustomerName = value; break;
                    case "KKADRESA": invoice.CustomerAddress = value; break;
                    case "KKPOSBROJ": invoice.PostalCode = value; break;
                    case "KKGRAD": invoice.City = value; break;
                    case "OIB": invoice.CustomerOib = value; break;
                    case "TIP_TV": invoice.ServiceTypeHot = value; break;
                    case "TIP_GRI": invoice.ServiceTypeHeating = value; break;
                    case "UKUPNO_BEZ":
                        invoice.SubTotal = ParseDecimal(value);
                        break;
                    case "PDV_1":
                        invoice.VatAmount = ParseDecimal(value);
                        break;
                    case "SVEUKUP":
                        invoice.TotalAmount = ParseDecimal(value);
                        break;
                    case "DUG_TXT": invoice.DebtText = value; break;
                    case "POTROS_TXT": invoice.ConsumptionText = value; break;
                }
            }

            return invoice;
        }

        private DateTime ParseCroatianDate(string dateStr)
        {
            var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy" };
            if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            return DateTime.Parse(dateStr);
        }

        private decimal ParseDecimal(string value)
        {
            value = value.Replace(",", ".");
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}