using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BonniePDF.Drawing;
using BonniePDF.Models;
using BonniePDF.Typography;

namespace BonniePDF.Layout;

/// <summary>
/// Context for rendering PDF elements, managing pages, fonts, and positioning
/// </summary>
public class RenderContext
{
    private static readonly Random _random = new();

    private int _fontCounter;
    private int _imageCounter;

    public RenderContext(PdfSize size, float margin, bool isSvgMode = false)
    {
        IsSvgMode = isSvgMode;
        PageSize = size;
        Margin = margin;
        Pages.Add(new StringBuilder());
        CurrentX = Margin;
        CurrentY = Margin;

        UpdateCanvas();
    }

    /// <summary>
    /// Gets or sets whether PDF/A compliance is enabled.
    /// When true, standard font names are automatically resolved to embedded TTF fonts.
    /// </summary>
    public bool PdfAEnabled { get; set; }

    /// <summary>
    /// Maps standard font identifiers (e.g. "Helvetica", "Arial, sans-serif") to their
    /// resolved TTF file path when PdfAEnabled is true. Used to look up the correct
    /// EmbeddedFontInfo for fonts registered under their original name.
    /// </summary>
    internal Dictionary<string, string> FontAliasMap { get; } = new();

    /// <summary>
    /// Gets or sets whether the current page is the last page being rendered.
    /// Used by elements that need to know if more pages will follow.
    /// </summary>
    public bool IsLastPage { get; set; }

    public List<StringBuilder> Pages { get; } = new();

    // FONT MANAGER: Maps "Helvetica" -> "/F1", "Times" -> "/F2"
    // Also stores the Object ID for the writer later.
    public Dictionary<string, string> FontNameMap { get; } = new();
    public Dictionary<string, int> FontIdMap { get; } = new(); // To be filled by Writer

    /// <summary>
    /// Stores embedded font information (TTF fonts).
    /// Key: file path, Value: font info including tracker
    /// </summary>
    public Dictionary<string, EmbeddedFontInfo> EmbeddedFonts { get; } = new();

    /// <summary>
    /// Stores loaded image information
    /// Key: file path or data hash, Value: image info
    /// </summary>
    public Dictionary<string, ImageInfo> LoadedImages { get; } = new();

    /// <summary>
    /// Stores deferred Form XObjects (e.g. for Total Page count).
    /// Key: Signature (based on font/style), Value: Info
    /// </summary>
    public Dictionary<string, DeferredXObjectInfo> DeferredXObjects { get; } = new();

    /// <summary>
    /// Maps image keys to PDF object IDs
    /// </summary>
    public Dictionary<string, int> ImageIdMap { get; } = new();

    /// <summary>
    /// Maps image IDs to resource names (e.g., /I1, /I2)
    /// </summary>
    public Dictionary<int, string> ImageResourceNames { get; } = new();

    /// <summary>
    /// Stores hyperlinks per page index
    /// Key: page index, Value: list of hyperlinks on that page
    /// </summary>
    public Dictionary<int, List<Hyperlink>> PageHyperlinks { get; } = new();

    /// <summary>
    /// Gets the current drawing canvas.
    /// </summary>
    public ICanvas Canvas { get; private set; }

    /// <summary>
    /// Gets whether the context is in SVG rendering mode.
    /// </summary>
    public bool IsSvgMode { get; }

    public StringBuilder CurrentStream => Pages[CurrentPageIndex];
    public int CurrentPageIndex { get; private set; }

    public float CurrentX { get; set; }
    public float CurrentY { get; set; }
    public PdfSize PageSize { get; set; }
    public float Margin { get; set; }
    public float MaxY => PageSize.Height - Margin;

    /// <summary>
    /// Event raised when a new page is created.
    /// Used by templates to render headers/footers on each page.
    /// </summary>
    public event Action<int>? OnPageCreated;

    /// <summary>
    /// Updates the Canvas property to point to the current page's stream.
    /// </summary>
    private void UpdateCanvas()
    {
        if (IsSvgMode)
            Canvas = new SvgCanvas(PageSize.Width, PageSize.Height, 0, 0, CurrentStream, true);
        else
            Canvas = new PdfCanvas(CurrentStream, PageSize.Height, 0, 0, RegisterFont, ResolveImageResource,
                ResolveEmbeddedFont, TrackGlyphUsage);
    }

    /// <summary>
    /// Gets a canvas for a specific region or with specific offsets, writing to the current stream.
    /// </summary>
    public ICanvas GetCanvas(float x, float y)
    {
        if (IsSvgMode) return new SvgCanvas(PageSize.Width, PageSize.Height, x, y, CurrentStream, false);

        return new PdfCanvas(CurrentStream, PageSize.Height, x, y, RegisterFont, ResolveImageResource, ResolveEmbeddedFont, TrackGlyphUsage);
    }

