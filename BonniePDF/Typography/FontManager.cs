using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Typography;

/// <summary>
/// Global font registry for BonniePDF.
/// </summary>
/// <remarks>
/// Fonts registered here are automatically substituted for standard PDF Type-1
/// fonts (Helvetica, Times-Roman, Courier) when <c>PdfAEnabled = true</c> is set
/// on <see cref="BonniePDF.Configuration.DocumentSettings"/>. This lets the library
/// produce genuinely PDF/A-1b–conformant output without the caller having to supply
/// font paths on every element.
///
/// <para><b>Zero-config:</b> the bundled Liberation fonts (OFL license, metric-compatible
/// with Helvetica, Times and Courier) are registered automatically from the embedded
/// assembly resources when the class is first referenced. No code changes are required
/// on the caller side.</para>
///
/// <para><b>Custom fonts:</b> call <see cref="RegisterFont(byte[], string?, string?)"/>
/// or <see cref="RegisterFont(Stream, string?, string?)"/> to register additional or
/// replacement fonts before generating any document.</para>
/// </remarks>
public static class FontManager
{
    // Keys: $"{family.ToLower()}:{normalizedWeight}"
    private static readonly Dictionary<string, byte[]> _registry = new(StringComparer.OrdinalIgnoreCase);

    // Standard PDF font → Liberation family+weight mapping
    private static readonly Dictionary<string, (string family, string weight)> StandardFontMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Helvetica family → Liberation Sans
            ["Helvetica"]            = ("liberation sans", "regular"),
            ["Helvetica-Bold"]       = ("liberation sans", "bold"),
            ["Helvetica-Oblique"]    = ("liberation sans", "italic"),
            ["Helvetica-BoldOblique"]= ("liberation sans", "bolditalic"),
            // Times family → Liberation Serif
            ["Times-Roman"]          = ("liberation serif", "regular"),
            ["Times-Bold"]           = ("liberation serif", "bold"),
            ["Times-Italic"]         = ("liberation serif", "italic"),
            ["Times-BoldItalic"]     = ("liberation serif", "bolditalic"),
            // Courier family → Liberation Mono
            ["Courier"]              = ("liberation mono", "regular"),
            ["Courier-Bold"]         = ("liberation mono", "bold"),
            ["Courier-Oblique"]      = ("liberation mono", "italic"),
            ["Courier-BoldOblique"]  = ("liberation mono", "bolditalic"),
        };

    static FontManager()
    {
        AutoRegisterBundledFonts();
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Registers a TrueType font from a byte array.
    /// The family and subfamily names are read from the font's name table unless overridden.
    /// </summary>
    /// <param name="bytes">Raw TTF font data.</param>
    /// <param name="family">Override the family name (e.g. "MyFont").</param>
    /// <param name="subfamily">Override the subfamily (e.g. "Bold", "Italic").</param>
    public static void RegisterFont(byte[] bytes, string? family = null, string? subfamily = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var font = TtfFont.LoadFromBytes(bytes);
        var fam = (family ?? font.FontFamily).ToLowerInvariant();
        var wt  = NormalizeWeight(subfamily ?? font.FontSubfamily ?? "Regular");
        _registry[$"{fam}:{wt}"] = bytes;
    }

    /// <summary>
    /// Registers a TrueType font from a stream.
    /// The stream is read to completion and the data is stored in memory.
    /// </summary>
    /// <param name="stream">Readable stream containing raw TTF data.</param>
    /// <param name="family">Override the family name.</param>
    /// <param name="subfamily">Override the subfamily.</param>
    public static void RegisterFont(Stream stream, string? family = null, string? subfamily = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        RegisterFont(ms.ToArray(), family, subfamily);
    }

    /// <summary>
    /// Returns raw TTF bytes for the given standard PDF font identifier
    /// (e.g. <c>"Helvetica"</c>, <c>"Times-Bold"</c>, <c>"Courier-Oblique"</c>),
    /// or <c>null</c> if no substitution font has been registered.
    /// </summary>
    public static byte[]? GetBytesForStandardFont(string standardFontName)
    {
        if (!StandardFontMap.TryGetValue(standardFontName, out var mapping))
            return null;
        return GetBytesForFamily(mapping.family, mapping.weight);
    }

    /// <summary>
    /// Returns raw TTF bytes for a registered family/weight pair, or <c>null</c>.
    /// </summary>
    public static byte[]? GetBytesForFamily(string family, string subfamily)
    {
        var key = $"{family.ToLowerInvariant()}:{NormalizeWeight(subfamily)}";
        return _registry.TryGetValue(key, out var bytes) ? bytes : null;
    }

    /// <summary>Returns <c>true</c> if at least one substitution font is registered.</summary>
    public static bool HasBundledFonts() => _registry.Count > 0;

    /// <summary>Removes all registered fonts (primarily for test isolation).</summary>
    public static void Clear() => _registry.Clear();

    // ── Internal ─────────────────────────────────────────────────

    private static void AutoRegisterBundledFonts()
    {
        // 1. Try embedded assembly resources (works in all .NET environments)
        TryRegisterFromResources();

        // 2. Fall back to font files next to the assembly (e.g. after publish)
        if (_registry.Count == 0)
            TryRegisterFromAssemblyDirectory();
    }

    private static void TryRegisterFromResources()
    {
        var asm = typeof(FontManager).Assembly;
        var resourceNames = asm.GetManifestResourceNames();

        var map = new Dictionary<string, (string family, string weight)>(StringComparer.OrdinalIgnoreCase)
        {
            ["LiberationSans-Regular"]      = ("liberation sans",  "regular"),
            ["LiberationSans-Bold"]         = ("liberation sans",  "bold"),
            ["LiberationSans-Italic"]       = ("liberation sans",  "italic"),
            ["LiberationSans-BoldItalic"]   = ("liberation sans",  "bolditalic"),
            ["LiberationSerif-Regular"]     = ("liberation serif", "regular"),
            ["LiberationSerif-Bold"]        = ("liberation serif", "bold"),
            ["LiberationSerif-Italic"]      = ("liberation serif", "italic"),
            ["LiberationSerif-BoldItalic"]  = ("liberation serif", "bolditalic"),
            ["LiberationMono-Regular"]      = ("liberation mono",  "regular"),
            ["LiberationMono-Bold"]         = ("liberation mono",  "bold"),
            ["LiberationMono-Italic"]       = ("liberation mono",  "italic"),
            ["LiberationMono-BoldItalic"]   = ("liberation mono",  "bolditalic"),
        };

        foreach (var resourceName in resourceNames)
        {
            foreach (var (fileName, (family, weight)) in map)
            {
                if (!resourceName.EndsWith($"{fileName}.ttf", StringComparison.OrdinalIgnoreCase))
                    continue;

                using var stream = asm.GetManifestResourceStream(resourceName)!;
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                _registry[$"{family}:{weight}"] = ms.ToArray();
                break;
            }
        }
    }

    private static void TryRegisterFromAssemblyDirectory()
    {
        string? asmDir = Path.GetDirectoryName(typeof(FontManager).Assembly.Location);
        if (asmDir is null) return;

        string fontsDir = Path.Combine(asmDir, "Fonts");
        if (!Directory.Exists(fontsDir)) return;

        var map = new Dictionary<string, (string family, string weight)>
        {
            ["LiberationSans-Regular"]      = ("liberation sans",  "regular"),
            ["LiberationSans-Bold"]         = ("liberation sans",  "bold"),
            ["LiberationSans-Italic"]       = ("liberation sans",  "italic"),
            ["LiberationSans-BoldItalic"]   = ("liberation sans",  "bolditalic"),
            ["LiberationSerif-Regular"]     = ("liberation serif", "regular"),
            ["LiberationSerif-Bold"]        = ("liberation serif", "bold"),
            ["LiberationSerif-Italic"]      = ("liberation serif", "italic"),
            ["LiberationSerif-BoldItalic"]  = ("liberation serif", "bolditalic"),
            ["LiberationMono-Regular"]      = ("liberation mono",  "regular"),
            ["LiberationMono-Bold"]         = ("liberation mono",  "bold"),
            ["LiberationMono-Italic"]       = ("liberation mono",  "italic"),
            ["LiberationMono-BoldItalic"]   = ("liberation mono",  "bolditalic"),
        };

        foreach (var (fileName, (family, weight)) in map)
        {
            string path = Path.Combine(fontsDir, $"{fileName}.ttf");
            if (!File.Exists(path)) continue;
            _registry[$"{family}:{weight}"] = File.ReadAllBytes(path);
        }
    }

    private static string NormalizeWeight(string raw) =>
        raw.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
}
