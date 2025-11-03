using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UtilityImportController : ControllerBase
    {
        private readonly UtilityDbContext _context;
        private readonly ILogger<UtilityImportController> _logger;

        public UtilityImportController(UtilityDbContext context, ILogger<UtilityImportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("csv")]
        public async Task<ActionResult<ApiResponse<ImportResult>>> ImportUtilityInvoicesFromCsv(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponse<ImportResult>
                    {
                        Success = false,
                        Message = "No file provided"
                    });
                }

                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiResponse<ImportResult>
                    {
                        Success = false,
                        Message = "Only CSV files are allowed"
                    });
                }

                var result = new ImportResult();
                var errors = new List<string>();

                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                var csvContent = await reader.ReadToEndAsync();
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                {
                    return BadRequest(new ApiResponse<ImportResult>
                    {
                        Success = false,
                        Message = "CSV file must contain at least a header and one data row"
                    });
                }

                // Skip header row
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    try
                    {
                        var invoice = await ParseCsvLineToUtilityInvoice(line, i + 1);
                        if (invoice != null)
                        {
                            _context.UtilityInvoices.Add(invoice);
                            result.ProcessedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Line {i + 1}: {ex.Message}");
                        result.ErrorCount++;
                        _logger.LogWarning(ex, "Error processing line {LineNumber}", i + 1);
                    }
                }

                if (result.ProcessedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    result.ImportedCount = result.ProcessedCount;
                }

                result.Errors = errors;

                return Ok(new ApiResponse<ImportResult>
                {
                    Success = true,
                    Message = $"Import completed. {result.ImportedCount} invoices imported, {result.ErrorCount} errors",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing utility invoices");
                return StatusCode(500, new ApiResponse<ImportResult>
                {
                    Success = false,
                    Message = $"Import failed: {ex.Message}"
                });
            }
        }

        private async Task<UtilityInvoice?> ParseCsvLineToUtilityInvoice(string csvLine, int lineNumber)
        {
            var columns = csvLine.Split(';').Select(c => c.Trim('"').Trim()).ToArray();

            if (columns.Length < 44) // Minimum required columns
            {
                throw new ArgumentException($"CSV line must have at least 44 columns, found {columns.Length}");
            }

            // Check if invoice number already exists
            var invoiceNumber = columns[2]; // BRRN
            if (await _context.UtilityInvoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber))
            {
                throw new ArgumentException($"Invoice number {invoiceNumber} already exists");
            }

            // Parse dates with Croatian format
            if (!DateTime.TryParseExact(columns[10], "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var issueDate))
            {
                throw new ArgumentException($"Invalid issue date: {columns[10]}");
            }

            if (!DateTime.TryParseExact(columns[11], "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueDate))
            {
                throw new ArgumentException($"Invalid due date: {columns[11]}");
            }

            DateTime? validityDate = null;
            if (!string.IsNullOrEmpty(columns[12]) && DateTime.TryParseExact(columns[12], "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var vDate))
            {
                validityDate = vDate;
            }

            // Parse financial amounts
            if (!decimal.TryParse(columns[41].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var subTotal))
            {
                throw new ArgumentException($"Invalid subtotal: {columns[41]}");
            }

            if (!decimal.TryParse(columns[42].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var vatAmount))
            {
                throw new ArgumentException($"Invalid VAT amount: {columns[42]}");
            }

            if (!decimal.TryParse(columns[44].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var totalAmount))
            {
                throw new ArgumentException($"Invalid total amount: {columns[44]}");
            }

            // Create utility invoice
            var invoice = new UtilityInvoice
            {
                Building = columns[0], // ZGRADA
                Period = columns[1], // RAZDOBLJE
                InvoiceNumber = columns[2], // BRRN
                Model = columns[3], // MODEL
                CustomerCode = columns[4], // KKSIFRA
                CustomerName = columns[5], // KKIME
                CustomerAddress = columns[6], // KKADR
                PostalCode = columns[7], // KKPTT
                City = columns[8], // KKGRAD
                CustomerOib = columns[9], // KKOIB
                IssueDate = issueDate,
                DueDate = dueDate,
                ValidityDate = validityDate,
                BankAccount = columns[13], // RNIBAN
                ServiceTypeHot = columns[14], // TIP_TV
                ServiceTypeHeating = columns[15], // TIP_GRI
                SubTotal = subTotal,
                VatAmount = vatAmount,
                TotalAmount = totalAmount,
                DebtText = columns.Length > 45 ? columns[45] : string.Empty, // DUG_TXT
                ConsumptionText = columns.Length > 46 ? columns[46] : string.Empty, // POTROS_TXT
                FiscalizationStatus = "not_required",
                CreatedAt = DateTime.UtcNow
            };

            // Parse service items (OPIS1-5, JED1-5, KOL1-5, CIJ1-5, IZN1-5)
            for (int i = 0; i < 5; i++)
            {
                int baseIndex = 16 + (i * 5); // Starting from OPIS1

                if (baseIndex + 4 < columns.Length && !string.IsNullOrEmpty(columns[baseIndex]))
                {
                    var item = new UtilityInvoiceItem
                    {
                        Description = columns[baseIndex], // OPIS
                        Unit = columns[baseIndex + 1], // JED
                        ItemOrder = i + 1
                    };

                    // Parse quantity
                    if (decimal.TryParse(columns[baseIndex + 2].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                    {
                        item.Quantity = quantity;
                    }

                    // Parse unit price
                    if (decimal.TryParse(columns[baseIndex + 3].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice))
                    {
                        item.UnitPrice = unitPrice;
                    }

                    // Parse amount
                    if (decimal.TryParse(columns[baseIndex + 4].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                    {
                        item.Amount = amount;
                    }

                    invoice.Items.Add(item);
                }
            }

            // Parse consumption data (TXT_TAB1-10, NUM_TAB1-10)
            int consumptionStartIndex = Math.Max(0, columns.Length - 20); // Last 20 columns are consumption data
            for (int i = 0; i < 10; i++)
            {
                int txtIndex = consumptionStartIndex + (i * 2);
                int numIndex = txtIndex + 1;

                if (txtIndex < columns.Length && !string.IsNullOrEmpty(columns[txtIndex]))
                {
                    var consumptionData = new UtilityConsumptionData
                    {
                        ParameterName = columns[txtIndex],
                        ParameterOrder = i + 1
                    };

                    if (numIndex < columns.Length && decimal.TryParse(columns[numIndex].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                    {
                        consumptionData.ParameterValue = value;
                    }

                    invoice.ConsumptionData.Add(consumptionData);
                }
            }

            return invoice;
        }

        [HttpGet("template")]
        public IActionResult DownloadCsvTemplate()
        {
            var csvContent = "ZGRADA;RAZDOBLJE;BRRN;MODEL;KKSIFRA;KKIME;KKADR;KKPTT;KKGRAD;KKOIB;DATISP;DATRN;DATVAL;RNIBAN;TIP_TV;TIP_GRI;OPIS1;JED1;KOL1;CIJ1;IZN1;OPIS2;JED2;KOL2;CIJ2;IZN2;OPIS3;JED3;KOL3;CIJ3;IZN3;OPIS4;JED4;KOL4;CIJ4;IZN4;OPIS5;JED5;KOL5;CIJ5;IZN5;UKUPNO_BEZ;PDV_1;REZ_1;SVEUKUP;DUG_TXT;POTROS_TXT;;;;;;TXT_TAB1;NUM_TAB1;TXT_TAB2;NUM_TAB2;TXT_TAB3;NUM_TAB3;TXT_TAB4;NUM_TAB4;TXT_TAB5;NUM_TAB5;TXT_TAB6;NUM_TAB6;TXT_TAB7;NUM_TAB7;TXT_TAB8;NUM_TAB8;TXT_TAB9;NUM_TAB9;TXT_TAB10;NUM_TAB10";

            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", "utility_invoice_template.csv");
        }
    }
}