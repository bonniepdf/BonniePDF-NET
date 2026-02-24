using System.Collections.Generic;

namespace BonniePDF.Typography;

/// <summary>
/// Resolves a font family name + weight + italic to the actual PDF font name or TTF file path.
/// Handles the 14 standard PDF fonts (Helvetica, Times, Courier families) and custom TTF families.
/// </summary>
public static class FontFamilyResolver
{
    /// <summary>
    /// Registered custom font families mapping family name to variant paths.
    /// </summary>
    private static readonly Dictionary<string, FontFamilyDefinition> _customFamilies = new();

    /// <summary>
    /// Standard font family lookup: (family, isBold, isItalic) → PDF font name.
    /// Maps the 3 standard families (Helvetica, Times, Courier) to their 4 variants each.
    /// </summary>
    private static readonly Dictionary<(string family, bool bold, bool italic), string> _standardFonts = new()
    {
        // Helvetica family
        [("Helvetica", false, false)] = StandardFonts.Helvetica,
        [("Helvetica", true, false)] = StandardFonts.HelveticaBold,
        [("Helvetica", false, true)] = StandardFonts.HelveticaOblique,
        [("Helvetica", true, true)] = StandardFonts.HelveticaBoldOblique,

        // Times family
        [("Times", false, false)] = StandardFonts.TimesRoman,
        [("Times", true, false)] = StandardFonts.TimesBold,
        [("Times", false, true)] = StandardFonts.TimesItalic,
        [("Times", true, true)] = StandardFonts.TimesBoldItalic,

        // Courier family
        [("Courier", false, false)] = StandardFonts.Courier,
        [("Courier", true, false)] = StandardFonts.CourierBold,
        [("Courier", false, true)] = StandardFonts.CourierOblique,
        [("Courier", true, true)] = StandardFonts.CourierBoldOblique
    };

    /// <summary>
    /// Maps family name aliases to their canonical family name.
    /// Allows users to specify "Arial" or "sans-serif" and get Helvetica.
    /// </summary>
    private static readonly Dictionary<string, string> _familyAliases = new()
    {
        ["arial"] = "Helvetica",
        ["sans-serif"] = "Helvetica",
        ["helvetica"] = "Helvetica",
        ["times"] = "Times",
        ["times new roman"] = "Times",
        ["times-roman"] = "Times",
        ["serif"] = "Times",
        ["courier"] = "Courier",
        ["courier new"] = "Courier",
        ["monospace"] = "Courier",
        ["mono"] = "Courier"
    };

    /// <summary>
    /// Resolves a font family + weight + italic to the actual font name or path.
    /// </summary>
    /// <param name="family">Font family name (e.g. "Helvetica", "Arial", or a TTF path)</param>
    /// <param name="weight">Font weight (Normal, Bold, etc.)</param>
    /// <param name="italic">Whether italic/oblique is requested</param>
    /// <returns>The resolved PDF font name or TTF file path</returns>
    public static string Resolve(string family, FontWeight weight, bool italic)
    {
        if (string.IsNullOrEmpty(family)) return StandardFonts.Helvetica;

        // If the family is already a fully-qualified standard font name, return it directly
        if (IsStandardFontName(family)) return family;

        // If it's a TTF file path, check custom families first
        if (family.EndsWith(".ttf", System.StringComparison.OrdinalIgnoreCase) ||
            family.EndsWith(".otf", System.StringComparison.OrdinalIgnoreCase))
            return family; // Direct file path — no variant resolution

        bool isBold = weight >= FontWeight.SemiBold;

        // Check custom registered families
        if (_customFamilies.TryGetValue(family, out FontFamilyDefinition definition))
            return definition.GetVariant(isBold, italic);

        // Resolve alias to canonical family name
        string canonicalFamily = ResolveAlias(family);

        // Look up standard font
        if (_standardFonts.TryGetValue((canonicalFamily, isBold, italic), out string fontName))
            return fontName;

        // Fallback: return Helvetica variant
        if (_standardFonts.TryGetValue(("Helvetica", isBold, italic), out string fallback))
            return fallback;

        return StandardFonts.Helvetica;
    }

    /// <summary>
    /// Registers a custom font family with paths to its variant TTF files.
    /// </summary>
    public static void RegisterFamily(string familyName, FontFamilyDefinition definition)
    {
        _customFamilies[familyName] = definition;
    }

    /// <summary>
    /// Clears all registered custom font families.
    /// </summary>
    public static void ClearCustomFamilies()
    {
        _customFamilies.Clear();
    }

    /// <summary>
    /// Resolves a family name alias to its canonical name (case-insensitive).
    /// Returns the original name if no alias is found.
    /// </summary>
    private static string ResolveAlias(string family)
    {
        string lower = family.ToLowerInvariant().Trim();
        return _familyAliases.TryGetValue(lower, out string canonical) ? canonical : family;
    }

    /// <summary>
    /// Checks whether a font name is already a fully-qualified standard font name
    /// (e.g. "Helvetica-Bold", "Times-Italic").
    /// </summary>
    private static bool IsStandardFontName(string name)
    {
        return name switch
        {
            StandardFonts.Helvetica or StandardFonts.HelveticaBold
                or StandardFonts.HelveticaOblique or StandardFonts.HelveticaBoldOblique
                or StandardFonts.TimesRoman or StandardFonts.TimesBold
                or StandardFonts.TimesItalic or StandardFonts.TimesBoldItalic
                or StandardFonts.Courier or StandardFonts.CourierBold
                or StandardFonts.CourierOblique or StandardFonts.CourierBoldOblique
                or StandardFonts.Symbol or StandardFonts.ZapfDingbats => true,
            _ => false
        };
    }
}

/// <summary>
/// Defines the TTF file paths for each variant of a custom font family.
/// </summary>
public class FontFamilyDefinition
{
    /// <summary>Regular (Normal weight, non-italic) variant path</summary>
    public string Regular { get; set; }

    /// <summary>Bold variant path. Falls back to Regular if not set.</summary>
    public string Bold { get; set; }

    /// <summary>Italic variant path. Falls back to Regular if not set.</summary>
    public string Italic { get; set; }

    /// <summary>Bold Italic variant path. Falls back to Bold, then Italic, then Regular.</summary>
    public string BoldItalic { get; set; }

    /// <summary>
    /// Gets the best matching variant path for the requested style.
    /// Falls back gracefully when a specific variant isn't available.
    /// </summary>
    public string GetVariant(bool bold, bool italic)
    {
        if (bold && italic)
            return BoldItalic ?? Bold ?? Italic ?? Regular;
        if (bold)
            return Bold ?? Regular;
        if (italic)
            return Italic ?? Regular;
        return Regular;
    }
}
