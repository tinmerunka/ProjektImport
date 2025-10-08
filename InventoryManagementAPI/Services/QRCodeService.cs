using QRCoder;

namespace InventoryManagementAPI.Services
{
    public interface IQRCodeService
    {
        byte[] GenerateFiscalQRCode(string jir);
        string GenerateFiscalQRCodeBase64(string jir);
    }

    public class QRCodeService : IQRCodeService
    {
        /// <summary>
        /// Generira QR kod za fiskalizirani račun (vraća byte array PNG-a)
        /// </summary>
        public byte[] GenerateFiscalQRCode(string jir)
        {
            if (string.IsNullOrEmpty(jir))
                throw new ArgumentException("JIR is required", nameof(jir));

            // FINA format za QR kod
            var qrContent = $"https://porezna-uprava.hr/rn/jir?jir={jir}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            return qrCode.GetGraphic(20); // 20 pixels per module
        }

        /// <summary>
        /// Generira QR kod kao Base64 string (za frontend)
        /// </summary>
        public string GenerateFiscalQRCodeBase64(string jir)
        {
            var qrBytes = GenerateFiscalQRCode(jir);
            return Convert.ToBase64String(qrBytes);
        }
    }
}