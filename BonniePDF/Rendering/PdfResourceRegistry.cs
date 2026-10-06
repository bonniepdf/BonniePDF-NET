using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Writers;
using BonniePDF.Typography.TrueType;
using BonniePDF.Rendering.Pdf;

namespace BonniePDF;

/// <summary>
/// A maximal substring of a draw call that renders with one font, after
/// glyph-fallback resolution. <see cref="EmbeddedFont"/> is <c>null</c> for
/// non-embedded (standard Type1) fonts, in which case the text is emitted as a
/// literal string rather than glyph IDs.
/// </summary>
internal sealed class ResolvedTextRun
{
    /// <summary>PDF resource name (e.g. <c>/F1</c>) of the font for this run.</summary>
    public required string ResourceName { get; init; }

    /// <summary>The embedded font, or <c>null</c> for a non-embedded standard Type1 font.</summary>
    public TtfFont? EmbeddedFont { get; init; }

    /// <summary>The font identifier used to track glyph usage on this run.</summary>
    public required string FontIdentifier { get; init; }

    /// <summary>The substring rendered by this run's font.</summary>
    public required string Text { get; init; }
}

/// <summary>
/// Manages PDF resources (fonts, images, XObjects) for a document.
/// Tracks used resources during rendering and handles their serialization.
/// </summary>
internal class PdfResourceRegistry
{
    private static readonly Random _random = new();

    /// <summary>Identifier prefix for glyph-fallback fonts registered on demand.</summary>
    private const string FallbackPrefix = "virtual:fallback:";

    private int _fontCounter;
    private int _imageCounter;
    private int _graphicsStateCounter;
    private int _shadingCounter;
    private int _softMaskGsCounter;

    private PdfAConformance _conformance = PdfAConformance.None;

    /// <summary>
    /// Gets or sets the PDF/A conformance level used by resource writers.
    /// </summary>
    public PdfAConformance Conformance
    {
        get => _conformance;
        set => _conformance = value;
    }

    /// <summary>
    /// Gets or sets whether PDF/A compliance is enabled.
    /// When true, standard font names are automatically resolved to embedded TTF fonts.
    /// </summary>
    public bool PdfAEnabled
    {
        get => _conformance != PdfAConformance.None;
        set => _conformance = value ? PdfAConformance.PdfA2b : PdfAConformance.None;
    }

    /// <summary>
    /// When true, standard-14 font references are substituted with embedded
    /// Liberation TTFs even without a PDF/A claim. PDF/UA-1 mandates font
    /// embedding (ISO 14289-1, veraPDF rule 7.21.4.1) independently of PDF/A,
    /// so the serializer sets this for tagged output. PDF/A implies it via
    /// <see cref="Conformance"/>.
    /// </summary>
    public bool EmbedStandardFonts { get; set; }

    /// <summary>
    /// Whether standard-14 names should be resolved to embedded TTFs. True for
    /// any PDF/A conformance, or when <see cref="EmbedStandardFonts"/> is set
    /// (PDF/UA-1).
    /// </summary>
    private bool ShouldEmbedStandardFonts => _conformance != PdfAConformance.None || EmbedStandardFonts;

    /// <summary>
    /// Collector that receives recoverable failures encountered while loading
    /// or embedding resources. Wired by Document; defaults to a private
    /// instance so non-Document callers still get a usable collector.
    /// </summary>
    public DiagnosticsCollector Diagnostics { get; set; } = new DiagnosticsCollector();

    internal Dictionary<string, string> FontAliasMap { get; } = new();
    
    /// <summary>
    /// Maps font identifiers to their PDF resource name (e.g., /F1).
    /// </summary>
    public IDictionary<string, string> FontNameMap { get; } = new ConcurrentDictionary<string, string>();
    
    /// <summary>
    /// Maps font identifiers to their PDF object ID.
    /// </summary>
    public IDictionary<string, int> FontIdMap { get; } = new ConcurrentDictionary<string, int>();

    /// <summary>
    /// Tracks embedded fonts for subsetting.
    /// </summary>
    public IDictionary<string, EmbeddedFontInfo> EmbeddedFonts { get; } = new ConcurrentDictionary<string, EmbeddedFontInfo>();

    /// <summary>
    /// Stores loaded images.
    /// </summary>
    public IDictionary<string, ImageInfo> LoadedImages { get; } = new ConcurrentDictionary<string, ImageInfo>();
    
    /// <summary>
    /// Maps image keys to their internal integer ID.
    /// </summary>
    public IDictionary<string, int> ImageIdMap { get; } = new ConcurrentDictionary<string, int>();
    
    /// <summary>
    /// Maps internal image IDs to their resource name (e.g., /I1).
    /// </summary>
    public IDictionary<int, string> ImageResourceNames { get; } = new ConcurrentDictionary<int, string>();

    // Map from ImageIdMap key to actual PDF object ID
    private readonly Dictionary<string, int> _imagePdfObjectIds = new();

    /// <summary>
    /// Stores deferred form XObjects like Total Pages indicators.
    /// </summary>
    public IDictionary<string, DeferredXObjectInfo> DeferredXObjects { get; } = new ConcurrentDictionary<string, DeferredXObjectInfo>();

    private readonly Dictionary<string, AlphaGraphicsStateInfo> _alphaGraphicsStates = new();
    private readonly List<ShadingResourceInfo> _shadings = new();

