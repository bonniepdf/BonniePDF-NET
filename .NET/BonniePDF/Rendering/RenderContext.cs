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
    /// <summary>
    /// Initializes a new <see cref="RenderContext"/> for a single document.
    /// </summary>
    /// <param name="size">The page size (width and height in points).</param>
    /// <param name="margin">The uniform page margin in points.</param>
    /// <param name="isSvgMode">When <c>true</c>, the context emits SVG instead of PDF operators.</param>
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
    /// Gets the shared PDF resource registry that tracks fonts, images, and other PDF resources
    /// for the document being rendered.
    /// </summary>
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

    /// <summary>
    /// Gets the list of page content streams, one <see cref="StringBuilder"/> per page.
    /// </summary>
    public IList<StringBuilder> Pages { get; } = new List<StringBuilder>();

    /// <summary>
    /// Stores hyperlinks per page index
    /// Key: page index, Value: list of hyperlinks on that page
    /// </summary>
    public IDictionary<int, List<Hyperlink>> PageHyperlinks { get; } = new ConcurrentDictionary<int, List<Hyperlink>>();

    /// <summary>
    /// Stores the resolved page numbers for Table of Contents entries.
    /// Key: TOC Identifier, Value: 1-based page number.
    /// </summary>
    public IDictionary<string, int> TocEntries { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Gets the current drawing canvas.
    /// </summary>
    public ICanvas Canvas { get; private set; }

    /// <summary>
    /// Gets whether the context is in SVG rendering mode.
    /// </summary>
    public bool IsSvgMode { get; }

    /// <summary>
    /// Gets the content stream for the page currently being rendered.
    /// </summary>
    public StringBuilder CurrentStream => Pages[CurrentPageIndex];

    /// <summary>
    /// Gets the zero-based index of the page currently being rendered.
    /// </summary>
    public int CurrentPageIndex { get; private set; }

    /// <summary>
    /// Gets or sets the current horizontal cursor position in points from the left edge.
    /// </summary>
    public float CurrentX { get; set; }

    /// <summary>
    /// Gets or sets the current vertical cursor position in points from the top edge.
    /// </summary>
    public float CurrentY { get; set; }

    /// <summary>
    /// Gets or sets the size of the current page in points.
    /// </summary>
    public PdfSize PageSize { get; set; }

    /// <summary>
    /// Gets or sets the uniform page margin in points applied to all four sides.
    /// </summary>
    public float Margin { get; set; }

    /// <summary>
    /// Gets the maximum Y coordinate before the bottom margin is reached.
    /// Elements that exceed this value trigger a page break.
    /// </summary>
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

    /// <summary>
    /// Resolves the embedded <see cref="TtfFont"/> for the given font identifier,
    /// or <c>null</c> if the font is not embedded (i.e. it is a standard PDF font).
    /// </summary>
    internal TtfFont ResolveEmbeddedFont(string fontIdentifier) => Resources.ResolveEmbeddedFont(fontIdentifier);

    /// <summary>
    /// Records the characters in <paramref name="text"/> as used by the specified font,
    /// enabling accurate glyph subsetting when the PDF is written.
    /// </summary>
    internal void TrackGlyphUsage(string fontIdentifier, string text) => Resources.TrackGlyphUsage(fontIdentifier, text);

    /// <summary>
    /// Registers a font by its identifier and returns the PDF resource name (e.g. <c>/F1</c>)
    /// to use in content stream operators.
    /// </summary>
    public string RegisterFont(string fontIdentifier) => Resources.RegisterFont(fontIdentifier);

    /// <summary>
    /// Returns <c>true</c> if the given font identifier refers to an embedded TrueType font
    /// rather than one of the 14 standard PDF fonts.
    /// </summary>
    public static bool IsEmbeddedFont(string fontIdentifier) => PdfResourceRegistry.IsEmbeddedFont(fontIdentifier);

    /// <summary>
    /// Returns the <see cref="EmbeddedFontInfo"/> for a font loaded from the given file path,
    /// loading and caching it on first access.
    /// </summary>
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

    /// <summary>
    /// Allocates and returns a new unique image object ID for use in the PDF cross-reference table.
    /// </summary>
    public int GetNewImageId() => Resources.GetNewImageId();

    /// <summary>
    /// Registers an image with the resource registry and returns its assigned object ID.
    /// Identical images are de-duplicated automatically.
    /// </summary>
    public int RegisterImage(ImageInfo info) => Resources.RegisterImage(info);

    /// <summary>
    /// Returns the PDF resource name (e.g. <c>/Im1</c>) for a previously registered image.
    /// </summary>
    public string GetImageResourceName(int imageId) => Resources.GetImageResourceName(imageId);

    /// <summary>
    /// Registers a hyperlink for the current page
    /// </summary>
    public void RegisterHyperlink(Hyperlink hyperlink)
    {
        if (!PageHyperlinks.ContainsKey(CurrentPageIndex)) PageHyperlinks[CurrentPageIndex] = new List<Hyperlink>();
        PageHyperlinks[CurrentPageIndex].Add(hyperlink);
    }

    /// <summary>
    /// Registers a deferred Form XObject for a watermark or overlay pattern and returns
    /// its PDF resource name. The XObject content is generated at the end of the document.
    /// </summary>
    public string RegisterDeferredXObject(string fontFamily, float fontSize, Color color) => Resources.RegisterDeferredXObject(fontFamily, fontSize, color);
}

/// <summary>
/// Information about a deferred Form XObject to be generated at the end of the document.
/// </summary>
public class DeferredXObjectInfo
{
    /// <summary>Gets or sets the PDF resource name (e.g. <c>/XO1</c>) assigned to this XObject.</summary>
    public required string ResourceName { get; set; }

    /// <summary>Gets or sets the font identifier used within this XObject's content stream.</summary>
    public required string FontIdentifier { get; set; }

    /// <summary>Gets or sets the PDF font resource name (e.g. <c>/F1</c>) for text drawing operations.</summary>
    public required string FontResourceName { get; set; }

    /// <summary>Gets or sets the font size in points used to render text within this XObject.</summary>
    public float FontSize { get; set; }

    /// <summary>Gets or sets the fill color applied to text within this XObject.</summary>
    public required Color Color { get; set; }
}
