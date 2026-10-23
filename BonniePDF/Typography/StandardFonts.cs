namespace BonniePDF;

/// <summary>
/// Standard PDF font names (14 Type 1 fonts built into every PDF reader)
/// </summary>
public static class StandardFonts
{
    // Serif
    /// <summary>The standard PDF font <c>Times-Roman</c>, which every PDF reader provides.</summary>
    public const string TimesRoman = "Times-Roman";
    /// <summary>The standard PDF font <c>Times-Bold</c>, which every PDF reader provides.</summary>
    public const string TimesBold = "Times-Bold";
    /// <summary>The standard PDF font <c>Times-Italic</c>, which every PDF reader provides.</summary>
    public const string TimesItalic = "Times-Italic";
    /// <summary>The standard PDF font <c>Times-BoldItalic</c>, which every PDF reader provides.</summary>
    public const string TimesBoldItalic = "Times-BoldItalic";

    // Sans-Serif
    /// <summary>The standard PDF font <c>Helvetica</c>, which every PDF reader provides.</summary>
    public const string Helvetica = "Helvetica";
    /// <summary>The standard PDF font <c>Helvetica-Bold</c>, which every PDF reader provides.</summary>
    public const string HelveticaBold = "Helvetica-Bold";
    /// <summary>The standard PDF font <c>Helvetica-Oblique</c>, which every PDF reader provides.</summary>
    public const string HelveticaOblique = "Helvetica-Oblique";
    /// <summary>The standard PDF font <c>Helvetica-BoldOblique</c>, which every PDF reader provides.</summary>
    public const string HelveticaBoldOblique = "Helvetica-BoldOblique";

    // Monospace
    /// <summary>The standard PDF font <c>Courier</c>, which every PDF reader provides.</summary>
    public const string Courier = "Courier";
    /// <summary>The standard PDF font <c>Courier-Bold</c>, which every PDF reader provides.</summary>
    public const string CourierBold = "Courier-Bold";
    /// <summary>The standard PDF font <c>Courier-Oblique</c>, which every PDF reader provides.</summary>
    public const string CourierOblique = "Courier-Oblique";
    /// <summary>The standard PDF font <c>Courier-BoldOblique</c>, which every PDF reader provides.</summary>
    public const string CourierBoldOblique = "Courier-BoldOblique";

    // Special
    /// <summary>The standard PDF font <c>Symbol</c>, which every PDF reader provides.</summary>
    public const string Symbol = "Symbol";
    /// <summary>The standard PDF font <c>ZapfDingbats</c>, which every PDF reader provides.</summary>
    public const string ZapfDingbats = "ZapfDingbats";
}