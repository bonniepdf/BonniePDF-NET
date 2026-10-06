using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Resolves a font family name + weight + italic to the actual PDF font name or TTF file path.
/// Handles the 14 standard PDF fonts (Helvetica, Times, Courier families) and custom TTF families.
/// </summary>
public static class FontFamilyResolver
{
    /// <summary>
    /// Registered custom font families mapping family name to variant paths.
    /// </summary>
    private static readonly Dictionary<string, FontFamilyDefinition> CustomFamilies = new();

    /// <summary>
    /// Standard font family lookup: (family, isBold, isItalic) → PDF font name.
    /// Maps the 3 standard families (Helvetica, Times, Courier) to their 4 variants each.
    /// </summary>
    private static readonly Dictionary<(string family, bool bold, bool italic), string> StandardFonts = new()
    {
        // Helvetica family
        [("Helvetica", false, false)] = BonniePDF.StandardFonts.Helvetica,
        [("Helvetica", true, false)] = BonniePDF.StandardFonts.HelveticaBold,
        [("Helvetica", false, true)] = BonniePDF.StandardFonts.HelveticaOblique,
        [("Helvetica", true, true)] = BonniePDF.StandardFonts.HelveticaBoldOblique,

        // Times family
        [("Times", false, false)] = BonniePDF.StandardFonts.TimesRoman,
        [("Times", true, false)] = BonniePDF.StandardFonts.TimesBold,
        [("Times", false, true)] = BonniePDF.StandardFonts.TimesItalic,
        [("Times", true, true)] = BonniePDF.StandardFonts.TimesBoldItalic,

        // Courier family
        [("Courier", false, false)] = BonniePDF.StandardFonts.Courier,
        [("Courier", true, false)] = BonniePDF.StandardFonts.CourierBold,
        [("Courier", false, true)] = BonniePDF.StandardFonts.CourierOblique,
        [("Courier", true, true)] = BonniePDF.StandardFonts.CourierBoldOblique
    };

    /// <summary>
    /// Maps family name aliases to their canonical family name.
    /// Allows users to specify "Arial" or "sans-serif" and get Helvetica.
    /// </summary>
    private static readonly Dictionary<string, string> FamilyAliases = new()
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
        if (string.IsNullOrEmpty(family)) return BonniePDF.StandardFonts.Helvetica;

        // A font name a document made for one of its faces is already resolved.
        if (DocumentFonts.IsDocumentFontName(family)) return family;

        // The document's own fonts come first (spec 16 §4).
        DocumentFontFace? documentFace = FontMetrics.ActiveDocumentFonts?.Resolve(family, weight, italic);
        if (documentFace is not null) return documentFace.FontName;

        // If the family is already a fully-qualified standard font name, return it directly
        if (IsStandardFontName(family)) return family;

        // If it's a TTF file path, check custom families first
        if (family.EndsWith(".ttf", System.StringComparison.OrdinalIgnoreCase) ||
            family.EndsWith(".otf", System.StringComparison.OrdinalIgnoreCase))
            return family; // Direct file path - no variant resolution

        bool isBold = weight >= FontWeight.SemiBold;

        // Check custom registered families
        if (CustomFamilies.TryGetValue(family, out FontFamilyDefinition? definition))
            return definition.GetVariant(isBold, italic);

        // Resolve alias to canonical family name
        string canonicalFamily = ResolveAlias(family);

        // Look up standard font
        if (StandardFonts.TryGetValue((canonicalFamily, isBold, italic), out string? fontName))
            return fontName;

        // Fallback: return Helvetica variant
        return StandardFonts.GetValueOrDefault(("Helvetica", isBold, italic), BonniePDF.StandardFonts.Helvetica);
    }

    /// <summary>
    /// Resolves a family name as <see cref="Resolve"/> does, but only when the name is
    /// known: a registered custom family, a standard family, or one of their aliases.
    /// Returns null rather than falling back to Helvetica, and never treats the name
    /// as a file path.
    /// </summary>
    /// <remarks>
    /// For callers that must not substitute a font nobody chose, such as imported SVG,
    /// whose <c>font-family</c> lists fall through to the next name instead.
    /// </remarks>
    public static string? ResolveKnown(string family, FontWeight weight, bool italic)
    {
        DocumentFontFace? documentFace = FontMetrics.ActiveDocumentFonts?.Resolve(family, weight, italic);
        if (documentFace is not null) return documentFace.FontName;
        bool isBold = weight >= FontWeight.SemiBold;
        if (CustomFamilies.TryGetValue(family, out FontFamilyDefinition? definition))
            return definition.GetVariant(isBold, italic);
        return StandardFonts.TryGetValue((ResolveAlias(family), isBold, italic), out string? fontName) ? fontName : null;
    }

    /// <summary>
    /// Registers a custom font family with paths to its variant TTF files.
    /// </summary>
    public static void RegisterFamily(string familyName, FontFamilyDefinition definition)
    {
        CustomFamilies[familyName] = definition;
    }

    /// <summary>
    /// Clears all registered custom font families.
    /// </summary>
    public static void ClearCustomFamilies()
    {
        CustomFamilies.Clear();
    }

    /// <summary>
    /// Resolves a family name alias to its canonical name (case-insensitive).
    /// Returns the original name if no alias is found.
    /// </summary>
    private static string ResolveAlias(string family)
    {
        string lower = family.ToLowerInvariant().Trim();
        return FamilyAliases.TryGetValue(lower, out string? canonical) ? canonical : family;
    }

    /// <summary>
    /// Checks whether a font name is already a fully-qualified standard font variant name
    /// (e.g. "Helvetica-Bold", "Times-Italic") that should be passed through as-is.
    /// Base family names ("Helvetica", "Times-Roman", "Courier") are intentionally excluded
    /// so that Weight and Italic parameters are still applied when resolving them.
    /// </summary>
    private static bool IsStandardFontName(string name)
    {
        return name switch
        {
            // Helvetica variants - base "Helvetica" excluded so Bold/Italic can still be applied
            BonniePDF.StandardFonts.HelveticaBold
                or BonniePDF.StandardFonts.HelveticaOblique
                or BonniePDF.StandardFonts.HelveticaBoldOblique
            // Times variants - base "Times-Roman" excluded so Bold/Italic can still be applied
                or BonniePDF.StandardFonts.TimesBold
                or BonniePDF.StandardFonts.TimesItalic
                or BonniePDF.StandardFonts.TimesBoldItalic
            // Courier variants - base "Courier" excluded so Bold/Italic can still be applied
                or BonniePDF.StandardFonts.CourierBold
                or BonniePDF.StandardFonts.CourierOblique
                or BonniePDF.StandardFonts.CourierBoldOblique
            // Special fonts with no variants
                or BonniePDF.StandardFonts.Symbol
                or BonniePDF.StandardFonts.ZapfDingbats => true,
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
    public required string Regular { get; set; }

    /// <summary>Bold variant path. Falls back to Regular if not set.</summary>
    public string? Bold { get; set; }

    /// <summary>Italic variant path. Falls back to Regular if not set.</summary>
    public string? Italic { get; set; }

    /// <summary>Bold Italic variant path. Falls back to Bold, then Italic, then Regular.</summary>
    public string? BoldItalic { get; set; }

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