    private static readonly HashSet<string> StandardFontNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique",
        "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
        "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique",
        "Symbol", "ZapfDingbats",
    };

    /// <summary>
    /// Registers a font and returns its resource name (e.g., /F1).
    /// </summary>
    public string RegisterFont(string fontIdentifier)
    {
        // (A) Direct TTF/OTF file path
        if (IsEmbeddedFont(fontIdentifier))
        {
            // A face the document carries itself is in memory already (spec 16).
            byte[]? documentFontBytes = FontMetrics.GetDocumentFontBytes(fontIdentifier);
            return documentFontBytes is not null
                ? RegisterEmbeddedFontFromBytes(documentFontBytes, fontIdentifier)
                : RegisterEmbeddedFont(fontIdentifier);
        }

        // (B) Already registered as a virtual (bytes-based) font
        string virtualKey = $"virtual:{fontIdentifier}";
        if (EmbeddedFonts.ContainsKey(virtualKey))
            return EmbeddedFonts[virtualKey].ResourceName;

        if (ShouldEmbedStandardFonts)
        {
            // 1. Try FontManager (bytes-based - works in all environments)
            byte[]? bytes = FontManager.GetBytesForStandardFont(fontIdentifier);
            if (bytes != null)
            {
                string resourceName = RegisterEmbeddedFontFromBytes(bytes, virtualKey);
                FontNameMap[fontIdentifier] = resourceName;
                FontAliasMap[fontIdentifier] = virtualKey;
                // Register the parsed font with FontMetrics for accurate measurement.
                if (EmbeddedFonts.TryGetValue(virtualKey, out var info))
                    FontMetrics.RegisterFontInstance(fontIdentifier, info.Font);
                return resourceName;
            }

            // 2. Legacy: file-path based resolution (system fonts).
            // Skipped when InternalConfiguration.AllowSystemFontFallback is false
            // (used by tests that need to simulate a no-TTF environment).
            string? ttfPath = InternalConfiguration.AllowSystemFontFallback
                ? ResolveStandardFontToTtf(fontIdentifier)
                : null;
            if (ttfPath != null)
            {
                string resourceName = RegisterEmbeddedFont(ttfPath);
                FontNameMap[fontIdentifier] = resourceName;
                FontAliasMap[fontIdentifier] = ttfPath;
                FontMetrics.RegisterFontAlias(fontIdentifier, ttfPath);
                return resourceName;
            }
        }

        // (D) Standard type-1 font
        if (!FontNameMap.TryGetValue(fontIdentifier, out string? value))
        {
            _fontCounter++;
            value = $"/F{_fontCounter}";
            FontNameMap[fontIdentifier] = value;
        }

        return value;
    }

    /// <summary>
    /// Checks if a font is an embedded TrueType/OpenType font (or a virtual bytes-based key).
    /// </summary>
    public static bool IsEmbeddedFont(string fontIdentifier)
    {
        return fontIdentifier.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
               fontIdentifier.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
               fontIdentifier.StartsWith("virtual:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets embedded font info by file path.
    /// </summary>
    public EmbeddedFontInfo? GetEmbeddedFont(string filePath)
    {
        return EmbeddedFonts.TryGetValue(filePath, out EmbeddedFontInfo? info) ? info : null;
    }

    /// <summary>
    /// Reports whether this document will end up emitting any
    /// non-embedded standard-14 Type1 font reference (Helvetica,
    /// Times-*, Courier-*, Symbol, ZapfDingbats). Such references are
    /// <b>forbidden by PDF/A-1b §6.3.4</b>, so when this returns
    /// <c>true</c> the serializer suppresses the PDF/A conformance
    /// claim rather than emitting an invalid one.
    ///
    /// Mirrors what <see cref="WriteResources"/> does: anything that
    /// won't be aliased to a TTF and isn't already an embedded-font
    /// path will be written via <c>WriteType1Font</c> (non-embedded).
    /// </summary>
    public bool HasNonEmbeddedStandardFont() => GetNonEmbeddedStandardFonts().Count > 0;

    /// <summary>
    /// The fonts in the order they were first used, which is the order of their <c>/F&lt;n&gt;</c> resource names.
    /// <see cref="FontNameMap"/> is a concurrent dictionary, and its enumeration order follows the hashes of its keys:
    /// different in every process, and for a document font different in every document (its name carries the
    /// document's serial). Writing in that order made the same document produce differently ordered font objects.
    /// </summary>
    private IEnumerable<KeyValuePair<string, string>> FontsInRegistrationOrder() =>
        FontNameMap
            .OrderBy(entry => int.TryParse(entry.Value.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : int.MaxValue)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .ToList();

    /// <summary>The standard-14 font references that will be written without embedding.</summary>
    public IReadOnlyList<string> GetNonEmbeddedStandardFonts()
    {
        var result = new List<string>();
        foreach (string fontIdentifier in FontsInRegistrationOrder().Select(e => e.Key))
        {
            if (FontAliasMap.ContainsKey(fontIdentifier)) continue;
            if (IsEmbeddedFont(fontIdentifier)) continue;
            result.Add(fontIdentifier);
        }
        return result;
    }

    private string RegisterEmbeddedFontFromBytes(byte[] bytes, string key)
    {
        if (!EmbeddedFonts.ContainsKey(key))
        {
            TtfFont font = TtfFont.LoadFromBytesCached(bytes);
            GlyphUsageTracker tracker = new(font);

            _fontCounter++;
            string resourceName = $"/F{_fontCounter}";
            string subsetPrefix = GenerateSubsetPrefix();

            EmbeddedFonts[key] = new EmbeddedFontInfo
            {
                Font = font,
                Tracker = tracker,
                SubsetPrefix = subsetPrefix,
                ResourceName = resourceName
            };

            FontNameMap[key] = resourceName;
        }

        return FontNameMap[key];
    }

    private string RegisterEmbeddedFont(string filePath)
    {
        if (!EmbeddedFonts.TryGetValue(filePath, out EmbeddedFontInfo? existingFont))
        {
            TtfFont font = TtfFont.LoadFromFileCached(filePath);
            GlyphUsageTracker tracker = new(font);

            _fontCounter++;
            string resourceName = $"/F{_fontCounter}";
            string subsetPrefix = GenerateSubsetPrefix();

            EmbeddedFonts[filePath] = new EmbeddedFontInfo
            {
                Font = font,
                Tracker = tracker,
                SubsetPrefix = subsetPrefix,
                ResourceName = resourceName
            };

            FontNameMap[filePath] = resourceName;
        }

        return FontNameMap[filePath];
    }

    private static string? ResolveStandardFontToTtf(string fontIdentifier)
    {
        string lower = fontIdentifier.ToLowerInvariant();
        bool isBold = lower.Contains("bold");
        bool isItalic = lower.Contains("oblique") || lower.Contains("italic");

        string[] candidates;
        if (lower.Contains("courier") || lower.Contains("mono"))
        {
            candidates = (isBold, isItalic) switch
            {
                (true, true) => new[] { "courbi.ttf", "consolaz.ttf" },
                (true, false) => new[] { "courbd.ttf", "consolab.ttf" },
                (false, true) => new[] { "couri.ttf", "consolai.ttf" },
                _ => new[] { "cour.ttf", "consola.ttf" }
            };
        }
        else if (lower.Contains("times"))
        {
            candidates = (isBold, isItalic) switch
            {
                (true, true) => new[] { "timesbi.ttf" },
                (true, false) => new[] { "timesbd.ttf" },
                (false, true) => new[] { "timesi.ttf" },
                _ => new[] { "times.ttf", "timesnewroman.ttf" }
            };
        }
        else
        {
            candidates = (isBold, isItalic) switch
            {
                (true, true) => new[] { "arialbi.ttf", "segoeuiz.ttf", "calibriz.ttf" },
                (true, false) => new[] { "arialbd.ttf", "segoeuib.ttf", "calibrib.ttf" },
                (false, true) => new[] { "ariali.ttf", "segoeuii.ttf", "calibrii.ttf" },
                _ => new[] { "arial.ttf", "segoeui.ttf", "calibri.ttf" }
            };
        }

        string fontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
        foreach (string candidate in candidates)
        {
            string path = Path.Combine(fontsDir, candidate);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static string GenerateSubsetPrefix()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }

    internal TtfFont? ResolveEmbeddedFont(string fontIdentifier)
    {
        if (IsEmbeddedFont(fontIdentifier))
        {
            EmbeddedFontInfo? fontInfo = GetEmbeddedFont(fontIdentifier);
            return fontInfo?.Font;
        }

        // Check virtual (bytes-based) font
        string virtualKey = $"virtual:{fontIdentifier}";
        if (EmbeddedFonts.TryGetValue(virtualKey, out EmbeddedFontInfo? virtualFontInfo))
            return virtualFontInfo?.Font;

        if (FontAliasMap.TryGetValue(fontIdentifier, out string? ttfPath))
        {
            EmbeddedFontInfo? fontInfo = GetEmbeddedFont(ttfPath);
            return fontInfo?.Font;
        }

        return null;
    }

    internal void TrackGlyphUsage(string fontIdentifier, string text)
    {
        if (IsEmbeddedFont(fontIdentifier))
        {
            EmbeddedFontInfo? fontInfo = GetEmbeddedFont(fontIdentifier);
            fontInfo?.Tracker.MarkUsed(text);
            return;
        }

        // Check virtual (bytes-based) font
        string virtualKey = $"virtual:{fontIdentifier}";
        if (EmbeddedFonts.TryGetValue(virtualKey, out EmbeddedFontInfo? virtualFontInfo))
        {
            virtualFontInfo?.Tracker.MarkUsed(text);
            return;
        }

        if (FontAliasMap.TryGetValue(fontIdentifier, out string? ttfPath))
        {
            EmbeddedFontInfo? fontInfo = GetEmbeddedFont(ttfPath);
            fontInfo?.Tracker.MarkUsed(text);
        }
    }

    /// <summary>
    /// Resolves a draw call's text into one or more runs, each renderable by a
    /// single font. The primary font is used wherever it has the glyph; code
    /// points it lacks are routed to the first registered fallback font that
    /// covers them (registering it for embedding on demand). Control and
    /// default-ignorable code points are dropped, as are code points no font can
    /// render — both silently in output, the latter with a diagnostic — so the
    /// content stream never references the forbidden <c>.notdef</c> glyph.
    /// </summary>
    /// <remarks>
    /// When the primary font covers the whole string (or is a non-embedded
    /// Type1 font) the result is a single run identical to the legacy path.
    /// </remarks>
    public IList<ResolvedTextRun> ResolveTextRuns(string primaryFontIdentifier, string text)
    {
        string resourceName = RegisterFont(primaryFontIdentifier);
        TtfFont? primaryFont = ResolveEmbeddedFont(primaryFontIdentifier);

        // Non-embedded standard Type1 fonts can't be shaped (no cmap/glyph IDs);
        // preserve the literal-string emission path unchanged.
        if (primaryFont == null)
        {
            return new List<ResolvedTextRun>
            {
                new() { ResourceName = resourceName, EmbeddedFont = null, FontIdentifier = primaryFontIdentifier, Text = text }
            };
        }

        // candidates[0] is the primary font; fallbackByIndex is aligned to
        // candidates (index 0 is the primary and has no FallbackFont entry).
        var candidates = new List<ShaperFont> { new() { FontIdentifier = primaryFontIdentifier, Font = primaryFont } };
        var fallbackByIndex = new List<FallbackFont?> { null };
        // The document's own faces come first, then the global registrations (spec 16 §4).
        IEnumerable<FallbackFont> documentFallbacks = FontMetrics.ActiveDocumentFonts?.FallbackFonts() ?? [];
        foreach (FallbackFont fb in documentFallbacks.Concat(FontManager.GetFallbackFonts()))
        {
            if (fb.Font == primaryFont) continue;
            candidates.Add(new ShaperFont { FontIdentifier = $"{FallbackPrefix}{fb.Key}", Font = fb.Font });
            fallbackByIndex.Add(fb);
        }

        ShapeResult shaped = TextRunShaper.ShapeTextRuns(text, candidates);

        if (shaped.Unsupported.Count > 0)
        {
            string sample = string.Join(", ", shaped.Unsupported
                .Distinct()
                .Take(8)
                .Select(cp => $"U+{cp:X4}"));
            Diagnostics.AddWarning(
                DiagnosticCategory.Font,
                $"Dropped {shaped.Unsupported.Count} code point(s) with no glyph in the selected or fallback fonts ({sample}).",
                primaryFontIdentifier);
        }

        var result = new List<ResolvedTextRun>(shaped.Runs.Count);
        foreach (ShapedRun run in shaped.Runs)
        {
            if (run.FontIndex == 0)
            {
                result.Add(new ResolvedTextRun
                {
                    ResourceName = resourceName,
                    EmbeddedFont = primaryFont,
                    FontIdentifier = primaryFontIdentifier,
                    Text = run.Text
                });
                continue;
            }

            FallbackFont fb = fallbackByIndex[run.FontIndex]!;
            string fbIdentifier = candidates[run.FontIndex].FontIdentifier;
            string fbResource = RegisterEmbeddedFontFromBytes(fb.Bytes, fbIdentifier);
            TtfFont? fbFont = ResolveEmbeddedFont(fbIdentifier);
            result.Add(new ResolvedTextRun
            {
                ResourceName = fbResource,
                EmbeddedFont = fbFont,
                FontIdentifier = fbIdentifier,
                Text = run.Text
            });
        }

        return result;
    }

    /// <summary>
    /// Generates a new image internal ID.
    /// </summary>
    public int GetNewImageId()
    {
        return ++_imageCounter;
    }

    /// <summary>
    /// Registers an image and returns its internal ID, avoiding duplicates based on content hash.
    /// </summary>
    public int RegisterImage(ImageInfo info)
    {
        string key = ImageKey(info);

        if (!ImageIdMap.TryGetValue(key, out int existingImageId))
        {
            int imageId = GetNewImageId();
            ImageIdMap[key] = imageId;
            LoadedImages[key] = info;
            return imageId;
        }

        return existingImageId;
    }

    /// <summary>
    /// The resolution cap for embedded images, in pixels per inch; <c>null</c> embeds every image
    /// at its source resolution. Wired from the document's settings.
    /// </summary>
    public int? MaxImageDpi { get; set; }

    /// <summary>The largest size each image is painted at, per axis of the stored pixels.</summary>
    private sealed class ImagePlacements
    {
        public long MaxWidthMilliPoints;
        public long MaxHeightMilliPoints;
        public bool KeepResolution;
    }

    private readonly ConcurrentDictionary<string, ImagePlacements> _imagePlacements = new();

    /// <summary>
    /// Records that <paramref name="image"/> is painted <paramref name="widthPoints"/> x
    /// <paramref name="heightPoints"/> on the page (its display size, already scaled by any
    /// transform). One embedded copy serves every placement, so it keeps the largest size asked
    /// of it, and one placement that keeps full resolution keeps it for all of them.
    /// </summary>
    public void NoteImagePlacement(ImageInfo image, float widthPoints, float heightPoints)
    {
        // Lengths are along the stored pixel axes: EXIF orientations 5-8 swap them for display.
        (float storedWidth, float storedHeight) = image.Orientation is >= 5 and <= 8
            ? (heightPoints, widthPoints)
            : (widthPoints, heightPoints);

        long widthMilli = (long)Math.Round((double)storedWidth * 1000, MidpointRounding.AwayFromZero);
        long heightMilli = (long)Math.Round((double)storedHeight * 1000, MidpointRounding.AwayFromZero);

        ImagePlacements placements = _imagePlacements.GetOrAdd(ImageKey(image), _ => new ImagePlacements());
        lock (placements)
        {
            placements.MaxWidthMilliPoints = Math.Max(placements.MaxWidthMilliPoints, widthMilli);
            placements.MaxHeightMilliPoints = Math.Max(placements.MaxHeightMilliPoints, heightMilli);
            placements.KeepResolution |= image.KeepResolution;
        }
    }

    /// <summary>
    /// Returns the image to embed for <paramref name="key"/>: the source itself, or a reduced copy
    /// when the resolution cap applies. Decided from pixel counts and placement sizes alone, so the
    /// sync and async write paths, and the other language implementation, agree.
    /// </summary>
    private ImageInfo PrepareImageForWrite(string key, ImageInfo info)
    {
        if (MaxImageDpi is not int dpi || !_imagePlacements.TryGetValue(key, out ImagePlacements? placed))
            return info;
        if (placed.KeepResolution || placed.MaxWidthMilliPoints <= 0 || placed.MaxHeightMilliPoints <= 0)
            return info;

        int targetWidth = ImageResampler.TargetLength(placed.MaxWidthMilliPoints, dpi, info.Width);
        int targetHeight = ImageResampler.TargetLength(placed.MaxHeightMilliPoints, dpi, info.Height);
        if (!ImageResampler.ShouldResample(info.Width, info.Height, targetWidth, targetHeight))
            return info;

        string ppi(long pixels, long milliPoints) =>
            (pixels * 72000 / milliPoints).ToString(System.Globalization.CultureInfo.InvariantCulture);
        string drawn = $"{placed.MaxWidthMilliPoints / 1000.0:0.##} x {placed.MaxHeightMilliPoints / 1000.0:0.##} pt";

        if (!ImageResampler.IsEligible(info, out string reason))
        {
            Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, DiagnosticCategory.Image,
                $"image.passed-through: {info.Width}x{info.Height} px drawn {drawn} " +
                $"(about {ppi(info.Width, placed.MaxWidthMilliPoints)} ppi, cap {dpi}); embedded as it is: {reason}.", key));
            return info;
        }

        // The reduction depends only on the image and the target size, so documents that embed the same
        // image at the same size share one reduced copy.
        ImageInfo reduced = ImageCache.GetOrAdd($"r|{key}|{targetWidth}x{targetHeight}",
            () => ImageResampler.Resample(info, targetWidth, targetHeight));
        long samplesPerPixel = info.Components + (info.HasAlpha ? 1 : 0);
        long saved = ((long)info.Width * info.Height - (long)targetWidth * targetHeight) * samplesPerPixel;
        Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, DiagnosticCategory.Image,
            $"image.resampled: {info.Width}x{info.Height} -> {targetWidth}x{targetHeight} px, drawn {drawn} " +
            $"(about {ppi(info.Width, placed.MaxWidthMilliPoints)} -> {ppi(targetWidth, placed.MaxWidthMilliPoints)} ppi), " +
            $"{saved.ToString(System.Globalization.CultureInfo.InvariantCulture)} fewer raw bytes.", key));
        return reduced;
    }

    /// <summary>The content-hash key for an image, computed once per decoded image.</summary>
    private static string ImageKey(ImageInfo info) => info.ContentKey ??= ContentKeyOf(info);

    internal static string ContentKeyOf(ImageInfo info)
    {
        byte[] dataToHash = info.OriginalData != null && info.OriginalData.Length > 0 ? info.OriginalData : info.Data;
        return "img_" + ComputeHash(dataToHash);
    }

    private static string ComputeHash(byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    /// <summary>
    /// Gets the resource name for an image ID.
    /// </summary>
    public string GetImageResourceName(int imageId)
    {
        if (!ImageResourceNames.TryGetValue(imageId, out string? existingResourceName))
        {
            string resourceName = $"/I{imageId}";
            ImageResourceNames[imageId] = resourceName;
            return resourceName;
        }
        return existingResourceName;
    }

    /// <summary>
    /// Registers a deferred form XObject to be generated later, like total page count placeholders.
    /// </summary>
    public string RegisterDeferredXObject(string fontFamily, float fontSize, Color color)
    {
        string fontResName = RegisterFont(fontFamily);
        string signature = $"{fontResName}_{fontSize}_{color.ToHexString()}";

        if (!DeferredXObjects.TryGetValue(signature, out DeferredXObjectInfo? info))
        {
            string resName = $"/DXO{DeferredXObjects.Count + 1}";
            info = new DeferredXObjectInfo
            {
                ResourceName = resName,
                FontIdentifier = fontFamily,
                FontResourceName = fontResName,
                FontSize = fontSize,
                Color = color
            };
            DeferredXObjects[signature] = info;
        }

        return info.ResourceName;
    }

    /// <summary>
    /// Registers a reusable transparency graphics state and returns its PDF resource name.
    /// </summary>
    public string RegisterAlphaGraphicsState(float alpha)
    {
        float clamped = Math.Clamp(alpha, 0f, 1f);
        float effectiveAlpha = RequiresPdfA1TransparencyFallback() ? 1f : clamped;
        string key = effectiveAlpha.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);

        if (!_alphaGraphicsStates.TryGetValue(key, out AlphaGraphicsStateInfo? info))
        {
            _graphicsStateCounter++;
            info = new AlphaGraphicsStateInfo($"/GS_ALPHA_{_graphicsStateCounter}", effectiveAlpha);
            _alphaGraphicsStates[key] = info;
        }

        return info.ResourceName;
    }

    private bool RequiresPdfA1TransparencyFallback() =>
        _conformance is PdfAConformance.PdfA1b or PdfAConformance.PdfA1a;

    /// <summary>
    /// Registers a native gradient shading (axial Type-2 / radial Type-3) and, for
    /// translucent gradients, the luminosity soft mask that carries its alpha.
    /// Returns the resource names the content stream should reference, or <c>null</c>
    /// when the gradient can't be represented as a shading under the active
    /// conformance — specifically a translucent gradient under PDF/A-1, which forbids
    /// transparency (ISO 19005-1 §6.4). The caller falls back to the strip/disc
    /// approximation in that case.
    ///
    /// Opaque gradients are always emitted as shadings (smooth shadings are valid in
    /// every PDF/A level); only the soft mask is gated on transparency support.
    /// </summary>
    public ShadingResources? RegisterShading(ShadingRequest request)
    {
        if (request.HasAlpha && RequiresPdfA1TransparencyFallback()) return null;

        _shadingCounter++;
        string shadingName = $"/Sh{_shadingCounter}";
        string? softMaskGsName = null;
        if (request.HasAlpha)
        {
            _softMaskGsCounter++;
            softMaskGsName = $"/GSsm{_softMaskGsCounter}";
        }
        _shadings.Add(new ShadingResourceInfo(shadingName, softMaskGsName, request));
        return new ShadingResources(shadingName, softMaskGsName);
    }

    private static string MapToStandardFontName(string fontIdentifier)
    {
        string lower = fontIdentifier.ToLowerInvariant();
        return lower switch
        {
            var s when s.Contains("arial") || s.Contains("helvetica") => StandardFonts.Helvetica,
            var s when s.Contains("times") => StandardFonts.TimesRoman,
            var s when s.Contains("courier") || s.Contains("mono") => StandardFonts.Courier,
            _ => StandardFonts.Helvetica
        };
    }

    private int ResolveFontObjectId(string fontIdentifier)
    {
        if (FontAliasMap.TryGetValue(fontIdentifier, out string? ttfPath) &&
            FontIdMap.TryGetValue(ttfPath, out int ttfFontId))
            return ttfFontId;
        return FontIdMap.TryGetValue(fontIdentifier, out int fontId) ? fontId : 0;
    }

    /// <summary>
    /// Writes all resources (fonts, images, deferred xobjects) and returns the /Resources dictionary string.
    /// </summary>
    public string WriteResources(IPdfWriter writer, int totalPages)
    {
        FontWriter fontWriter = new(writer);
        ImageWriter imageWriter = new(writer) { Conformance = Conformance };

        // 1. Write Fonts
        foreach (KeyValuePair<string, string> fontEntry in FontsInRegistrationOrder())
        {
            string fontIdentifier = fontEntry.Key;

            // Skip file-path aliases (already written under the TTF path key)
            if (FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            // Skip logical names that were mapped to a virtual (bytes-based) font - written under "virtual:" key
            if (EmbeddedFonts.ContainsKey($"virtual:{fontIdentifier}"))
                continue;

            if (IsEmbeddedFont(fontIdentifier))
            {
                if (EmbeddedFonts.TryGetValue(fontIdentifier, out EmbeddedFontInfo? fontInfo))
                {
                    int type0Id = writer.GetNewId();
                    fontInfo.Type0ObjectId = type0Id;
                    FontIdMap[fontIdentifier] = type0Id;
                    fontWriter.WriteEmbeddedTtfFont(type0Id, fontInfo);
                }
            }
            else
            {
                int fontId = writer.GetNewId();
                FontIdMap[fontIdentifier] = fontId;
                fontWriter.WriteType1Font(fontId, fontIdentifier);
            }
        }

        // 2. Write Images
        _imagePdfObjectIds.Clear();
        foreach (KeyValuePair<string, ImageInfo> imageEntry in LoadedImages)
        {
            string key = imageEntry.Key;
            ImageInfo info = imageEntry.Value;

            int pdfObjectId = writer.GetNewId();
            _imagePdfObjectIds[key] = pdfObjectId;

            int internalId = ImageIdMap[key];
            string resourceName = GetImageResourceName(internalId);

            imageWriter.WriteImage(pdfObjectId, PrepareImageForWrite(key, info), resourceName);
        }

        // 3. Write Deferred XObjects
        Dictionary<string, int> deferredIds = new();
        foreach (KeyValuePair<string, DeferredXObjectInfo> kvp in DeferredXObjects)
        {
            DeferredXObjectInfo info = kvp.Value;
            int id = writer.GetNewId();
            deferredIds[kvp.Key] = id;

            string text = totalPages.ToString();
            string colorCmd = $"{info.Color.ScR:F3} {info.Color.ScG:F3} {info.Color.ScB:F3} rg";

            TtfFont? embeddedFont = ResolveEmbeddedFont(info.FontIdentifier);
            string textOperand;
            if (embeddedFont != null)
            {
                TrackGlyphUsage(info.FontIdentifier, text);
                textOperand = PdfSyntax.ToGlyphIdHexString(text, embeddedFont);
            }
            else
            {
                textOperand = $"({PdfSyntax.Escape(text)})";
            }

            string content =
                $"BT {info.FontResourceName} {info.FontSize} Tf {colorCmd} 0 -{info.FontSize} Td {textOperand} Tj ET";

            Dictionary<string, string> dict = new()
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Form",
                ["/BBox"] = $"[0 -{info.FontSize * 2} 100 0]",
                ["/Resources"] = $"<< /Font << {info.FontResourceName} {ResolveFontObjectId(info.FontIdentifier)} 0 R >> >>"
            };

            writer.WriteObject(id, dict, content);
        }

        // 4. Build Resource String
        List<string> fontRefs = new();
        foreach (KeyValuePair<string, string> fontEntry in FontsInRegistrationOrder())
        {
            string fontIdentifier = fontEntry.Key;
            string resourceName = fontEntry.Value;

            // Skip file-path aliases (written under the TTF path key)
            if (FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            // Skip logical names that were mapped to a virtual (bytes-based) font - handled under "virtual:" key
            if (EmbeddedFonts.ContainsKey($"virtual:{fontIdentifier}"))
                continue;

            // For virtual: keys, resolve the object ID via the virtual key itself
            int objectId = 0;
            if (fontIdentifier.StartsWith("virtual:", StringComparison.OrdinalIgnoreCase))
            {
                if (FontIdMap.TryGetValue(fontIdentifier, out objectId))
                    fontRefs.Add($"{resourceName} {objectId} 0 R");
            }
            else if (FontIdMap.TryGetValue(fontIdentifier, out objectId))
            {
                fontRefs.Add($"{resourceName} {objectId} 0 R");
            }
        }
        string fontResources = "<< /Font << " + string.Join(' ', fontRefs) + " >> >>";

        List<string> xobjRefs = new();
        if (ImageIdMap.Count > 0)
            foreach (KeyValuePair<string, int> imageEntry in ImageIdMap)
            {
                string key = imageEntry.Key;
                int internalId = imageEntry.Value;

                if (_imagePdfObjectIds.TryGetValue(key, out int pdfObjectId))
                {
                    string resourceName = GetImageResourceName(internalId);
                    xobjRefs.Add($"{resourceName} {pdfObjectId} 0 R");
                }
            }
        if (deferredIds.Count > 0)
            foreach (KeyValuePair<string, int> kvp in deferredIds)
            {
                string signature = kvp.Key;
                int objectId = kvp.Value;

                if (DeferredXObjects.TryGetValue(signature, out DeferredXObjectInfo? info))
                    xobjRefs.Add($"{info.ResourceName} {objectId} 0 R");
            }

        string imageResources = xobjRefs.Count > 0 ? "<< /XObject << " + string.Join(' ', xobjRefs) + " >> >>" : "";

        List<string> graphicsStateRefs = new();
        foreach (AlphaGraphicsStateInfo info in _alphaGraphicsStates.Values)
        {
            int objectId = writer.GetNewId();
            // info.Alpha is already conformance-aware: RegisterAlphaGraphicsState()
            // forces it to 1.0 only for PDF/A-1 (ISO 19005-1 §6.4 forbids
            // transparency). PDF/A-2/3/4 permit constant alpha (ISO 19005-2 §6.2.4),
            // so they keep their real opacity - mirroring image soft-mask handling.
            float alphaValue = info.Alpha;
            writer.WriteObject(objectId, new Dictionary<string, string>
            {
                ["/Type"] = "/ExtGState",
                ["/ca"] = alphaValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
                ["/CA"] = alphaValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)
            });
            graphicsStateRefs.Add($"{info.ResourceName} {objectId} 0 R");
        }

        // Gradient shadings (native axial/radial) and their luminosity soft masks.
        // The colour shading is an inline dict in /Shading; a translucent gradient
        // additionally needs a form XObject carrying the grayscale alpha shading,
        // wrapped by an /SMask ExtGState that joins the page's /ExtGState dict.
        List<string> shadingRefs = new();
        foreach (ShadingResourceInfo shading in _shadings)
        {
            shadingRefs.Add($"{shading.ShadingName} {GradientShading.BuildColorShadingDict(shading.Request)}");
            if (shading.SoftMaskGsName == null) continue;

            int formId = writer.GetNewId();
            writer.WriteObject(formId, BuildSoftMaskFormDict(shading.Request), "/ShM sh\n");

            int gsId = writer.GetNewId();
            writer.WriteObject(gsId, BuildSoftMaskGsDict(formId));
            graphicsStateRefs.Add($"{shading.SoftMaskGsName} {gsId} 0 R");
        }
        string shadingResources = shadingRefs.Count > 0
            ? "<< /Shading << " + string.Join(' ', shadingRefs) + " >> >>"
            : "";

        string graphicsStateResources = graphicsStateRefs.Count > 0
            ? "<< /ExtGState << " + string.Join(' ', graphicsStateRefs) + " >> >>"
            : "";

        return CombineResources(fontResources, imageResources, graphicsStateResources, shadingResources);
    }

    /// <summary>
    /// Writes all resources (fonts, images, deferred xobjects) asynchronously and returns the /Resources dictionary string.
    /// </summary>
    public async Task<string> WriteResourcesAsync(IAsyncPdfWriter writer, int totalPages, CancellationToken cancellationToken = default)
    {
        FontWriter fontWriter = new(writer);
        ImageWriter imageWriter = new(writer) { Conformance = Conformance };

        // 1. Write Fonts
        foreach (KeyValuePair<string, string> fontEntry in FontsInRegistrationOrder())
        {
            string fontIdentifier = fontEntry.Key;

            // Skip file-path aliases (already written under the TTF path key)
            if (FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            // Skip logical names that were mapped to a virtual (bytes-based) font - written under "virtual:" key
            if (EmbeddedFonts.ContainsKey($"virtual:{fontIdentifier}"))
                continue;

            if (IsEmbeddedFont(fontIdentifier))
            {
                if (EmbeddedFonts.TryGetValue(fontIdentifier, out EmbeddedFontInfo? fontInfo))
                {
                    int type0Id = writer.GetNewId();
                    fontInfo.Type0ObjectId = type0Id;
                    FontIdMap[fontIdentifier] = type0Id;
                    await fontWriter.WriteEmbeddedTtfFontAsync(type0Id, fontInfo, true, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                int fontId = writer.GetNewId();
                FontIdMap[fontIdentifier] = fontId;
                await fontWriter.WriteType1FontAsync(fontId, fontIdentifier, cancellationToken).ConfigureAwait(false);
            }
        }

        // 2. Write Images
        _imagePdfObjectIds.Clear();
        foreach (KeyValuePair<string, ImageInfo> imageEntry in LoadedImages)
        {
            string key = imageEntry.Key;
            ImageInfo info = imageEntry.Value;

            int pdfObjectId = writer.GetNewId();
            _imagePdfObjectIds[key] = pdfObjectId;

            int internalId = ImageIdMap[key];
            string resourceName = GetImageResourceName(internalId);

            await imageWriter.WriteImageAsync(pdfObjectId, PrepareImageForWrite(key, info), resourceName, cancellationToken).ConfigureAwait(false);
        }

        // 3. Write Deferred XObjects
        Dictionary<string, int> deferredIds = new();
        foreach (KeyValuePair<string, DeferredXObjectInfo> kvp in DeferredXObjects)
        {
            DeferredXObjectInfo info = kvp.Value;
            int id = writer.GetNewId();
            deferredIds[kvp.Key] = id;

            string text = totalPages.ToString();
            string colorCmd = $"{info.Color.ScR:F3} {info.Color.ScG:F3} {info.Color.ScB:F3} rg";

            TtfFont? embeddedFont = ResolveEmbeddedFont(info.FontIdentifier);
            string textOperand;
            if (embeddedFont != null)
            {
                TrackGlyphUsage(info.FontIdentifier, text);
                textOperand = PdfSyntax.ToGlyphIdHexString(text, embeddedFont);
            }
            else
            {
                textOperand = $"({PdfSyntax.Escape(text)})";
            }

            string content =
                $"BT {info.FontResourceName} {info.FontSize} Tf {colorCmd} 0 -{info.FontSize} Td {textOperand} Tj ET";

            Dictionary<string, string> dict = new()
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Form",
                ["/BBox"] = $"[0 -{info.FontSize * 2} 100 0]",
                ["/Resources"] = $"<< /Font << {info.FontResourceName} {ResolveFontObjectId(info.FontIdentifier)} 0 R >> >>"
            };

            await writer.WriteObjectAsync(id, dict, content, cancellationToken).ConfigureAwait(false);
        }

        // 4. Build Resource String
        List<string> fontRefs = new();
        foreach (KeyValuePair<string, string> fontEntry in FontsInRegistrationOrder())
        {
            string fontIdentifier = fontEntry.Key;
            string resourceName = fontEntry.Value;

            // Skip file-path aliases
            if (FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            // Skip logical names that were mapped to a virtual (bytes-based) font
            if (EmbeddedFonts.ContainsKey($"virtual:{fontIdentifier}"))
                continue;

            // For virtual: keys, resolve the object ID via the virtual key itself
            int objectId = 0;
            if (fontIdentifier.StartsWith("virtual:", StringComparison.OrdinalIgnoreCase))
            {
                if (FontIdMap.TryGetValue(fontIdentifier, out objectId))
                    fontRefs.Add($"{resourceName} {objectId} 0 R");
            }
            else if (FontIdMap.TryGetValue(fontIdentifier, out objectId))
            {
                fontRefs.Add($"{resourceName} {objectId} 0 R");
            }
        }
        string fontResources = "<< /Font << " + string.Join(' ', fontRefs) + " >> >>";

        List<string> xobjRefs = new();
        if (ImageIdMap.Count > 0)
            foreach (KeyValuePair<string, int> imageEntry in ImageIdMap)
            {
                string key = imageEntry.Key;
                int internalId = imageEntry.Value;

                if (_imagePdfObjectIds.TryGetValue(key, out int pdfObjectId))
                {
                    string resourceName = GetImageResourceName(internalId);
                    xobjRefs.Add($"{resourceName} {pdfObjectId} 0 R");
                }
            }
        if (deferredIds.Count > 0)
            foreach (KeyValuePair<string, int> kvp in deferredIds)
            {
                string signature = kvp.Key;
                int objectId = kvp.Value;

                if (DeferredXObjects.TryGetValue(signature, out DeferredXObjectInfo? info))
                    xobjRefs.Add($"{info.ResourceName} {objectId} 0 R");
            }

        string imageResources = xobjRefs.Count > 0 ? "<< /XObject << " + string.Join(' ', xobjRefs) + " >> >>" : "";

        List<string> graphicsStateRefs = new();
        foreach (AlphaGraphicsStateInfo info in _alphaGraphicsStates.Values)
        {
            int objectId = writer.GetNewId();
            // info.Alpha is already conformance-aware: RegisterAlphaGraphicsState()
            // forces it to 1.0 only for PDF/A-1 (ISO 19005-1 §6.4 forbids
            // transparency). PDF/A-2/3/4 permit constant alpha (ISO 19005-2 §6.2.4),
            // so they keep their real opacity - mirroring image soft-mask handling.
            float alphaValue = info.Alpha;
            await writer.WriteObjectAsync(objectId, new Dictionary<string, string>
            {
                ["/Type"] = "/ExtGState",
                ["/ca"] = alphaValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
                ["/CA"] = alphaValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)
            }, null, cancellationToken).ConfigureAwait(false);
            graphicsStateRefs.Add($"{info.ResourceName} {objectId} 0 R");
        }

        // Gradient shadings (native axial/radial) and their luminosity soft masks.
        List<string> shadingRefs = new();
        foreach (ShadingResourceInfo shading in _shadings)
        {
            shadingRefs.Add($"{shading.ShadingName} {GradientShading.BuildColorShadingDict(shading.Request)}");
            if (shading.SoftMaskGsName == null) continue;

            int formId = writer.GetNewId();
            await writer.WriteObjectAsync(formId, BuildSoftMaskFormDict(shading.Request), "/ShM sh\n", cancellationToken)
                .ConfigureAwait(false);

            int gsId = writer.GetNewId();
            await writer.WriteObjectAsync(gsId, BuildSoftMaskGsDict(formId), null, cancellationToken).ConfigureAwait(false);
            graphicsStateRefs.Add($"{shading.SoftMaskGsName} {gsId} 0 R");
        }
        string shadingResources = shadingRefs.Count > 0
            ? "<< /Shading << " + string.Join(' ', shadingRefs) + " >> >>"
            : "";

        string graphicsStateResources = graphicsStateRefs.Count > 0
            ? "<< /ExtGState << " + string.Join(' ', graphicsStateRefs) + " >> >>"
            : "";

        return CombineResources(fontResources, imageResources, graphicsStateResources, shadingResources);
    }

    /// <summary>
    /// Builds the luminosity soft-mask form XObject dict for a translucent shading:
    /// a transparency group (DeviceGray) whose content paints the gradient's grayscale
    /// alpha shading across the form's bounding box.
    /// </summary>
    private static IDictionary<string, string> BuildSoftMaskFormDict(ShadingRequest request)
    {
        string Fmt(float v) => MathF.Round(v, 4).ToString(System.Globalization.CultureInfo.InvariantCulture);
        float[] b = request.Bbox;
        return new Dictionary<string, string>
        {
            ["/Type"] = "/XObject",
            ["/Subtype"] = "/Form",
            ["/FormType"] = "1",
            ["/BBox"] = $"[{Fmt(b[0])} {Fmt(b[1])} {Fmt(b[2])} {Fmt(b[3])}]",
            ["/Group"] = "<< /Type /Group /S /Transparency /CS /DeviceGray /I true >>",
            ["/Resources"] = $"<< /Shading << /ShM {GradientShading.BuildAlphaShadingDict(request)} >> >>"
        };
    }

    /// <summary>Builds the <c>/SMask /S /Luminosity</c> ExtGState dict referencing a soft-mask form.</summary>
    private static IDictionary<string, string> BuildSoftMaskGsDict(int formId) =>
        new Dictionary<string, string>
        {
            ["/Type"] = "/ExtGState",
            ["/SMask"] = $"<< /Type /Mask /S /Luminosity /G {formId} 0 R >>",
            ["/ca"] = "1",
            ["/CA"] = "1"
        };

    private static string CombineResources(params string[] resources)
    {
        List<string> parts = new();

        foreach (string resource in resources)
        {
            if (string.IsNullOrWhiteSpace(resource)) continue;

            string content = resource.Trim();
            if (content.StartsWith("<<", StringComparison.OrdinalIgnoreCase))
                content = content.Substring(2).TrimStart();
            if (content.EndsWith(">>", StringComparison.OrdinalIgnoreCase))
                content = content.Substring(0, content.Length - 2).TrimEnd();

            if (!string.IsNullOrWhiteSpace(content)) parts.Add(content);
        }

        return parts.Count == 0 ? "<< >>" : "<< " + string.Join(' ', parts) + " >>";
    }

    private sealed record AlphaGraphicsStateInfo(string ResourceName, float Alpha);

    private sealed record ShadingResourceInfo(string ShadingName, string? SoftMaskGsName, ShadingRequest Request);
}

/// <summary>Resource names a content stream references to paint a registered gradient shading.</summary>
/// <param name="ShadingName">The <c>/Shading</c> resource name (e.g. <c>/Sh1</c>) for the <c>sh</c> operator.</param>
/// <param name="SoftMaskGs">The luminosity soft-mask <c>/ExtGState</c> name (e.g. <c>/GSsm1</c>), or <c>null</c> for an opaque gradient.</param>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
internal readonly record struct ShadingResources(string ShadingName, string? SoftMaskGs);
