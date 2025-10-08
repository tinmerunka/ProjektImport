namespace InventoryManagementAPI.Constants
{
    public static class TaxRateConstants
    {
        public const decimal Zero = 0.00m;
        public const decimal Reduced1 = 5.00m;   // Knjige, lijekovi, zdravstvo
        public const decimal Reduced2 = 13.00m;  // Hoteli, restorani, prijevoz
        public const decimal Standard = 25.00m;   // Standardna stopa

        public static readonly List<decimal> AllowedRates = new()
        {
            Zero, Reduced1, Reduced2, Standard
        };

        public static bool IsValid(decimal taxRate)
        {
            return AllowedRates.Contains(taxRate);
        }

        public static string GetDescription(decimal taxRate)
        {
            return taxRate switch
            {
                Zero => "0% - Oslobođeno PDV-a",
                Reduced1 => "5% - Snižena stopa (knjige, lijekovi)",
                Reduced2 => "13% - Snižena stopa (ugostiteljstvo, hoteli)",
                Standard => "25% - Standardna stopa",
                _ => $"{taxRate}% - Nestandardna stopa"
            };
        }

        // Primjeri razloga oslobođenja za pomoć korisnicima
        public static readonly List<string> CommonExemptionReasons = new()
        {
            "Nije u sustavu PDV-a",
            "Oslobođeni proizvod prema čl. 39 Zakona o PDV-u",
            "Izvoz robe izvan EU",
            "Isporuka unutar EU s PDV ID-om",
            "Poseban režim oporezivanja marže",
            "Financijske usluge",
            "Osiguranje",
            "Zdravstvene usluge",
            "Obrazovne usluge",
            "Kulturne usluge"
        };
    }
}