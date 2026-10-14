using System;

namespace XBLauncher.UwpApp.Services
{
    /// <summary>
    /// QR-Code helper backed by the battle-tested QRCoder library (MIT), so the
    /// device-code login QR is always valid and scannable. Returns PNG bytes via
    /// QRCoder's pure-managed PngByteQRCode (works under UWP/.NET Native without
    /// System.Drawing).
    /// </summary>
    public static class QrCode
    {
        /// <summary>Render <paramref name="text"/> as a PNG QR code.</summary>
        public static byte[] ToPngBytes(string text, int pixelsPerModule = 8)
        {
            using (var gen = new QRCoder.QRCodeGenerator())
            {
                var data = gen.CreateQrCode(text, QRCoder.QRCodeGenerator.ECCLevel.M);
                var png = new QRCoder.PngByteQRCode(data);
                return png.GetGraphic(pixelsPerModule);
            }
        }
    }
}