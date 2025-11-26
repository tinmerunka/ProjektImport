namespace InventoryManagementAPI.Services
{
    /// <summary>
    /// Service for managing KPD (Klasifikacija proizvoda i usluga) codes for Croatian fiscalization
    /// KPD codes are required for mojE-Raèun (UBL) invoices
    /// </summary>
    public interface IKpdCodeService
    {
        string GetKpdCodeForService(string description);
        string GetTaxCategoryCode(decimal taxRate);
        decimal GetDefaultTaxRateForService(string description);
    }

    public class KpdCodeService : IKpdCodeService
    {
        private readonly ILogger<KpdCodeService> _logger;

        // Standard KPD codes for utility services in Croatia
        // ? UPDATED: Default VAT rate changed to 5% for utility services
        private static readonly Dictionary<string, (string KpdCode, decimal DefaultTaxRate)> ServiceKpdMap = new()
        {
            // Heating and hot water - 5% PDV (snižena stopa za komunalne usluge)
            { "grijanje", ("35.30.11", 5.00m) },
            { "toplinska", ("35.30.11", 5.00m) },
            { "topla voda", ("35.30.11", 5.00m) },
            { "grijanje prostora", ("35.30.11", 5.00m) },
            { "centralno grijanje", ("35.30.11", 5.00m) },
            
            // Electricity - 5% PDV
            { "elektrièna energija", ("35.11.10", 5.00m) },
            { "struja", ("35.11.10", 5.00m) },
            { "elektrika", ("35.11.10", 5.00m) },
            { "el. energija", ("35.11.10", 5.00m) },
            
            // Gas - 5% PDV
            { "plin", ("35.21.10", 5.00m) },
            { "prirodni plin", ("35.21.10", 5.00m) },
            { "plinski", ("35.21.10", 5.00m) },
            
            // Water - 5% PDV
            { "voda", ("36.00.20", 5.00m) },
            { "vodovod", ("36.00.20", 5.00m) },
            { "pitka voda", ("36.00.20", 5.00m) },
            { "potrošnja vode", ("36.00.20", 5.00m) },
            
            // Waste - 25% PDV (ostaje 25% jer nije komunalna usluga)
            { "otpad", ("38.11.11", 25.00m) },
            { "odvoz smeæa", ("38.11.11", 25.00m) },
            { "komunalni otpad", ("38.11.11", 25.00m) },
            { "smeæe", ("38.11.11", 25.00m) },
            
            // Maintenance/Service - 25% PDV
            { "održavanje", ("43.99.90", 25.00m) },
            { "servis", ("43.99.90", 25.00m) },
            { "popravak", ("43.99.90", 25.00m) },
            { "naknada", ("43.99.90", 25.00m) },
            { "usluga", ("43.99.90", 25.00m) }
        };

        public KpdCodeService(ILogger<KpdCodeService> logger)
        {
            _logger = logger;
        }

        /// Determines KPD code based on service description
        /// Uses keyword matching to identify the service type
        public string GetKpdCodeForService(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                _logger.LogDebug("Empty description, using default KPD code 35.30.11");
                return "35.30.11"; // Default: heating/hot water
            }

            var descriptionLower = description.ToLowerInvariant();

            foreach (var kvp in ServiceKpdMap)
            {
                if (descriptionLower.Contains(kvp.Key))
                {
                    _logger.LogDebug("Matched KPD code {KpdCode} for description '{Description}' (keyword: '{Keyword}')", 
                        kvp.Value.KpdCode, description, kvp.Key);
                    return kvp.Value.KpdCode;
                }
            }

            // Default fallback
            _logger.LogInformation("No KPD code match found for '{Description}', using default 35.30.11", description);
            return "35.30.11"; // Default: heating/hot water
        }

        /// Gets UBL tax category code based on VAT rate
        /// Used in mojE-Raèun XML generation
        public string GetTaxCategoryCode(decimal taxRate)
        {
            return taxRate switch
            {
                0 => "Z",        // Zero-rated
                5.00m => "S",    // Super-reduced standard rate (5%)
                13.00m => "S",   // Reduced standard rate
                25.00m => "S",   // Standard rate
                _ => "S"         // Default to standard
            };
        }

        /// Gets default tax rate based on sevice description
        /// Uses the same keyword matching as KPD code determination
        /// ? UPDATED: Default changed to 5% for utility services
        public decimal GetDefaultTaxRateForService(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return 5.00m; // ? Changed from 13% to 5%

            var descriptionLower = description.ToLowerInvariant();

            foreach (var kvp in ServiceKpdMap)
            {
                if (descriptionLower.Contains(kvp.Key))
                {
                    _logger.LogDebug("Matched tax rate {TaxRate}% for description '{Description}' (keyword: '{Keyword}')", 
                        kvp.Value.DefaultTaxRate, description, kvp.Key);
                    return kvp.Value.DefaultTaxRate;
                }
            }

            return 5.00m; // ? Changed from 13% to 5% - default for utilites
        }
    }
}