    private string ResolveImageResource(ImageInfo info)
    {
        int id = RegisterImage(info);
        return GetImageResourceName(id);
    }

    /// <summary>
    /// Resolves an embedded font file path to its TtfFont instance.
    /// Returns null if the font is not embedded (i.e., it's a standard font).
    /// Also checks the font alias map for PDF/A auto-embedded fonts.
    /// </summary>
    internal TtfFont ResolveEmbeddedFont(string fontIdentifier)
    {
        // Direct TTF path
        if (IsEmbeddedFont(fontIdentifier))
        {
            EmbeddedFontInfo fontInfo = GetEmbeddedFont(fontIdentifier);
            return fontInfo?.Font;
        }

        // Check alias map (PDF/A auto-embedded standard fonts)
        if (FontAliasMap.TryGetValue(fontIdentifier, out string ttfPath))
        {
            EmbeddedFontInfo fontInfo = GetEmbeddedFont(ttfPath);
            return fontInfo?.Font;
        }

        return null;
    }

    /// <summary>
    /// Tracks glyph usage for font subsetting.
    /// This ensures that all used glyphs are included in the subset font.
    /// Also checks the font alias map for PDF/A auto-embedded fonts.
    /// </summary>
    internal void TrackGlyphUsage(string fontIdentifier, string text)
    {
        // Direct TTF path
        if (IsEmbeddedFont(fontIdentifier))
        {
            EmbeddedFontInfo fontInfo = GetEmbeddedFont(fontIdentifier);
            fontInfo?.Tracker.MarkUsed(text);
            return;
        }

        // Check alias map (PDF/A auto-embedded standard fonts)
        if (FontAliasMap.TryGetValue(fontIdentifier, out string ttfPath))
        {
            EmbeddedFontInfo fontInfo = GetEmbeddedFont(ttfPath);
            fontInfo?.Tracker.MarkUsed(text);
        }
    }

    /// <summary>
    /// Registers a font and returns its resource name (e.g., "/F1").
    /// Handles both standard fonts and embedded TTF fonts.
    /// When PdfAEnabled, standard fonts are auto-resolved to embedded TTF equivalents.
    /// </summary>
    public string RegisterFont(string fontIdentifier)
    {
        // Check if it's an embedded TTF font
        if (IsEmbeddedFont(fontIdentifier)) return RegisterEmbeddedFont(fontIdentifier);

        // PDF/A requires all fonts to be embedded — resolve standard font names to system TTF files
        if (PdfAEnabled)
        {
            string ttfPath = ResolveStandardFontToTtf(fontIdentifier);
            if (ttfPath != null)
            {
                string resourceName = RegisterEmbeddedFont(ttfPath);
                // Store alias so the original identifier maps to the same resource
                FontNameMap[fontIdentifier] = resourceName;
                FontAliasMap[fontIdentifier] = ttfPath;
                return resourceName;
            }
        }

        // Standard font handling
        if (!FontNameMap.ContainsKey(fontIdentifier))
        {
            _fontCounter++;
            FontNameMap[fontIdentifier] = $"/F{_fontCounter}";
        }

        return FontNameMap[fontIdentifier];
    }

