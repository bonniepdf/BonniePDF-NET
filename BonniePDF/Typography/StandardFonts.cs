namespace BonniePDF.Typography;

/// <summary>
/// Standard PDF font names (14 Type 1 fonts built into every PDF reader)
/// </summary>
public static class StandardFonts
{
    // Serif
    public const string TimesRoman = "Times-Roman";
    public const string TimesBold = "Times-Bold";
    public const string TimesItalic = "Times-Italic";
    public const string TimesBoldItalic = "Times-BoldItalic";

    // Sans-Serif
    public const string Helvetica = "Helvetica";
    public const string HelveticaBold = "Helvetica-Bold";
    public const string HelveticaOblique = "Helvetica-Oblique";
    public const string HelveticaBoldOblique = "Helvetica-BoldOblique";

    // Monospace
    public const string Courier = "Courier";
    public const string CourierBold = "Courier-Bold";
    public const string CourierOblique = "Courier-Oblique";
    public const string CourierBoldOblique = "Courier-BoldOblique";

    // Special
    public const string Symbol = "Symbol";
    public const string ZapfDingbats = "ZapfDingbats";
}