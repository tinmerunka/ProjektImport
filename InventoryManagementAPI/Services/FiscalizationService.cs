using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace InventoryManagementAPI.Services
{
    public interface IFiscalizationService
    {
        Task<FiscalizationResponse> FiscalizeInvoiceAsync(Invoice invoice, CompanyProfile company);
        string GenerateZki(Invoice invoice, CompanyProfile company);
    }

    public class FiscalizationService : IFiscalizationService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FiscalizationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        private const string FINA_TEST_ENDPOINT = "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest";
        private const string FINA_NAMESPACE = "http://www.apis-it.hr/fin/2012/types/f73";
        private const string SOAP_NAMESPACE = "http://schemas.xmlsoap.org/soap/envelope/";

        public FiscalizationService(
            ILogger<FiscalizationService> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
            _httpClient = CreateFinaHttpClient();
        }

        private HttpClient CreateFinaHttpClient()
        {
            var handler = new HttpClientHandler();
            if (_environment.IsDevelopment())
            {
                handler.ServerCertificateCustomValidationCallback = (req, cert, chain, errs) => true;
            }
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Croatian-Fiscalization-Client/1.0");
            return client;
        }

        public async Task<FiscalizationResponse> FiscalizeInvoiceAsync(Invoice invoice, CompanyProfile company)
        {
            try
            {
                if (company == null)
                    throw new ArgumentNullException(nameof(company), "Company profile is required for fiscalization");

                var certificate = LoadFinaCertificate(company);
                var certificateOib = ExtractOibFromFinaCertificate(certificate, company);

                var zki = GenerateFinaZki(invoice, certificateOib, company);
                var finaXml = CreateFinaXmlRequest(invoice, company, certificateOib, zki);
                var signedXml = SignForFina(finaXml, certificate);

                var response = await SendToFinaAsync(signedXml, invoice, company);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiscalization failed for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                return new FiscalizationResponse
                {
                    Success = false,
                    Message = $"Fiscalization failed: {ex.Message}",
                    Zki = company != null ? GenerateZki(invoice, company) : null
                };
            }
        }

        public string GenerateZki(Invoice invoice, CompanyProfile company)
        {
            if (company == null)
                throw new ArgumentNullException(nameof(company), "Company profile is required for ZKI generation");

            var certificate = LoadFinaCertificate(company);
            var oib = ExtractOibFromFinaCertificate(certificate, company);
            return GenerateFinaZki(invoice, oib, company);
        }

        private X509Certificate2 LoadFinaCertificate(CompanyProfile company)
        {
            if (company == null)
                throw new ArgumentNullException(nameof(company));

            if (string.IsNullOrEmpty(company.FiscalizationCertificatePath))
                throw new InvalidOperationException("Certificate path not configured in company profile");

            var fullPath = Path.Combine(_environment.WebRootPath, company.FiscalizationCertificatePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Certificate not found: {fullPath}");

            _logger.LogInformation("Attempting to load certificate from: {Path}", fullPath);

            // Use empty string if password is null
            var password = company.FiscalizationCertificatePassword ?? string.Empty;

            _logger.LogInformation("Using password: {HasPassword}", !string.IsNullOrEmpty(password) ? "Yes" : "No (empty)");

            try
            {
                var cert = new X509Certificate2(fullPath, password, X509KeyStorageFlags.Exportable);

                if (!cert.HasPrivateKey)
                    throw new InvalidOperationException("Certificate does not have a private key!");

                _logger.LogInformation("Certificate loaded successfully: Subject={Subject}, HasPrivateKey={HasPrivateKey}, NotAfter={NotAfter}",
                    cert.Subject, cert.HasPrivateKey, cert.NotAfter);

                return cert;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Failed to load certificate. Password might be incorrect or certificate might be corrupted.");
                _logger.LogError("Certificate path: {Path}", fullPath);
                _logger.LogError("Password provided: {HasPassword}", !string.IsNullOrEmpty(password));

                throw new InvalidOperationException($"Failed to load certificate. The password might be incorrect or the certificate might be corrupted. Original error: {ex.Message}", ex);
            }
        }

        private string ExtractOibFromFinaCertificate(X509Certificate2 certificate, CompanyProfile company)
        {
            // Log certificate subject za debugging
            _logger.LogInformation("Certificate Subject: {Subject}", certificate.Subject);

            // Također logiraj certificate Organization da vidimo sve OIB-ove
            var subjectParts = certificate.Subject.Split(',');
            foreach (var part in subjectParts)
            {
                _logger.LogInformation("Certificate Subject Part: {Part}", part.Trim());
            }

            // Prioritet: Company.FiscalizationOib > Config > Certifikat
            if (company != null && !string.IsNullOrEmpty(company.FiscalizationOib))
            {
                _logger.LogInformation("Using issuer OIB from company profile: {Oib}", company.FiscalizationOib);
                return company.FiscalizationOib;
            }

            var configOib = _configuration["Fiscalization:IssuerOib"];
            if (!string.IsNullOrEmpty(configOib))
            {
                _logger.LogInformation("Using issuer OIB from config: {Oib}", configOib);

                // VAŽNO: Provjeri da li config OIB odgovara certifikatu
                if (IsOibInCertificate(certificate, configOib))
                {
                    _logger.LogInformation("Config OIB {Oib} matches certificate", configOib);
                    return configOib;
                }
                else
                {
                    _logger.LogWarning("Config OIB {Oib} does NOT match certificate! Will try to extract from certificate.", configOib);
                }
            }

            // Pokušaj izvući iz Subject-a certifikata
            var subject = certificate.Subject;

            // UVIJEK prvo pokušaj izvući iz certifikata (najsigurniji način)
            // 1. HR12345678901
            var match = System.Text.RegularExpressions.Regex.Match(subject, @"HR(\d{11})");
            if (match.Success)
            {
                var oib = match.Groups[1].Value;
                _logger.LogInformation("Extracted issuer OIB from certificate (HR format): {Oib}", oib);
                return oib;
            }

            // 2. OID.2.5.4.97=VATHR-12345678901 (European VAT format)
            match = System.Text.RegularExpressions.Regex.Match(subject, @"VATHR-(\d{11})");
            if (match.Success)
            {
                var oib = match.Groups[1].Value;
                _logger.LogInformation("Extracted issuer OIB from certificate (VATHR format): {Oib}", oib);
                return oib;
            }

            // 3. Samo brojevi u O= (Organization)
            match = System.Text.RegularExpressions.Regex.Match(subject, @"O=.*?(\d{11})");
            if (match.Success)
            {
                var oib = match.Groups[1].Value;
                _logger.LogInformation("Extracted issuer OIB from certificate (O= format): {Oib}", oib);
                return oib;
            }

            // FALLBACK: Company profile
            if (company != null && !string.IsNullOrEmpty(company.FiscalizationOib))
            {
                _logger.LogWarning("Could not extract OIB from certificate or config, using company OIB: {Oib}", company.FiscalizationOib);
                return company.FiscalizationOib;
            }
            _logger.LogError("Could not extract OIB from certificate! Subject: {Subject}", subject);
            throw new InvalidOperationException($"Could not extract OIB from certificate. Subject: {subject}");
        }

        // Dodaj helper metodu za provjeru
        private bool IsOibInCertificate(X509Certificate2 certificate, string oibToCheck)
        {
            var subject = certificate.Subject;

            // Provjeri sve moguće formate
            var patterns = new[]
            {
        $@"HR{oibToCheck}",
        $@"VATHR-{oibToCheck}",
        $@"O=.*?{oibToCheck}"
    };

            foreach (var pattern in patterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(subject, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private string ExtractInvoiceNumber(string fullInvoiceNumber)
        {
            if (string.IsNullOrEmpty(fullInvoiceNumber))
                return "1";

            if (fullInvoiceNumber.Contains('/'))
            {
                var parts = fullInvoiceNumber.Split('/');
                return parts[0].Trim();
            }

            if (fullInvoiceNumber.Contains('-'))
            {
                var parts = fullInvoiceNumber.Split('-');
                return parts[0].Trim();
            }

            return fullInvoiceNumber.Trim();
        }

        private string GenerateFinaZki(Invoice invoice, string oib, CompanyProfile company)
        {
            var datumVrijeme = invoice.IssueDate.ToString("dd.MM.yyyyTHH:mm:ss");
            var iznosUkupno = invoice.TotalAmount.ToString("F2", CultureInfo.InvariantCulture);
            var oznPosPr = company != null && !string.IsNullOrEmpty(company.InvoiceParam1) ? company.InvoiceParam1 : "01";
            var oznNapUr = company != null && !string.IsNullOrEmpty(company.InvoiceParam2) ? company.InvoiceParam2 : "1";
            var brOznRac = ExtractInvoiceNumber(invoice.InvoiceNumber);

            var zkiString = $"{oib}{datumVrijeme}{brOznRac}{oznPosPr}{oznNapUr}{iznosUkupno}";

            _logger.LogDebug("ZKI Input String: {ZkiString}", zkiString);

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(zkiString));
            var zki = Convert.ToHexString(hash).ToLowerInvariant();

            _logger.LogDebug("Generated ZKI: {Zki}", zki);
            return zki;
        }

        private string CreateFinaXmlRequest(Invoice invoice, CompanyProfile company, string oib, string zki)
        {
            var elementId = "signXmlId";
            var currentDateTime = DateTime.Now.ToString("dd.MM.yyyyTHH:mm:ss");
            var issueDateTime = invoice.IssueDate.ToString("dd.MM.yyyyTHH:mm:ss");

            var oznPosPr = company != null && !string.IsNullOrEmpty(company.InvoiceParam1) ? company.InvoiceParam1 : "01";
            var oznNapUr = company != null && !string.IsNullOrEmpty(company.InvoiceParam2) ? company.InvoiceParam2 : "1";
            var brOznRac = ExtractInvoiceNumber(invoice.InvoiceNumber);
            var oznSlijed = "P";

            // OibOper iz company profila, fallback na config
            var oibOper = company?.FiscalizationOperatorOib
                ?? _configuration["Fiscalization:OperatorOib"]
                ?? oib; // Ako ništa, koristi OIB izdavatelja

            var inPdv = company?.InPDV ?? false;

            // Generiraj PDV strukturu - grupira stavke po stopama PDV-a
            var pdvXml = GeneratePdvXml(invoice);

            var xml = $@"<tns:RacunZahtjev xmlns:tns=""{FINA_NAMESPACE}"" Id=""{elementId}"">
  <tns:Zaglavlje>
    <tns:IdPoruke>{Guid.NewGuid()}</tns:IdPoruke>
    <tns:DatumVrijeme>{currentDateTime}</tns:DatumVrijeme>
  </tns:Zaglavlje>
  <tns:Racun>
    <tns:Oib>{oib}</tns:Oib>
    <tns:USustPdv>{inPdv.ToString().ToLower()}</tns:USustPdv>
    <tns:DatVrijeme>{issueDateTime}</tns:DatVrijeme>
    <tns:OznSlijed>{oznSlijed}</tns:OznSlijed>
    <tns:BrRac>
      <tns:BrOznRac>{brOznRac}</tns:BrOznRac>
      <tns:OznPosPr>{oznPosPr}</tns:OznPosPr>
      <tns:OznNapUr>{oznNapUr}</tns:OznNapUr>
    </tns:BrRac>
{pdvXml}
    <tns:IznosUkupno>{invoice.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)}</tns:IznosUkupno>
    <tns:NacinPlac>{invoice.PaymentMethodCode ?? "G"}</tns:NacinPlac>
    <tns:OibOper>{oibOper}</tns:OibOper>
    <tns:ZastKod>{zki}</tns:ZastKod>
    <tns:NakDost>false</tns:NakDost>
  </tns:Racun>
</tns:RacunZahtjev>";

            _logger.LogDebug("Created FINA XML for invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return xml;
        }

        /// <summary>
        /// Generira PDV XML strukturu - grupira stavke računa po stopama PDV-a
        /// </summary>
        private string GeneratePdvXml(Invoice invoice)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    <tns:Pdv>");

            // Ako invoice nema Items (stari način), koristi invoice-level PDV podatke
            if (invoice.Items == null || !invoice.Items.Any())
            {
                _logger.LogWarning("Invoice {InvoiceNumber} has no items, using invoice-level tax data",
                    invoice.InvoiceNumber);

                sb.AppendLine("      <tns:Porez>");
                sb.AppendLine($"        <tns:Stopa>{invoice.TaxRate.ToString("F2", CultureInfo.InvariantCulture)}</tns:Stopa>");
                sb.AppendLine($"        <tns:Osnovica>{invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)}</tns:Osnovica>");
                sb.AppendLine($"        <tns:Iznos>{invoice.TaxAmount.ToString("F2", CultureInfo.InvariantCulture)}</tns:Iznos>");
                sb.AppendLine("      </tns:Porez>");
            }
            else
            {
                // Grupiraj stavke po stopama PDV-a
                var taxGroups = invoice.Items
                    .GroupBy(item => item.TaxRate)
                    .OrderByDescending(g => g.Key) // Poredaj po stopi (najviša prva)
                    .ToList();

                _logger.LogDebug("Invoice {InvoiceNumber} has {GroupCount} different tax rates",
                    invoice.InvoiceNumber, taxGroups.Count);

                foreach (var taxGroup in taxGroups)
                {
                    var taxRate = taxGroup.Key;

                    // Izračunaj osnovicu i PDV za ovu grupu
                    var taxAmount = taxGroup.Sum(item => item.LineTaxAmount);
                    var subTotal = taxGroup.Sum(item => item.LineTotal - item.LineTaxAmount);

                    sb.AppendLine("      <tns:Porez>");
                    sb.AppendLine($"        <tns:Stopa>{taxRate.ToString("F2", CultureInfo.InvariantCulture)}</tns:Stopa>");
                    sb.AppendLine($"        <tns:Osnovica>{subTotal.ToString("F2", CultureInfo.InvariantCulture)}</tns:Osnovica>");
                    sb.AppendLine($"        <tns:Iznos>{taxAmount.ToString("F2", CultureInfo.InvariantCulture)}</tns:Iznos>");
                    sb.AppendLine("      </tns:Porez>");

                    _logger.LogDebug("Tax group: Rate={TaxRate}%, Base={SubTotal}, Tax={TaxAmount}",
                        taxRate, subTotal, taxAmount);
                }
            }

            sb.Append("    </tns:Pdv>");
            return sb.ToString();
        }

        private string SignForFina(string xmlContent, X509Certificate2 certificate)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(xmlContent);

            var elementId = xmlDoc.DocumentElement?.GetAttribute("Id");
            if (string.IsNullOrEmpty(elementId))
                throw new InvalidOperationException("Missing Id attribute for signature");

            var signedXml = new SignedXml(xmlDoc)
            {
                SigningKey = certificate.GetRSAPrivateKey()
            };

            signedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/2001/10/xml-exc-c14n#";
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

            var reference = new Reference($"#{elementId}");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            var excC14nTransform = new XmlDsigExcC14NTransform();
            reference.AddTransform(excC14nTransform);
            reference.DigestMethod = SignedXml.XmlDsigSHA1Url;

            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();
            var x509Data = new KeyInfoX509Data(certificate);
            keyInfo.AddClause(x509Data);
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();
            var signatureElement = signedXml.GetXml();
            xmlDoc.DocumentElement?.AppendChild(xmlDoc.ImportNode(signatureElement, true));

            _logger.LogDebug("XML signed successfully with Exclusive C14N");
            return xmlDoc.OuterXml;
        }

        private void SaveXmlForDebug(string invoiceNumber, string xmlContent, bool isSoap = false)
        {
            try
            {
                var type = isSoap ? "SOAP" : "SIGNED";

                // Sanitiziraj invoice number - ukloni nedozvoljene karaktere za filename
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

                var fileName = $"Fina_{type}_{safeInvoiceNumber}_{DateTime.Now:yyyyMMddHHmmssfff}.xml";

                // Koristi ContentRootPath kao fallback ili kreiraj u projektu
                var rootPath = _environment.WebRootPath ?? _environment.ContentRootPath ?? Directory.GetCurrentDirectory();
                var debugFolder = Path.Combine(rootPath, "FiscalizationDebug");

                _logger.LogInformation("Attempting to save XML to: {DebugFolder}", debugFolder);

                if (!Directory.Exists(debugFolder))
                {
                    Directory.CreateDirectory(debugFolder);
                    _logger.LogInformation("Created debug folder: {DebugFolder}", debugFolder);
                }

                var filePath = Path.Combine(debugFolder, fileName);
                File.WriteAllText(filePath, xmlContent, Encoding.UTF8);

                _logger.LogInformation("[DEBUG] {Type} XML for invoice {InvoiceNumber} saved: {FilePath}",
                    type, safeInvoiceNumber, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save debug XML for invoice {InvoiceNumber}. Folder: {WebRootPath}",
                    invoiceNumber, _environment.WebRootPath ?? "NULL");
            }
        }

        private async Task<FiscalizationResponse> SendToFinaAsync(string signedXml, Invoice invoice, CompanyProfile company)
        {
            try
            {
                var endpoint = FINA_TEST_ENDPOINT;

                var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""{SOAP_NAMESPACE}"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <soapenv:Body>
    {signedXml}
  </soapenv:Body>
</soapenv:Envelope>";

                SaveXmlForDebug(invoice.InvoiceNumber, signedXml, isSoap: false);
                SaveXmlForDebug(invoice.InvoiceNumber, soapEnvelope, isSoap: true);

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "text/xml; charset=utf-8");
                content.Headers.Add("SOAPAction", "\"\"");

                _logger.LogInformation("Sending fiscalization request to FINA for invoice {InvoiceNumber}",
                    invoice.InvoiceNumber);

                var httpResponse = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("FINA HTTP Status: {StatusCode}", httpResponse.StatusCode);

                SaveXmlForDebug($"{invoice.InvoiceNumber}_RESPONSE_{httpResponse.StatusCode}", responseContent, isSoap: false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("FINA HTTP {StatusCode} Response: {Response}",
                        httpResponse.StatusCode, responseContent);

                    return new FiscalizationResponse
                    {
                        Success = false,
                        Message = $"FINA returned HTTP {httpResponse.StatusCode}",
                        RawResponse = responseContent
                    };
                }

                _logger.LogDebug("FINA Response: {Response}", responseContent);

                var doc = new XmlDocument();
                doc.LoadXml(responseContent);

                var nsManager = new XmlNamespaceManager(doc.NameTable);
                nsManager.AddNamespace("soap", SOAP_NAMESPACE);
                nsManager.AddNamespace("soapenv", SOAP_NAMESPACE);
                nsManager.AddNamespace("tns", FINA_NAMESPACE);

                var faultNode = doc.SelectSingleNode("//soap:Fault | //soapenv:Fault", nsManager);
                if (faultNode != null)
                {
                    var faultString = faultNode.SelectSingleNode("faultstring")?.InnerText ?? "Unknown SOAP fault";
                    var faultCode = faultNode.SelectSingleNode("faultcode")?.InnerText ?? "Unknown";

                    _logger.LogError("FINA SOAP Fault: {FaultCode} - {FaultString}", faultCode, faultString);

                    return new FiscalizationResponse
                    {
                        Success = false,
                        Message = $"FINA error: {faultString}",
                        RawResponse = responseContent
                    };
                }

                var greskaNode = doc.SelectSingleNode("//tns:Greske/tns:Greska", nsManager);
                if (greskaNode != null)
                {
                    var sifraGreske = greskaNode.SelectSingleNode("tns:SifraGreske", nsManager)?.InnerText;
                    var porukaGreske = greskaNode.SelectSingleNode("tns:PorukaGreske", nsManager)?.InnerText;

                    _logger.LogError("FINA Error: {ErrorCode} - {ErrorMessage}", sifraGreske, porukaGreske);

                    return new FiscalizationResponse
                    {
                        Success = false,
                        Message = $"FINA greška [{sifraGreske}]: {porukaGreske}",
                        RawResponse = responseContent
                    };
                }

                var jirNode = doc.SelectSingleNode("//tns:Jir", nsManager);
                if (jirNode != null && !string.IsNullOrEmpty(jirNode.InnerText))
                {
                    _logger.LogInformation("Fiscalization successful for invoice {InvoiceNumber}, JIR: {Jir}",
                        invoice.InvoiceNumber, jirNode.InnerText);

                    return new FiscalizationResponse
                    {
                        Success = true,
                        Message = "Fiscalization successful",
                        Jir = jirNode.InnerText,
                        FiscalizedAt = DateTime.UtcNow,
                        Zki = GenerateZki(invoice, company)
                    };
                }

                _logger.LogWarning("FINA response contains no JIR or error for invoice {InvoiceNumber}",
                    invoice.InvoiceNumber);

                return new FiscalizationResponse
                {
                    Success = false,
                    Message = "Invalid response from FINA - no JIR received",
                    RawResponse = responseContent
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while sending to FINA for invoice {InvoiceNumber}",
                    invoice.InvoiceNumber);
                return new FiscalizationResponse
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "XML parsing error for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                return new FiscalizationResponse
                {
                    Success = false,
                    Message = $"Invalid XML response: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fiscalizing invoice {InvoiceNumber}",
                    invoice.InvoiceNumber);
                return new FiscalizationResponse
                {
                    Success = false,
                    Message = $"Error sending to FINA: {ex.Message}"
                };
            }
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}