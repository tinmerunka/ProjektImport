using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace InventoryManagementAPI.Services
{
    /// <summary>
    /// Service for mojE-Račun 2.0 fiscalization (UBL 2.1 format)
    /// Croatian Tax Administration e-invoicing system
    /// </summary>
    public interface IMojeRacunService
    {
        Task<MojeRacunResponse> SubmitInvoiceAsync(UtilityInvoice invoice, CompanyProfile company);
        string GenerateUblXml(UtilityInvoice invoice, CompanyProfile company);
        Task<MojeRacunResponse> CheckInvoiceStatusAsync(string invoiceId, CompanyProfile company);
        Task<MojeRacunResponse> TestConnectionAsync(CompanyProfile company);
        Task<string> DownloadInvoiceXmlAsync(long electronicId, CompanyProfile company); // NEW
        Task<List<OutboxInvoiceHeader>> QueryOutboxAsync(CompanyProfile company, OutboxQueryFilter? filter = null);
    }

    public class MojeRacunService : IMojeRacunService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MojeRacunService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        // mojE-Račun API endpoints (CORRECTED based on actual API)
        private const string MOJE_RACUN_TEST_BASE = "https://demo.moj-eracun.hr/apis/v2";
        private const string MOJE_RACUN_PROD_BASE = "https://api.moj-eracun.hr/apis/v2";

        private const string PING_ENDPOINT = "/Ping";
        private const string SEND_ENDPOINT = "/send";  // FIXED: Changed from /OutgoingInvoices/Send
        private const string STATUS_ENDPOINT = "/OutgoingInvoices/Status";
        private const string RECEIVE_ENDPOINT = "/receive";  // NEW

        // UBL 2.1 Namespaces
        private const string UBL_INVOICE_NS = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private const string UBL_CAC_NS = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private const string UBL_CBC_NS = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        public MojeRacunService(
            ILogger<MojeRacunService> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
            _httpClient = CreateHttpClient();
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler();
            if (_environment.IsDevelopment())
            {
                handler.ServerCertificateCustomValidationCallback = (req, cert, chain, errs) => true;
            }
            
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            
            client.DefaultRequestHeaders.Add("User-Agent", "Croatian-mojE-Racun-Client/2.0");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            return client;
        }

        public async Task<MojeRacunResponse> SubmitInvoiceAsync(UtilityInvoice invoice, CompanyProfile company)
        {
            try
            {
                _logger.LogInformation("Starting mojE-Račun submission for invoice {InvoiceNumber}", 
                    invoice.InvoiceNumber);

                if (company == null)
                    throw new ArgumentNullException(nameof(company), "Company profile is required");

                if (!company.MojeRacunEnabled)
                    throw new InvalidOperationException("mojE-Račun is not enabled for this company");

                if (string.IsNullOrEmpty(company.MojeRacunClientId) || string.IsNullOrEmpty(company.MojeRacunClientSecret))
                    throw new InvalidOperationException("mojE-Račun credentials (username/password) are not configured");

                if (string.IsNullOrEmpty(company.MojeRacunApiKey))
                    throw new InvalidOperationException("mojE-Račun SoftwareId (API Key) is not configured");

                // Generate UBL 2.1 XML - SEND IT DIRECTLY WITHOUT WRAPPER
                var ublXml = GenerateUblXml(invoice, company);
                
                // Save debug XML
                SaveXmlForDebug(invoice.InvoiceNumber, ublXml, "UBL_Direct");

                // Determine base URL
                var baseUrl = company.MojeRacunEnvironment == "production" 
                    ? MOJE_RACUN_PROD_BASE 
                    : MOJE_RACUN_TEST_BASE;

                var submitUrl = $"{baseUrl}{SEND_ENDPOINT}";

                // Prepare request payload matching mojE-Račun API format
                var requestPayload = new
                {
                    Username = company.MojeRacunClientId,           // Username (e.g., "1083")
                    Password = company.MojeRacunClientSecret,       // Password (e.g., "test123")
                    CompanyId = company.Oib,                         // Company OIB/Tax ID
                    CompanyBu = string.Empty,                        // Business Unit (optional)
                    SoftwareId = company.MojeRacunApiKey,           // Software ID (e.g., "Test-001")
                    File = ublXml                                    // FIXED: Send UBL XML directly, no wrapper
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Submitting invoice {InvoiceNumber} to {Url} with Username={Username}, SoftwareId={SoftwareId}", 
                    invoice.InvoiceNumber, submitUrl, company.MojeRacunClientId, company.MojeRacunApiKey);

                // Send request
                var response = await _httpClient.PostAsync(submitUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                // LOG AT INFORMATION LEVEL SO IT'S ALWAYS VISIBLE
                _logger.LogInformation("========== mojE-Račun API Response ==========");
                _logger.LogInformation("Status Code: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Response Body: {Response}", responseBody);
                _logger.LogInformation("=============================================");

                // Also save response to file for debugging
                SaveResponseForDebug(invoice.InvoiceNumber, responseBody, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("mojE-Račun submission failed: {StatusCode} - {Response}",
                        response.StatusCode, responseBody);

                    return new MojeRacunResponse
                    {
                        Success = false,
                        Message = $"Submission failed: {response.StatusCode} - {responseBody}",
                        RawResponse = responseBody
                    };
                }

                // Parse response
                _logger.LogInformation("Attempting to parse mojE-Račun response JSON...");
                var apiResponse = JsonSerializer.Deserialize<MojeRacunApiResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse == null)
                {
                    _logger.LogError("Failed to deserialize response. Raw response: {Response}", responseBody);
                    return new MojeRacunResponse
                    {
                        Success = false,
                        Message = "Failed to parse mojE-Račun response",
                        RawResponse = responseBody
                    };
                }

                _logger.LogInformation("Parsed ElectronicId: {ElectronicId}, StatusName: {StatusName}",
                    apiResponse.ElectronicId, apiResponse.StatusName);

                return new MojeRacunResponse
                {
                    Success = true,
                    Message = $"Invoice submitted successfully to mojE-Račun (Status: {apiResponse.StatusName})",
                    InvoiceId = apiResponse.ElectronicId.ToString(),
                    Status = apiResponse.StatusName?.ToLower() ?? "sent",
                    SubmittedAt = apiResponse.Sent ?? DateTime.UtcNow,
                    RawResponse = responseBody
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "mojE-Račun submission failed for invoice {InvoiceNumber}", 
                    invoice.InvoiceNumber);
                
                return new MojeRacunResponse
                {
                    Success = false,
                    Message = $"mojE-Račun submission failed: {ex.Message}"
                };
            }
        }
        
        private void SaveResponseForDebug(string invoiceNumber, string responseContent, int statusCode)
        {
            try
            {
                // Sanitize invoice number for filesystem
                var safeInvoiceNumber = invoiceNumber
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace(":", "_")
                    .Replace("*", "_")
                    .Replace("?", "_")
                    .Replace("\"", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace("|", "_");

                // Create organized folder structure: FiscalizationDebug/MojeRacun/{InvoiceNumber}/
                var rootPath = _environment.WebRootPath ?? _environment.ContentRootPath ?? Directory.GetCurrentDirectory();
                var invoiceFolder = Path.Combine(rootPath, "FiscalizationDebug", "MojeRacun", safeInvoiceNumber);

                if (!Directory.Exists(invoiceFolder))
                {
                    Directory.CreateDirectory(invoiceFolder);
                    _logger.LogInformation("Created mojE-Račun debug folder: {InvoiceFolder}", invoiceFolder);
                }

                var fileName = $"02-Response-{statusCode}.json";
                var filePath = Path.Combine(invoiceFolder, fileName);
                File.WriteAllText(filePath, responseContent, Encoding.UTF8);

                // Save metadata file with timestamp and details
                var metadataPath = Path.Combine(invoiceFolder, "metadata.json");
                var metadata = new
                {
                    InvoiceNumber = invoiceNumber,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Type = "mojE-Račun Fiscalization",
                    Files = new[]
                    {
                        new { Order = 1, File = "01-Request-UBL.xml", Description = "UBL 2.1 XML invoice sent to mojE-Račun" },
                        new { Order = 2, File = "02-Response-{StatusCode}.json", Description = "JSON response from mojE-Račun API" }
                    }
                };
                
                File.WriteAllText(metadataPath, 
                    System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), 
                    Encoding.UTF8);

                _logger.LogInformation("✅ mojE-Račun Response saved: {InvoiceNumber}/{FileName}", safeInvoiceNumber, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save mojE-Račun debug response");
            }
        }

        public async Task<MojeRacunResponse> TestConnectionAsync(CompanyProfile company)
        {
            try
            {
                _logger.LogInformation("Testing mojE-Račun connection for company {CompanyId}", company.Id);

                if (!company.MojeRacunEnabled)
                {
                    return new MojeRacunResponse
                    {
                        Success = false,
                        Message = "mojE-Račun is not enabled for this company"
                    };
                }

                if (string.IsNullOrEmpty(company.MojeRacunClientId) || string.IsNullOrEmpty(company.MojeRacunClientSecret))
                {
                    return new MojeRacunResponse
                    {
                        Success = false,
                        Message = "Username or password not configured"
                    };
                }

                // Determine base URL
                var baseUrl = company.MojeRacunEnvironment == "production"
                    ? MOJE_RACUN_PROD_BASE
                    : MOJE_RACUN_TEST_BASE;

                // Test with Ping endpoint
                var pingUrl = $"{baseUrl}{PING_ENDPOINT}";

                _logger.LogInformation("Pinging mojE-Račun at {PingUrl}", pingUrl);

                var response = await _httpClient.GetAsync(pingUrl);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var pingResponse = JsonSerializer.Deserialize<MojeRacunPingResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var isOk = pingResponse?.Status?.Equals("ok", StringComparison.OrdinalIgnoreCase) ?? false;

                    return new MojeRacunResponse
                    {
                        Success = isOk,
                        Message = isOk 
                            ? $"Successfully connected to mojE-Račun ({company.MojeRacunEnvironment} environment). {pingResponse.Message}"
                            : $"Connection test returned unexpected status: {pingResponse?.Status}",
                        Status = "connected",
                        RawResponse = responseBody
                    };
                }
                else
                {
                    return new MojeRacunResponse
                    {
                        Success = false,
                        Message = $"Connection test failed: {response.StatusCode}",
                        RawResponse = responseBody
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed for company {CompanyId}", company.Id);
                return new MojeRacunResponse
                {
                    Success = false,
                    Message = $"Connection test failed: {ex.Message}"
                };
            }
        }

        public string GenerateUblXml(UtilityInvoice invoice, CompanyProfile company)
        {

            // 🔍 DIAGNOSTIC: Log which OIBs are being used
            _logger.LogWarning("=== XML GENERATION DIAGNOSTIC ===");
            _logger.LogWarning("YOUR Company OIB (Supplier/Issuer): {CompanyOib}", company.Oib);
            _logger.LogWarning("YOUR Company Name: {CompanyName}", company.CompanyName);
            _logger.LogWarning("Customer OIB (From CSV): {CustomerOib}", invoice.CustomerOib ?? "NONE");
            _logger.LogWarning("Customer Name (From CSV): {CustomerName}", invoice.CustomerName);
            _logger.LogWarning("Invoice Number: {InvoiceNumber}", invoice.InvoiceNumber);
            _logger.LogWarning("Invoice Date: {IssueDate}", invoice.IssueDate);
            _logger.LogWarning("================================");

            var sb = new StringBuilder();
            
            // FIXED: Add proper XML declaration and schema location as per your example
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
            sb.AppendLine($" <Invoice xsi:schemaLocation=\"{UBL_INVOICE_NS} UBL-Invoice-2.1.xsd\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:cbc=\"{UBL_CBC_NS}\" xmlns:sbc=\"urn:oasis:names:specification:ubl:schema:xsd:SignatureBasicComponents-2\" xmlns:sac=\"urn:oasis:names:specification:ubl:schema:xsd:SignatureAggregateComponents-2\" xmlns:ext=\"urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2\" xmlns:exthr=\"urn:invoice:hr:schema:xsd:CommonExtensionComponents-1\" xmlns:cac=\"{UBL_CAC_NS}\" xmlns:sig=\"urn:oasis:names:specification:ubl:schema:xsd:CommonSignatureComponents-2\" xmlns=\"{UBL_INVOICE_NS}\">");
            
            // Header
            sb.AppendLine(" <cbc:CustomizationID>urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.hr:cius-2025:1.0#conformant#urn:mfin.gov.hr:ext-2025:1.0</cbc:CustomizationID>");
            sb.AppendLine(" <cbc:ProfileID>P3</cbc:ProfileID>");
            sb.AppendLine($" <cbc:ID>{invoice.InvoiceNumber}</cbc:ID>");
            sb.AppendLine(" <cbc:CopyIndicator>false</cbc:CopyIndicator>");
            
            // Use current date if invoice date is in future
            var issueDate = invoice.IssueDate > DateTime.Now ? DateTime.Now : invoice.IssueDate;
            sb.AppendLine($" <cbc:IssueDate>{issueDate:yyyy-MM-dd}</cbc:IssueDate>");
            sb.AppendLine($" <cbc:IssueTime>{DateTime.Now:HH:mm:ss}</cbc:IssueTime>");
            
            var dueDate = invoice.DueDate > DateTime.Now ? DateTime.Now.AddDays(30) : invoice.DueDate;
            sb.AppendLine($" <cbc:DueDate>{dueDate:yyyy-MM-dd}</cbc:DueDate>");
            
            sb.AppendLine(" <cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode>");
            sb.AppendLine("  <cbc:DocumentCurrencyCode listID=\"ISO 4217 Alpha\" listAgencyID=\"5\" listSchemeURI=\"http://docs.oasis-open.org/ubl/os-UBL-2.1/cl/gc/default/CurrencyCode-2.1.gc\">EUR</cbc:DocumentCurrencyCode>");
            
            // Supplier (Issuer) - YOUR COMPANY
            sb.AppendLine("  <cac:AccountingSupplierParty>");
            sb.AppendLine("   <cac:Party>");
            sb.AppendLine($"    <cbc:EndpointID schemeID=\"9934\">{company.Oib}</cbc:EndpointID>");
            sb.AppendLine("    <cac:PartyIdentification>");
            sb.AppendLine($"    <cbc:ID>9934:{company.Oib}</cbc:ID>");
            sb.AppendLine("    </cac:PartyIdentification>");
            sb.AppendLine("    <cac:PartyName>");
            sb.AppendLine($"    <cbc:Name>{XmlEscape(company.CompanyName)}</cbc:Name>");
            sb.AppendLine("    </cac:PartyName>");
            sb.AppendLine("    <cac:PostalAddress>");
            sb.AppendLine($"    <cbc:StreetName>{XmlEscape(company.Address)}</cbc:StreetName>");
            sb.AppendLine("    <cbc:CityName>Zagreb</cbc:CityName>");
            sb.AppendLine("    <cbc:PostalZone>10000</cbc:PostalZone>");
            sb.AppendLine("     <cac:AddressLine>");
            sb.AppendLine($"     <cbc:Line>{XmlEscape(company.Address)}, Zagreb, 10000</cbc:Line>");
            sb.AppendLine("     </cac:AddressLine>");
            sb.AppendLine("     <cac:Country>");
            sb.AppendLine("     <cbc:IdentificationCode>HR</cbc:IdentificationCode>");
            sb.AppendLine("     </cac:Country>");
            sb.AppendLine("    </cac:PostalAddress>");
            sb.AppendLine("    <cac:PartyTaxScheme>");
            sb.AppendLine($"    <cbc:CompanyID>HR{company.Oib}</cbc:CompanyID>");
            sb.AppendLine("     <cac:TaxScheme>");
            sb.AppendLine("     <cbc:ID>VAT</cbc:ID>");
            sb.AppendLine("     </cac:TaxScheme>");
            sb.AppendLine("    </cac:PartyTaxScheme>");
            sb.AppendLine("    <cac:PartyLegalEntity>");
            sb.AppendLine($"    <cbc:RegistrationName>{XmlEscape(company.CompanyName)}</cbc:RegistrationName>");
            sb.AppendLine($"    <cbc:CompanyID>{company.Oib}</cbc:CompanyID>");
            sb.AppendLine("    </cac:PartyLegalEntity>");
            sb.AppendLine("   </cac:Party>");
            sb.AppendLine("   <cac:SellerContact>");
            
            // Use FiscalizationOperatorOib if available, otherwise use company OIB
            var operatorOib = !string.IsNullOrEmpty(company.FiscalizationOperatorOib) 
                ? company.FiscalizationOperatorOib 
                : company.Oib;
            sb.AppendLine($"   <cbc:ID>{operatorOib}</cbc:ID>");
            sb.AppendLine("   <cbc:Name>OPERATER</cbc:Name>");
            sb.AppendLine("   </cac:SellerContact>");
            sb.AppendLine("  </cac:AccountingSupplierParty>");

            // Customer (Buyer) - FROM CSV
            sb.AppendLine("  <cac:AccountingCustomerParty>");
            sb.AppendLine("   <cac:Party>");

            bool hasCustomerOib = !string.IsNullOrEmpty(invoice.CustomerOib) &&
                      invoice.CustomerOib.Length == 11 &&
                      System.Text.RegularExpressions.Regex.IsMatch(invoice.CustomerOib, @"^\d{11}$");

            // ✅ PRODUCTION MODE: Require valid OIB, use test OIB only in development
            string effectiveCustomerOib;
            string effectiveCustomerName;

            if (hasCustomerOib)
            {
                // Use real customer OIB from invoice
                effectiveCustomerOib = invoice.CustomerOib!;
                effectiveCustomerName = invoice.CustomerName;
            }
            else if (company.MojeRacunEnvironment == "test")
            {
                // TEST MODE ONLY: Use A1 Hrvatska for testing
                _logger.LogWarning("⚠️ TEST MODE: Customer '{CustomerName}' has no valid OIB. Using A1 Hrvatska (OIB: 29524210204) for testing.",
                    invoice.CustomerName);
                effectiveCustomerOib = "29524210204"; // A1 Hrvatska d.o.o.
                effectiveCustomerName = "A1 HRVATSKA D.O.O.";
            }
            else
            {
                // ❌ PRODUCTION: Reject invoice without valid customer OIB
                var errorMessage =
                    $"Kupac '{invoice.CustomerName}' (Račun: {invoice.InvoiceNumber}) nema valjan OIB od 11 znamenki. " +
                    $"Svi kupci moraju imati valjan OIB za mojE-Račun fiskalizaciju u produkciji. " +
                    $"Trenutno okruženje: {company.MojeRacunEnvironment ?? "nije postavljeno"}. " +
                    $"Molimo ažurirajte OIB kupca prije slanja računa.";

                _logger.LogError("PRODUCTION MODE: Rejecting invoice without customer OIB. {Error}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // HR-BR-10: Buyer electronic address must be provided (cannot be empty)
            sb.AppendLine($"    <cbc:EndpointID schemeID=\"9934\">{effectiveCustomerOib}</cbc:EndpointID>");
            sb.AppendLine("    <cac:PartyIdentification>");
            sb.AppendLine($"    <cbc:ID>9934:{effectiveCustomerOib}</cbc:ID>");
            sb.AppendLine("    </cac:PartyIdentification>");

            // BT-44: Buyer name (REQUIRED by BR-07)
            sb.AppendLine("    <cac:PartyName>");
            sb.AppendLine($"    <cbc:Name>{XmlEscape(effectiveCustomerName)}</cbc:Name>");
            sb.AppendLine("    </cac:PartyName>");

            // BT-50 through BT-55: Buyer postal address
            sb.AppendLine("    <cac:PostalAddress>");

            // Use customer address or A1's address for testing
            var customerStreet = !string.IsNullOrWhiteSpace(invoice.CustomerAddress)
                ? XmlEscape(invoice.CustomerAddress)
                : "VRTNI PUT 1";
            var customerCity = !string.IsNullOrWhiteSpace(invoice.City)
                ? XmlEscape(invoice.City)
                : "ZAGREB";
            var customerPostalCode = !string.IsNullOrWhiteSpace(invoice.PostalCode)
                ? invoice.PostalCode
                : "10000";

            sb.AppendLine($"    <cbc:StreetName>{customerStreet}</cbc:StreetName>");
            sb.AppendLine($"    <cbc:CityName>{customerCity}</cbc:CityName>");
            sb.AppendLine($"    <cbc:PostalZone>{customerPostalCode}</cbc:PostalZone>");
            sb.AppendLine("     <cac:AddressLine>");
            sb.AppendLine($"     <cbc:Line>{customerStreet}, {customerCity}, {customerPostalCode}</cbc:Line>");
            sb.AppendLine("     </cac:AddressLine>");
            sb.AppendLine("     <cac:Country>");
            sb.AppendLine("     <cbc:IdentificationCode>HR</cbc:IdentificationCode>");
            sb.AppendLine("     </cac:Country>");
            sb.AppendLine("    </cac:PostalAddress>");

            // BT-48: Buyer VAT identifier (REQUIRED by HR-BR-S-01 when using Standard rate "S")
            // Always include for testing with A1's OIB
            sb.AppendLine("    <cac:PartyTaxScheme>");
            sb.AppendLine($"    <cbc:CompanyID>HR{effectiveCustomerOib}</cbc:CompanyID>");
            sb.AppendLine("     <cac:TaxScheme>");
            sb.AppendLine("     <cbc:ID>VAT</cbc:ID>");
            sb.AppendLine("     </cac:TaxScheme>");
            sb.AppendLine("    </cac:PartyTaxScheme>");
            sb.AppendLine("    <cac:PartyLegalEntity>");
            sb.AppendLine($"    <cbc:RegistrationName>{XmlEscape(effectiveCustomerName)}</cbc:RegistrationName>");
            sb.AppendLine($"    <cbc:CompanyID>{effectiveCustomerOib}</cbc:CompanyID>");
            sb.AppendLine("    </cac:PartyLegalEntity>");

            // Log warning if using test OIB
            if (!hasCustomerOib)
            {
                _logger.LogWarning("⚠️ TEST MODE: Customer '{OriginalName}' has no OIB. Using A1 Hrvatska (OIB: 29524210204) for testing.",
                    invoice.CustomerName);
            }

            sb.AppendLine("   </cac:Party>");
            sb.AppendLine("  </cac:AccountingCustomerParty>");

            // Delivery
            sb.AppendLine("  <cac:Delivery>");
            sb.AppendLine($"  <cbc:ActualDeliveryDate>{issueDate:yyyy-MM-dd}</cbc:ActualDeliveryDate>");
            sb.AppendLine("  </cac:Delivery>");
            
            // Payment means
            sb.AppendLine("  <cac:PaymentMeans>");
            sb.AppendLine("  <cbc:PaymentMeansCode>30</cbc:PaymentMeansCode>");
            sb.AppendLine("  <cbc:InstructionNote>Plaćanje po računu</cbc:InstructionNote>");
            sb.AppendLine("   <cac:PayeeFinancialAccount>");
            sb.AppendLine($"   <cbc:ID>{invoice.BankAccount}</cbc:ID>");
            sb.AppendLine("   </cac:PayeeFinancialAccount>");
            sb.AppendLine("  </cac:PaymentMeans>");
            
            // Tax Total
            var taxRate = invoice.VatAmount > 0 && invoice.SubTotal > 0 
                ? (invoice.VatAmount / invoice.SubTotal * 100) 
                : 5.00m;
                
            sb.AppendLine("  <cac:TaxTotal>");
            sb.AppendLine($"   <cbc:TaxAmount currencyID=\"EUR\">{invoice.VatAmount.ToString("F2", CultureInfo.InvariantCulture)}</cbc:TaxAmount>");
            sb.AppendLine("   <cac:TaxSubtotal>");
            sb.AppendLine($"    <cbc:TaxableAmount currencyID=\"EUR\">{invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)}</cbc:TaxableAmount>");
            sb.AppendLine($"    <cbc:TaxAmount currencyID=\"EUR\">{invoice.VatAmount.ToString("F2", CultureInfo.InvariantCulture)}</cbc:TaxAmount>");
            sb.AppendLine("    <cac:TaxCategory>");
            sb.AppendLine("    <cbc:ID>S</cbc:ID>");
            sb.AppendLine($"    <cbc:Percent>{taxRate.ToString("F0", CultureInfo.InvariantCulture)}</cbc:Percent>");
            sb.AppendLine("     <cac:TaxScheme>");
            sb.AppendLine("     <cbc:ID>VAT</cbc:ID>");
            sb.AppendLine("     </cac:TaxScheme>");
            sb.AppendLine("    </cac:TaxCategory>");
            sb.AppendLine("   </cac:TaxSubtotal>");
            sb.AppendLine("  </cac:TaxTotal>");
            
            // Legal Monetary Total
            sb.AppendLine("  <cac:LegalMonetaryTotal>");
            sb.AppendLine($"   <cbc:LineExtensionAmount currencyID=\"EUR\">{invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)}</cbc:LineExtensionAmount>");
            sb.AppendLine($"   <cbc:TaxExclusiveAmount currencyID=\"EUR\">{invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)}</cbc:TaxExclusiveAmount>");
            sb.AppendLine($"   <cbc:TaxInclusiveAmount currencyID=\"EUR\">{invoice.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}</cbc:TaxInclusiveAmount>");
            sb.AppendLine("   <cbc:AllowanceTotalAmount currencyID=\"EUR\">0.00</cbc:AllowanceTotalAmount>");
            sb.AppendLine("   <cbc:ChargeTotalAmount currencyID=\"EUR\">0.00</cbc:ChargeTotalAmount>");
            sb.AppendLine($"   <cbc:PayableAmount currencyID=\"EUR\">{invoice.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}</cbc:PayableAmount>");
            sb.AppendLine("  </cac:LegalMonetaryTotal>");
            
            // Invoice Lines
            int lineNumber = 1;
            foreach (var item in invoice.Items.OrderBy(i => i.ItemOrder))
            {
                sb.AppendLine("  <cac:InvoiceLine>");
                sb.AppendLine($"  <cbc:ID>{lineNumber++}</cbc:ID>");
                
                var unitCode = GetUnitCode(item.Unit);
                sb.AppendLine($"   <cbc:InvoicedQuantity unitCode=\"{unitCode}\">{item.Quantity.ToString("F3", CultureInfo.InvariantCulture)}</cbc:InvoicedQuantity>");
                sb.AppendLine($"   <cbc:LineExtensionAmount currencyID=\"EUR\">{item.Amount.ToString("F2", CultureInfo.InvariantCulture)}</cbc:LineExtensionAmount>");
                
                sb.AppendLine("   <cac:Item>");
                sb.AppendLine($"   <cbc:Name>{XmlEscape(item.Description)}</cbc:Name>");
                sb.AppendLine("    <cac:CommodityClassification>");
                
                var kpdCode = !string.IsNullOrWhiteSpace(item.KpdCode) ? item.KpdCode : "35.30.11";
                sb.AppendLine($"     <cbc:ItemClassificationCode listID=\"CG\">{kpdCode}</cbc:ItemClassificationCode>");
                sb.AppendLine("    </cac:CommodityClassification>");
                
                sb.AppendLine("    <cac:ClassifiedTaxCategory>");
                
                // FIXED: HR-BR-S-01 - Standard rate "S" requires buyer VAT ID
                // If customer has no OIB, we still use "S" but ensure PartyTaxScheme is present in customer section
                // The tax category from the item or use "S" as default
                var itemTaxRate = item.TaxRate > 0 ? item.TaxRate : 5.00m;
                var itemTaxCategory = item.TaxCategoryCode ?? "S";
                if (string.IsNullOrWhiteSpace(itemTaxCategory))
                    itemTaxCategory = "S";
                
                sb.AppendLine($"    <cbc:ID>{itemTaxCategory}</cbc:ID>");
                sb.AppendLine($"    <cbc:Percent>{itemTaxRate.ToString("F2", CultureInfo.InvariantCulture)}</cbc:Percent>");
                sb.AppendLine("     <cac:TaxScheme>");
                sb.AppendLine("     <cbc:ID>VAT</cbc:ID>");
                sb.AppendLine("     </cac:TaxScheme>");
                sb.AppendLine("    </cac:ClassifiedTaxCategory>");
                sb.AppendLine("   </cac:Item>");
                
                sb.AppendLine("   <cac:Price>");
                sb.AppendLine($"    <cbc:PriceAmount currencyID=\"EUR\">{item.UnitPrice.ToString("F2", CultureInfo.InvariantCulture)}</cbc:PriceAmount>");
                sb.AppendLine($"    <cbc:BaseQuantity unitCode=\"{unitCode}\">{item.Quantity.ToString("F3", CultureInfo.InvariantCulture)}</cbc:BaseQuantity>");
                sb.AppendLine("   </cac:Price>");
                
                sb.AppendLine("  </cac:InvoiceLine>");
            }
            
            sb.AppendLine(" </Invoice>");
            
            return sb.ToString();
        }

        /// Wraps UBL XML in OutgoingInvoicesData envelope as required by mojE-Račun API
        private string WrapInOutgoingInvoicesData(string ublXml)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<OutgoingInvoicesData>");
            sb.Append(ublXml.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "").Trim());
            sb.AppendLine("</OutgoingInvoicesData>");
            return sb.ToString();
        }

        public async Task<MojeRacunResponse> CheckInvoiceStatusAsync(string invoiceId, CompanyProfile company)
        {
            try
            {
                var baseUrl = company.MojeRacunEnvironment == "production"
                    ? MOJE_RACUN_PROD_BASE
                    : MOJE_RACUN_TEST_BASE;

                var statusUrl = $"{baseUrl}{STATUS_ENDPOINT}/{invoiceId}";

                var response = await _httpClient.GetAsync(statusUrl);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var statusResponse = JsonSerializer.Deserialize<MojeRacunApiResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return new MojeRacunResponse
                    {
                        Success = true,
                        Status = statusResponse?.StatusName?.ToLower() ?? "unknown",
                        Message = "Status retrieved successfully",
                        RawResponse = responseBody
                    };
                }
                else
                {
                    return new MojeRacunResponse
                    {
                        Success = false,
                        Message = $"Status check failed: {response.StatusCode}",
                        RawResponse = responseBody
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check status for invoice {InvoiceId}", invoiceId);
                return new MojeRacunResponse
                {
                    Success = false,
                    Message = $"Status check failed: {ex.Message}"
                };
            }
        }

        /// Query outbox to discover new statuses of sent documents
        /// Returns up to 10,000 results
        public async Task<List<OutboxInvoiceHeader>> QueryOutboxAsync(CompanyProfile company, OutboxQueryFilter? filter = null)
        {
            try
            {
                _logger.LogInformation("Querying mojE-Račun outbox for company {CompanyId}", company.Id);

                var baseUrl = company.MojeRacunEnvironment == "production"
                    ? MOJE_RACUN_PROD_BASE
                    : MOJE_RACUN_TEST_BASE;

                var queryUrl = $"{baseUrl}/queryOutbox";

                // Build request payload
                var requestPayload = new
                {
                    Username = company.MojeRacunClientId,
                    Password = company.MojeRacunClientSecret,
                    CompanyId = company.Oib,
                    CompanyBu = string.Empty,
                    SoftwareId = company.MojeRacunApiKey,
                    ElectronicId = filter?.ElectronicId,
                    StatusId = filter?.StatusId,
                    InvoiceYear = filter?.InvoiceYear,
                    InvoiceNumber = filter?.InvoiceNumber,
                    From = filter?.From?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    To = filter?.To?.ToString("yyyy-MM-ddTHH:mm:ss")
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending queryOutbox request to {Url}", queryUrl);

                var response = await _httpClient.PostAsync(queryUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("queryOutbox failed: {StatusCode} - {Response}",
                        response.StatusCode, responseBody);
                    return new List<OutboxInvoiceHeader>();
                }

                // Parse XML response
                var invoiceHeaders = ParseOutboxXmlResponse(responseBody);

                _logger.LogInformation("Retrieved {Count} invoice headers from outbox", invoiceHeaders.Count);

                return invoiceHeaders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query outbox");
                return new List<OutboxInvoiceHeader>();
            }
        }

        /// <summary>
        /// Parse XML response from queryOutbox endpoint
        /// </summary>
        private List<OutboxInvoiceHeader> ParseOutboxXmlResponse(string xmlResponse)
        {
            var headers = new List<OutboxInvoiceHeader>();

            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xmlResponse);

                var headerNodes = doc.SelectNodes("//InboxDocumentHeader");
                
                if (headerNodes == null || headerNodes.Count == 0)
                {
                    _logger.LogWarning("No InboxDocumentHeader nodes found in XML response");
                    return headers;
                }

                foreach (System.Xml.XmlNode node in headerNodes)
                {
                    var header = new OutboxInvoiceHeader
                    {
                        ElectronicId = GetXmlNodeValue(node, "ElectronicId"),
                        DocumentNr = GetXmlNodeValue(node, "DocumentNr"),
                        DocumentTypeId = int.TryParse(GetXmlNodeValue(node, "DocumentTypeId"), out var typeId) ? typeId : 0,
                        DocumentTypeName = GetXmlNodeValue(node, "DocumentTypeName"),
                        StatusId = int.TryParse(GetXmlNodeValue(node, "StatusId"), out var statusId) ? statusId : 0,
                        StatusName = GetXmlNodeValue(node, "StatusName"),
                        RecipientBusinessNumber = GetXmlNodeValue(node, "RecipientBusinessNumber"),
                        RecipientBusinessUnit = GetXmlNodeValue(node, "RecipientBusinessUnit"),
                        RecipientBusinessName = GetXmlNodeValue(node, "RecipientBusinessName"),
                        Created = ParseXmlDateTime(GetXmlNodeValue(node, "Created")),
                        Updated = ParseXmlDateTime(GetXmlNodeValue(node, "Updated")),
                        Sent = ParseXmlDateTime(GetXmlNodeValue(node, "Sent")),
                        Delivered = ParseXmlDateTime(GetXmlNodeValue(node, "Delivered"))
                    };

                    headers.Add(header);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing outbox XML response");
            }

            return headers;
        }

        private string GetXmlNodeValue(System.Xml.XmlNode parentNode, string nodeName)
        {
            var node = parentNode.SelectSingleNode(nodeName);
            return node?.InnerText ?? string.Empty;
        }

        private DateTime? ParseXmlDateTime(string dateTimeString)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
                return null;

            if (DateTime.TryParse(dateTimeString, out var result))
                return result;

            return null;
        }

        private string GetUnitCode(string unit)
        {
            return unit.ToLowerInvariant() switch
            {
                "kom" => "EA",
                "kg" => "KGM",
                "m²" => "MTK",
                "m3" => "MTQ",
                "lit" => "LTR",
                "kwh" => "KWH",
                _ => "EA"
            };
        }

        private string XmlEscape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private void SaveXmlForDebug(string invoiceNumber, string xmlContent, string type)
        {
            try
            {
                // Sanitize invoice number for filesystem
                var safeInvoiceNumber = invoiceNumber
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace(":", "_")
                    .Replace("*", "_")
                    .Replace("?", "_")
                    .Replace("\"", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace("|", "_");

                // Create organized folder structure: FiscalizationDebug/MojeRacun/{InvoiceNumber}/
                var rootPath = _environment.WebRootPath ?? _environment.ContentRootPath ?? Directory.GetCurrentDirectory();
                var invoiceFolder = Path.Combine(rootPath, "FiscalizationDebug", "MojeRacun", safeInvoiceNumber);

                if (!Directory.Exists(invoiceFolder))
                {
                    Directory.CreateDirectory(invoiceFolder);
                    _logger.LogInformation("Created mojE-Račun debug folder: {InvoiceFolder}", invoiceFolder);
                }

                var fileName = "01-Request-UBL.xml";
                var filePath = Path.Combine(invoiceFolder, fileName);
                File.WriteAllText(filePath, xmlContent, Encoding.UTF8);

                // Save metadata file with timestamp and details
                var metadataPath = Path.Combine(invoiceFolder, "metadata.json");
                var metadata = new
                {
                    InvoiceNumber = invoiceNumber,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Type = "mojE-Račun Fiscalization",
                    Files = new[]
                    {
                        new { Order = 1, File = "01-Request-UBL.xml", Description = "UBL 2.1 XML invoice sent to mojE-Račun" },
                        new { Order = 2, File = "02-Response-{StatusCode}.json", Description = "JSON response from mojE-Račun API" }
                    }
                };
                
                File.WriteAllText(metadataPath, 
                    System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), 
                    Encoding.UTF8);

                _logger.LogInformation("✅ mojE-Račun UBL XML saved: {InvoiceNumber}/{FileName}", safeInvoiceNumber, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save mojE-Račun debug XML");
            }
        }

        public async Task<string> DownloadInvoiceXmlAsync(long electronicId, CompanyProfile company)
        {
            try
            {
                var baseUrl = company.MojeRacunEnvironment == "production"
                    ? MOJE_RACUN_PROD_BASE
                    : MOJE_RACUN_TEST_BASE;

                var receiveUrl = $"{baseUrl}{RECEIVE_ENDPOINT}";

                var requestPayload = new
                {
                    Username = company.MojeRacunClientId,
                    Password = company.MojeRacunClientSecret,
                    CompanyId = company.Oib,
                    CompanyBu = string.Empty,
                    SoftwareId = company.MojeRacunApiKey,
                    ElectronicId = electronicId
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Downloading invoice XML for ElectronicId: {ElectronicId}", electronicId);

                var response = await _httpClient.PostAsync(receiveUrl, content);
                var xmlContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully downloaded invoice XML (ElectronicId: {ElectronicId})", electronicId);
                    return xmlContent;
                }
                else
                {
                    _logger.LogError("Failed to download invoice XML: {StatusCode} - {Response}",
                        response.StatusCode, xmlContent);
                    throw new InvalidOperationException($"Failed to download invoice: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading invoice XML for ElectronicId: {ElectronicId}", electronicId);
                throw;
            }
        }
    }

    // Helper classes for mojE-Račun API responses
    internal class MojeRacunPingResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal class MojeRacunApiResponse
    {
        public long ElectronicId { get; set; }
        public string? DocumentNr { get; set; }
        public int DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? RecipientBusinessNumber { get; set; }
        public string? RecipientBusinessUnit { get; set; }
        public string? RecipientBusinessName { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Sent { get; set; }
        public DateTime? Modified { get; set; }
        public DateTime? Delivered { get; set; }
    }
}
