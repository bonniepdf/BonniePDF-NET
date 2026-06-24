using BonniePDF.Core;
using BonniePDF.Elements.Shapes;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for QR codes.
/// </summary>
public static class QRCodeExtensions
{
    /// <summary>
    /// Sets the foreground color of the QR code modules.
    /// </summary>
    public static QRCode ForegroundColor(this QRCode qr, Color color)
    {
        qr.ForegroundColor = color;
        return qr;
    }

    /// <summary>
    /// Sets the background color of the QR code.
    /// </summary>
    public static QRCode BackgroundColor(this QRCode qr, Color color)
    {
        qr.BackgroundColor = color;
        return qr;
    }

    /// <summary>
    /// Toggles the drawing of the QR code background.
    /// </summary>
    public static QRCode DrawBackground(this QRCode qr, bool draw)
    {
        qr.DrawBackground = draw;
        return qr;
    }

    /// <summary>
    /// Sets the error correction level for the QR code.
    /// </summary>
    public static QRCode ErrorCorrection(this QRCode qr, QrErrorCorrection level)
    {
        qr.ErrorCorrection = level;
        return qr;
    }

    /// <summary>
    /// Sets the number of quiet zone modules around the QR code.
    /// </summary>
    public static QRCode QuietZone(this QRCode qr, int modules)
    {
        qr.QuietZone = modules;
        return qr;
    }

    /// <summary>Sets the alternate text for accessibility (PDF/UA-1).</summary>
    public static QRCode AltText(this QRCode qr, string altText)
    {
        qr.AltText = altText;
        return qr;
    }
}
