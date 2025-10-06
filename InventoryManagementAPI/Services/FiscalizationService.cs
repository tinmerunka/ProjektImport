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
                var certificate = LoadFinaCertificate();
                var certificateOib = ExtractOibFromFinaCertificate(certificate);

                var zki = GenerateFinaZki(invoice, certificateOib, company);
                var finaXml = CreateFinaXmlRequest(invoice, company, certificateOib, zki);
                var signedXml = SignForFina(finaXml, certificate);

                var response = await SendToFinaAsync(signedXml, invoice);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiscalization failed for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                return new FiscalizationResponse
                {
                    Success = false,
                    Message = $"Fiscalization failed: {ex.Message}",
                    Zki = GenerateZki(invoice, company)
                };
            }
        }

        public string GenerateZki(Invoice invoice, CompanyProfile company)
        {
            var certificate = LoadFinaCertificate();
            var oib = ExtractOibFromFinaCertificate(certificate);
            return GenerateFinaZki(invoice, oib, company);
        }

        private X509Certificate2 LoadFinaCertificate()
        {
            var certPath = _configuration["Fiscalization:CertificatePath"] ?? "testcert.pfx";
            var certPassword = _configuration["Fiscalization:CertificatePassword"] ?? "1234";
            string fullPath = Path.IsPathRooted(certPath) ? certPath : Path.Combine(_environment.WebRootPath, certPath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Certificate not found: {fullPath}");

            var cert = new X509Certificate2(fullPath, certPassword, X509KeyStorageFlags.Exportable);

            if (!cert.HasPrivateKey)
                throw new InvalidOperationException("Certificate does not have a private key!");

            _logger.LogInformation("Certificate loaded: Subject={Subject}, HasPrivateKey={HasPrivateKey}, NotAfter={NotAfter}",
                cert.Subject, cert.HasPrivateKey, cert.NotAfter);

            return cert;
        }

        private string ExtractOibFromFinaCertificate(X509Certificate2 certificate)
        {
            var configOib = _configuration["Fiscalization:IssuerOib"];
            if (!string.IsNullOrEmpty(configOib))
            {
                _logger.LogInformation("Using issuer OIB from config: {Oib}", configOib);
                return configOib;
            }

            var subject = certificate.Subject;
            var match = System.Text.RegularExpressions.Regex.Match(subject, @"HR(\d{11})");
            if (match.Success)
            {
                _logger.LogInformation("Extracted issuer OIB from certificate: {Oib}", match.Groups[1].Value);
                return match.Groups[1].Value;
            }

            var fallbackOib = "79830143058";
            _logger.LogWarning("Could not extract OIB from certificate, using fallback: {Oib}", fallbackOib);
            return fallbackOib;
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
            var oznPosPr = !string.IsNullOrEmpty(company.InvoiceParam1) ? company.InvoiceParam1 : "01";
            var oznNapUr = !string.IsNullOrEmpty(company.InvoiceParam2) ? company.InvoiceParam2 : "1";
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

            var oznPosPr = !string.IsNullOrEmpty(company.InvoiceParam1) ? company.InvoiceParam1 : "01";
            var oznNapUr = !string.IsNullOrEmpty(company.InvoiceParam2) ? company.InvoiceParam2 : "1";
            var brOznRac = ExtractInvoiceNumber(invoice.InvoiceNumber);
            var oznSlijed = "P";
            var oibOper = _configuration["Fiscalization:OperatorOib"] ?? "79830143058";

            var pdvXml = new StringBuilder();
            pdvXml.AppendLine($"    <tns:Pdv>");
            pdvXml.AppendLine($"      <tns:Porez>");
            pdvXml.AppendLine($"        <tns:Stopa>{invoice.TaxRate.ToString("F2", CultureInfo.InvariantCulture)}</tns:Stopa>");
            pdvXml.AppendLine($"        <tns:Osnovica>{invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)}</tns:Osnovica>");
            pdvXml.AppendLine($"        <tns:Iznos>{invoice.TaxAmount.ToString("F2", CultureInfo.InvariantCulture)}</tns:Iznos>");
            pdvXml.AppendLine($"      </tns:Porez>");
            pdvXml.Append($"    </tns:Pdv>");

            var defaultCustomerOib = _configuration["Fiscalization:DefaultCustomerOib"] ?? "";
            var customerOib = !string.IsNullOrEmpty(invoice.CustomerOib)
                ? invoice.CustomerOib
                : defaultCustomerOib;

            var oibKupcaXml = !string.IsNullOrEmpty(customerOib)
                ? $"<tns:OibKupca>{customerOib}</tns:OibKupca>"
                : "";

            var xml = $@"<tns:RacunZahtjev xmlns:tns=""{FINA_NAMESPACE}"" Id=""{elementId}"">
  <tns:Zaglavlje>
    <tns:IdPoruke>{Guid.NewGuid()}</tns:IdPoruke>
    <tns:DatumVrijeme>{currentDateTime}</tns:DatumVrijeme>
  </tns:Zaglavlje>
  <tns:Racun>
    <tns:Oib>{oib}</tns:Oib>
    <tns:USustPdv>{company.InPDV.ToString().ToLower()}</tns:USustPdv>
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
    {oibKupcaXml}
    <tns:ZastKod>{zki}</tns:ZastKod>
    <tns:NakDost>false</tns:NakDost>
  </tns:Racun>
</tns:RacunZahtjev>";

            _logger.LogDebug("Created FINA XML for invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return xml;
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
                var fileName = $"Fina_{type}_{invoiceNumber}_{DateTime.Now:yyyyMMddHHmmssfff}.xml";
                var debugFolder = Path.Combine(_environment.WebRootPath ?? ".", "FiscalizationDebug");

                if (!Directory.Exists(debugFolder))
                    Directory.CreateDirectory(debugFolder);

                var filePath = Path.Combine(debugFolder, fileName);
                File.WriteAllText(filePath, xmlContent, Encoding.UTF8);

                _logger.LogInformation("[DEBUG] {Type} XML for invoice {InvoiceNumber} saved: {FilePath}",
                    type, invoiceNumber, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save debug XML for invoice {InvoiceNumber}", invoiceNumber);
            }
        }

        private async Task<FiscalizationResponse> SendToFinaAsync(string signedXml, Invoice invoice)
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
                        Zki = GenerateZki(invoice, null)
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