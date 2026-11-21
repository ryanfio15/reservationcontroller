using QRCoder;

namespace App.UI;

/// <summary>
/// Renders a reservation's QR reference as a PNG data URL.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="PngByteQRCode"/> is used. QRCoder's other renderers reach for
/// <c>System.Drawing.Common</c>, which has no implementation on the browser runtime; the PNG
/// renderer is managed code throughout, so the same class works in the WebAssembly client and in
/// the MAUI shells later.
/// </para>
/// <para>
/// Error correction level Q (25%) is chosen because the code is read off a phone screen at a host
/// stand, often with glare and at an angle.
/// </para>
/// </remarks>
public static class QrCodeRenderer
{
    /// <summary>
    /// The <c>data:</c> URL for an <c>&lt;img&gt;</c> showing <paramref name="reference"/>.
    /// </summary>
    /// <param name="reference">
    /// The ticket reference, in the versioned form <c>R1.&lt;keyId&gt;.&lt;reservationId&gt;.&lt;mac&gt;</c>.
    /// </param>
    /// <param name="pixelsPerModule">Size of one QR module in device pixels.</param>
    public static string ToPngDataUrl(string reference, int pixelsPerModule = 8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelsPerModule, 1);

        return $"data:image/png;base64,{Convert.ToBase64String(ToPng(reference, pixelsPerModule))}";
    }

    /// <summary>The PNG bytes for <paramref name="reference"/>.</summary>
    public static byte[] ToPng(string reference, int pixelsPerModule = 8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelsPerModule, 1);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(reference, QRCodeGenerator.ECCLevel.Q);

        return new PngByteQRCode(data).GetGraphic(pixelsPerModule);
    }
}