    /// <summary>
    /// Checks if font identifier is an embedded TTF font
    /// </summary>
    public bool IsEmbeddedFont(string fontIdentifier)
    {
        return fontIdentifier.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
               fontIdentifier.EndsWith(".otf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets embedded font info for encoding text
    /// </summary>
    public EmbeddedFontInfo GetEmbeddedFont(string filePath)
    {
        return EmbeddedFonts.TryGetValue(filePath, out EmbeddedFontInfo info) ? info : null;
    }

    /// <summary>
    /// Registers an embedded TTF font
    /// </summary>
    private string RegisterEmbeddedFont(string filePath)
    {
        if (!EmbeddedFonts.ContainsKey(filePath))
        {
            TtfFont font = new(filePath);
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

    /// <summary>
    /// Resolves a standard font name to a system TTF file path for PDF/A embedding.
    /// Returns null if no matching system font is found.
    /// </summary>
    private static string ResolveStandardFontToTtf(string fontIdentifier)
    {
        string lower = fontIdentifier.ToLowerInvariant();

        // Map standard/CSS font names to Windows system TTF files
        string[] candidates;
        if (lower.Contains("courier") || lower.Contains("mono"))
            candidates = new[] { "cour.ttf", "consola.ttf" };
        else if (lower.Contains("times"))
            candidates = new[] { "times.ttf", "timesnewroman.ttf" };
        else // Arial, Helvetica, sans-serif, or any other
            candidates = new[] { "arial.ttf", "segoeui.ttf", "calibri.ttf" };

        string fontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
        foreach (string candidate in candidates)
        {
            string path = Path.Combine(fontsDir, candidate);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    /// <summary>
    /// Generates a 6-character random subset prefix (e.g., "ABCDEF").
    /// Required by PDF spec for subset fonts.
    /// </summary>
    private static string GenerateSubsetPrefix()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }

    /// <summary>
    /// Creates a new page and resets the cursor position
    /// </summary>
    public void NewPage()
    {
        Pages.Add(new StringBuilder());
        CurrentPageIndex++;
        CurrentX = Margin;
        CurrentY = Margin;
        UpdateCanvas();
        OnPageCreated?.Invoke(CurrentPageIndex);
    }

    /// <summary>
    /// Gets a canvas for fixed/absolute positioning on the current page.
    /// Used for headers, footers, and other elements that need to render
    /// at specific positions regardless of the current cursor position.
    /// </summary>
    /// <returns>A canvas positioned at (0, 0) on the current page</returns>
    public ICanvas GetFixedCanvas()
    {
        return GetCanvas(0, 0);
    }

    /// <summary>
    /// Temporarily sets the canvas to a specific instance.
    /// Useful for rendering templates or overlays that need a different coordinate system.
    /// </summary>
    public void SetCanvas(ICanvas canvas)
    {
        Canvas = canvas;
    }

    /// <summary>
    /// Switches to a specific page by index
    /// </summary>
    public void SwitchToPage(int index)
    {
        if (index < 0 || index >= Pages.Count)
            throw new ArgumentOutOfRangeException();
        CurrentPageIndex = index;
        UpdateCanvas();
    }

    /// <summary>
    /// Gets a new unique image ID
    /// </summary>
    public int GetNewImageId()
    {
        return ++_imageCounter;
    }

    /// <summary>
    /// Registers an image and returns its ID. Deduplicates based on content hash.
    /// </summary>
    public int RegisterImage(ImageInfo info)
    {
        byte[] dataToHash = info.OriginalData != null && info.OriginalData.Length > 0 ? info.OriginalData : info.Data;
        string key = "img_" + ComputeHash(dataToHash);

        if (!ImageIdMap.ContainsKey(key))
        {
            int imageId = GetNewImageId();
            ImageIdMap[key] = imageId;
            LoadedImages[key] = info;
        }

        return ImageIdMap[key];
    }

    private static string ComputeHash(byte[] data)
    {
        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    /// <summary>
    /// Gets the resource name for an image ID (e.g., /I1)
    /// </summary>
    public string GetImageResourceName(int imageId)
    {
        if (!ImageResourceNames.ContainsKey(imageId)) ImageResourceNames[imageId] = $"/I{imageId}";
        return ImageResourceNames[imageId];
    }

    /// <summary>
    /// Registers a hyperlink for the current page
    /// </summary>
    public void RegisterHyperlink(Hyperlink hyperlink)
    {
        if (!PageHyperlinks.ContainsKey(CurrentPageIndex)) PageHyperlinks[CurrentPageIndex] = new List<Hyperlink>();
        PageHyperlinks[CurrentPageIndex].Add(hyperlink);
    }

    /// <summary>
    /// Registers a deferred Form XObject request (e.g. for Total Pages).
    /// </summary>
    public string RegisterDeferredXObject(string fontFamily, float fontSize, Color color)
    {
        // Ensure font is registered so we have a valid resource name (e.g. /F1)
        string fontResName = RegisterFont(fontFamily);

        // Create signature based on style
        string signature = $"{fontResName}_{fontSize}_{color.ToHexString()}";

        if (!DeferredXObjects.TryGetValue(signature, out DeferredXObjectInfo info))
        {
            string resName = $"/DXO{DeferredXObjects.Count + 1}";
            info = new DeferredXObjectInfo
            {
                ResourceName = resName,
                FontIdentifier = fontFamily, // Store original identifier for ID lookup
                FontResourceName = fontResName,
                FontSize = fontSize,
                Color = color
            };
            DeferredXObjects[signature] = info;
        }

        return info.ResourceName;
    }
}

/// <summary>
/// Information about a deferred Form XObject to be generated at the end of the document.
/// </summary>
public class DeferredXObjectInfo
{
    public required string ResourceName { get; set; }
    public required string FontIdentifier { get; set; }
    public required string FontResourceName { get; set; }
    public float FontSize { get; set; }
    public required Color Color { get; set; }
}