using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Typography;

/// <summary>
/// Global font registry for BonniePDF.
///
/// Fonts registered here are automatically substituted for standard PDF type-1
/// fonts (Helvetica, Times-Roman, Courier) when <c>PdfAEnabled = true</c> is
/// set on <see cref="Configuration.DocumentSettings"/>. This allows the library
/// to produce genuinely PDF/A-1b conformant output without the caller having to
/// supply font bytes on every element.
///
/// The static constructor auto-registers the 12 bundled Liberation fonts from
/// embedded assembly resources — no caller code needed.
/// </summary>
public static class FontManager
{
    private static readonly Dictionary<string, string> StandardFontMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Helvetica"]             = "liberation sans:regular",
        ["Helvetica-Bold"]        = "liberation sans:bold",
        ["Helvetica-Oblique"]     = "liberation sans:italic",
        ["Helvetica-BoldOblique"] = "liberation sans:bolditalic",
        ["Times-Roman"]           = "liberation serif:regular",
        ["Times-Bold"]            = "liberation serif:bold",
        ["Times-Italic"]          = "liberation serif:italic",
        ["Times-BoldItalic"]      = "liberation serif:bolditalic",
        ["Courier"]               = "liberation mono:regular",
        ["Courier-Bold"]          = "liberation mono:bold",
        ["Courier-Oblique"]       = "liberation mono:italic",
        ["Courier-BoldOblique"]   = "liberation mono:bolditalic",
    };

    // Key: "family:weight" (lower-case, no spaces in weight)
    private static readonly Dictionary<string, byte[]> Registry = new(StringComparer.OrdinalIgnoreCase);

    static FontManager()
    {
        AutoRegisterBundledFonts();
    }

    /// <summary>Registers a font from raw bytes.</summary>
    public static void RegisterFont(byte[] bytes, string family = null, string subfamily = null)
    {
        var font = TtfFont.LoadFromBytes(bytes);
        var fam  = (family    ?? font.FontFamily).ToLowerInvariant();
        var wt   = NormalizeWeight(subfamily ?? font.FontSubfamily ?? "Regular");
        Registry[$"{fam}:{wt}"] = bytes;
    }

    /// <summary>Registers a font from a stream.</summary>
    public static void RegisterFont(Stream stream, string family = null, string subfamily = null)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        RegisterFont(ms.ToArray(), family, subfamily);
    }

    /// <summary>Returns TTF bytes for a standard PDF font identifier, or null if not registered.</summary>
    public static byte[] GetBytesForStandardFont(string standardFontName)
    {
        if (!StandardFontMap.TryGetValue(standardFontName, out var key)) return null;
        var parts = key.Split(':');
        return GetBytesForFamily(parts[0], parts[1]);
    }

    /// <summary>Returns TTF bytes for a family/weight pair, or null if not registered.</summary>
    public static byte[] GetBytesForFamily(string family, string subfamily)
    {
        var key = $"{family.ToLowerInvariant()}:{NormalizeWeight(subfamily)}";
        return Registry.TryGetValue(key, out var bytes) ? bytes : null;
    }

    /// <summary>Returns true if at least one font is registered.</summary>
    public static bool HasBundledFonts() => Registry.Count > 0;

    /// <summary>Clears all registered fonts (primarily for test isolation).</summary>
    public static void Clear() => Registry.Clear();

    /// <summary>Re-registers the bundled Liberation fonts (call after Clear() in tests).</summary>
    public static void RestoreBundledFonts() => AutoRegisterBundledFonts();

    private static void AutoRegisterBundledFonts()
    {
        // Try loading from embedded assembly resources first
        if (TryRegisterFromResources()) return;
        // Fall back to font files alongside the assembly (development layout)
        TryRegisterFromAssemblyDirectory();
    }

    private static bool TryRegisterFromResources()
    {
        var asm = typeof(FontManager).Assembly;
        string[] fonts = {
            "LiberationSans-Regular",    "LiberationSans-Bold",
            "LiberationSans-Italic",     "LiberationSans-BoldItalic",
            "LiberationSerif-Regular",   "LiberationSerif-Bold",
            "LiberationSerif-Italic",    "LiberationSerif-BoldItalic",
            "LiberationMono-Regular",    "LiberationMono-Bold",
            "LiberationMono-Italic",     "LiberationMono-BoldItalic",
        };
        bool anyLoaded = false;
        foreach (var name in fonts)
        {
            var resourceName = $"BonniePDF.Typography.Fonts.{name}.ttf";
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) continue;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();
            try
            {
                var font = TtfFont.LoadFromBytes(bytes);
                var fam = font.FontFamily.ToLowerInvariant();
                var wt  = NormalizeWeight(font.FontSubfamily ?? "Regular");
                Registry[$"{fam}:{wt}"] = bytes;
                anyLoaded = true;
            }
            catch
            {
                // ignore individual load failures
            }
        }
        return anyLoaded;
    }

    private static void TryRegisterFromAssemblyDirectory()
    {
        var dir = Path.GetDirectoryName(typeof(FontManager).Assembly.Location);
        if (string.IsNullOrEmpty(dir)) return;
        var fontsDir = Path.Combine(dir, "Typography", "Fonts");
        if (!Directory.Exists(fontsDir)) return;
        foreach (var ttf in Directory.GetFiles(fontsDir, "Liberation*.ttf"))
        {
            try
            {
                var bytes = File.ReadAllBytes(ttf);
                var font  = TtfFont.LoadFromBytes(bytes);
                var fam   = font.FontFamily.ToLowerInvariant();
                var wt    = NormalizeWeight(font.FontSubfamily ?? "Regular");
                Registry[$"{fam}:{wt}"] = bytes;
            }
            catch { /* ignore individual load failures */ }
        }
    }

    internal static string NormalizeWeight(string raw) =>
        raw.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
}
