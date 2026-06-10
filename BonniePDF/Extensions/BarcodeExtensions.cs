using BonniePDF.Core;
using BonniePDF.Elements.Shapes;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for 1D barcodes.
/// </summary>
public static class BarcodeExtensions
{
    /// <summary>
    /// Sets the height of the bars in points.
    /// </summary>
    public static Barcode Height(this Barcode barcode, float height)
    {
        barcode.Height = height;
        return barcode;
    }

    /// <summary>Sets the width of the narrowest bar (one module) in points.</summary>
    public static Barcode ModuleWidth(this Barcode barcode, float moduleWidth)
    {
        barcode.ModuleWidth = moduleWidth;
        return barcode;
    }

    /// <summary>Sets the number of quiet-zone modules on each side.</summary>
    public static Barcode QuietZone(this Barcode barcode, int modules)
    {
        barcode.QuietZone = modules;
        return barcode;
    }

    /// <summary>Toggles the human-readable text label beneath the bars.</summary>
    public static Barcode WithTextLabel(this Barcode barcode, bool include = true)
    {
        barcode.IncludeTextLabel = include;
        return barcode;
    }

    /// <summary>Sets the font size of the human-readable text label, in points.</summary>
    public static Barcode TextLabelFontSize(this Barcode barcode, float fontSize)
    {
        barcode.TextLabelFontSize = fontSize;
        return barcode;
    }

    /// <summary>Sets the colour of the bars.</summary>
    public static Barcode ForegroundColor(this Barcode barcode, Color color)
    {
        barcode.ForegroundColor = color;
        return barcode;
    }

    /// <summary>Sets the background colour.</summary>
    public static Barcode BackgroundColor(this Barcode barcode, Color color)
    {
        barcode.BackgroundColor = color;
        return barcode;
    }

    /// <summary>Toggles drawing of the background.</summary>
    public static Barcode DrawBackground(this Barcode barcode, bool draw)
    {
        barcode.DrawBackground = draw;
        return barcode;
    }

    /// <summary>Sets the alternate text for accessibility (PDF/UA-1).</summary>
    public static Barcode AltText(this Barcode barcode, string altText)
    {
        barcode.AltText = altText;
        return barcode;
    }
}
