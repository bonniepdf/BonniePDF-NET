using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Rendering.Images;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Rendering;

/// <summary>
/// Context for rendering PDF elements, managing pages, fonts, and positioning
/// </summary>
public class RenderContext
{
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

    public PdfResourceRegistry Resources { get; } = new();

    /// <summary>
    /// Gets or sets whether PDF/A compliance is enabled.
    /// When true, standard font names are automatically resolved to embedded TTF fonts.
    /// </summary>
    public bool PdfAEnabled
    {
        get => Resources.PdfAEnabled;
        set => Resources.PdfAEnabled = value;
    }

    /// <summary>
    /// Gets or sets whether the current page is the last page being rendered.
    /// Used by elements that need to know if more pages will follow.
    /// </summary>
    public bool IsLastPage { get; set; }

    public IList<StringBuilder> Pages { get; } = new List<StringBuilder>();

    /// <summary>
    /// Stores hyperlinks per page index
    /// Key: page index, Value: list of hyperlinks on that page
    /// </summary>
    public IDictionary<int, List<Hyperlink>> PageHyperlinks { get; } = new ConcurrentDictionary<int, List<Hyperlink>>();

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
        return Resources.GetImageResourceName(id);
    }

    internal TtfFont ResolveEmbeddedFont(string fontIdentifier) => Resources.ResolveEmbeddedFont(fontIdentifier);

    internal void TrackGlyphUsage(string fontIdentifier, string text) => Resources.TrackGlyphUsage(fontIdentifier, text);

    public string RegisterFont(string fontIdentifier) => Resources.RegisterFont(fontIdentifier);

    public static bool IsEmbeddedFont(string fontIdentifier) => PdfResourceRegistry.IsEmbeddedFont(fontIdentifier);

    public EmbeddedFontInfo GetEmbeddedFont(string filePath) => Resources.GetEmbeddedFont(filePath);

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

    public int GetNewImageId() => Resources.GetNewImageId();

    public int RegisterImage(ImageInfo info) => Resources.RegisterImage(info);

    public string GetImageResourceName(int imageId) => Resources.GetImageResourceName(imageId);

    /// <summary>
    /// Registers a hyperlink for the current page
    /// </summary>
    public void RegisterHyperlink(Hyperlink hyperlink)
    {
        if (!PageHyperlinks.ContainsKey(CurrentPageIndex)) PageHyperlinks[CurrentPageIndex] = new List<Hyperlink>();
        PageHyperlinks[CurrentPageIndex].Add(hyperlink);
    }

    public string RegisterDeferredXObject(string fontFamily, float fontSize, Color color) => Resources.RegisterDeferredXObject(fontFamily, fontSize, color);
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
