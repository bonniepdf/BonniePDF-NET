using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>A registered font available to the glyph-fallback chain.</summary>
public sealed class FallbackFont
{
    /// <summary>Registry key (<c>family:weight</c>), stable across the document.</summary>
    public required string Key { get; init; }

    /// <summary>Raw TTF bytes, for embedding the fallback font on demand.</summary>
    public required byte[] Bytes { get; init; }

    /// <summary>Parsed font, for glyph-coverage probing.</summary>
    internal TtfFont Font { get; init; } = null!;
}

/// <summary>
/// Global font registry for BonniePDF.
/// </summary>
/// <remarks>
/// Fonts registered here are automatically substituted for standard PDF Type-1
/// fonts (Helvetica, Times-Roman, Courier) when <c>PdfAEnabled = true</c> is set
/// on <see cref="BonniePDF.DocumentSettings"/>. This lets the library
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
    // ConcurrentDictionary: this is a process-wide static registry, and concurrent
    // document generation (a common server workload - see ConcurrencyTests) can
    // register or read fonts from multiple threads at once. A plain Dictionary is
    // not safe under concurrent writes.
    private static readonly ConcurrentDictionary<string, byte[]> _registry = new(StringComparer.OrdinalIgnoreCase);

    // Parsed-font cache for the fallback chain, keyed like _registry. Retaining
    // the source byte-array identity makes replacement registrations coherent:
    // an entry is reparsed whenever the registry value for its key changes.
    private static readonly ConcurrentDictionary<string, ParsedFontCacheEntry> _fallbackCache = new(StringComparer.OrdinalIgnoreCase);

    private sealed record ParsedFontCacheEntry(byte[] Bytes, TtfFont? Font);

    /// <summary>The bundled Liberation faces: file stem, registry family and weight.</summary>
    private static readonly (string FileName, string Family, string Weight)[] BundledFonts =
    [
        ("LiberationSans-Regular", "liberation sans", "regular"),
        ("LiberationSans-Bold", "liberation sans", "bold"),
        ("LiberationSans-Italic", "liberation sans", "italic"),
        ("LiberationSans-BoldItalic", "liberation sans", "bolditalic"),
        ("LiberationSerif-Regular", "liberation serif", "regular"),
        ("LiberationSerif-Bold", "liberation serif", "bold"),
        ("LiberationSerif-Italic", "liberation serif", "italic"),
        ("LiberationSerif-BoldItalic", "liberation serif", "bolditalic"),
        ("LiberationMono-Regular", "liberation mono", "regular"),
        ("LiberationMono-Bold", "liberation mono", "bold"),
        ("LiberationMono-Italic", "liberation mono", "italic"),
        ("LiberationMono-BoldItalic", "liberation mono", "bolditalic"),
    ];

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
    /// Registers a TrueType or static CFF OpenType font from a byte array.
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
    /// Registers a TrueType or static CFF OpenType font from a stream.
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
    /// Returns the parsed registered substitute for a standard PDF font, or
    /// <c>null</c> when no substitute is available. Instances are cached by the
    /// underlying family and weight and are safe to share as immutable metrics.
    /// </summary>
    internal static TtfFont? GetParsedStandardFont(string standardFontName)
    {
        if (!StandardFontMap.TryGetValue(standardFontName, out var mapping)) return null;

        string key = $"{mapping.family}:{mapping.weight}";
        if (!_registry.TryGetValue(key, out byte[]? bytes)) return null;

        return GetParsedFont(key, bytes);
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
    public static bool HasBundledFonts() => !_registry.IsEmpty;

    /// <summary>
    /// Returns every registered font (in registration order) parsed for
    /// glyph-coverage probing, for use as a glyph-fallback chain. Fonts whose
    /// bytes fail to parse are skipped. The parsed instances are cached.
    /// </summary>
    /// <remarks>
    /// The fallback chain lets the encoder render a code point the primary font
    /// lacks (e.g. a <c>★</c> bullet, a symbol, an emoji) using the first
    /// registered font that covers it, rather than emitting a forbidden
    /// <c>.notdef</c> glyph.
    /// </remarks>
    public static IList<FallbackFont> GetFallbackFonts()
    {
        var result = new List<FallbackFont>();
        foreach (var entry in _registry)
        {
            string key = entry.Key;
            byte[] bytes = entry.Value;

            TtfFont? font = GetParsedFont(key, bytes);

            if (font != null)
                result.Add(new FallbackFont { Key = key, Bytes = bytes, Font = font });
        }
        return result;
    }

    /// <summary>Removes all registered fonts (primarily for test isolation).</summary>
    public static void Clear()
    {
        _registry.Clear();
        _fallbackCache.Clear();
    }

    /// <summary>Re-registers the bundled Liberation fonts after a <see cref="Clear"/> call (test isolation).</summary>
    public static void RestoreBundledFonts() => AutoRegisterBundledFonts();

    // ── Internal ─────────────────────────────────────────────────

    private static TtfFont? GetParsedFont(string key, byte[] bytes)
    {
        ParsedFontCacheEntry entry = _fallbackCache.AddOrUpdate(
            key,
            static (_, source) => ParseFont(source),
            static (_, existing, source) => ReferenceEquals(existing.Bytes, source)
                ? existing
                : ParseFont(source),
            bytes);
        return entry.Font;
    }

    private static ParsedFontCacheEntry ParseFont(byte[] bytes)
    {
        try
        {
            return new ParsedFontCacheEntry(bytes, TtfFont.LoadFromBytes(bytes));
        }
        catch
        {
            return new ParsedFontCacheEntry(bytes, null);
        }
    }

    private static void AutoRegisterBundledFonts()
    {
        // 1. Try embedded assembly resources (works in all .NET environments)
        TryRegisterFromResources();

        // 2. Fall back to font files next to the assembly (e.g. after publish)
        if (_registry.IsEmpty)
            TryRegisterFromAssemblyDirectory();
    }

    private static void TryRegisterFromResources()
    {
        var asm = typeof(FontManager).Assembly;
        var resourceNames = asm.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            foreach (var (fileName, family, weight) in BundledFonts)
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

        foreach (var (fileName, family, weight) in BundledFonts)
        {
            string path = Path.Combine(fontsDir, $"{fileName}.ttf");
            if (!File.Exists(path)) continue;
            _registry[$"{family}:{weight}"] = File.ReadAllBytes(path);
        }
    }

    private static string NormalizeWeight(string raw) =>
        raw.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
}
