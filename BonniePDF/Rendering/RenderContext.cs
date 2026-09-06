using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Rendering.Images;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Rendering;

/// <summary>
/// PDF tag names for roles that are container/grouping elements and must
/// emit BMC (no MCID) instead of BDC in the content stream.
/// Mirrors <see cref="BonniePDF.Rendering.Tagging.SemanticRoleExtensions.IsContainerRole"/>.
/// </summary>
file static class ContainerRoleTags
{
    internal static readonly HashSet<string> Tags = new(StringComparer.Ordinal)
    {
        "Table", "TR", "THead", "TBody", "TFoot",
        "Document", "Sect", "Art", "Div", "TOC", "L", "LI",
    };
}

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
        : this(size, margin, isSvgMode, SvgRenderOptions.Legacy, false)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="RenderContext"/> for SVG export with explicit
    /// low-level options.
    /// </summary>
    /// <param name="size">The page size (width and height in points).</param>
    /// <param name="margin">The uniform page margin in points.</param>
    /// <param name="isSvgMode">When <c>true</c>, the context emits SVG instead of PDF operators.</param>
    /// <param name="svgOptions">Low-level SVG canvas options (precision, baseline parity, image embedding).</param>
    /// <param name="svgContentOnly">
    /// When <c>true</c>, SVG canvases emit page-body markup without an enclosing
    /// <c>&lt;svg&gt;</c> header, so the page export pipeline can wrap each page in
    /// a standalone, deterministic root element.
    /// </param>
    public RenderContext(PdfSize size, float margin, bool isSvgMode, SvgRenderOptions svgOptions, bool svgContentOnly)
    {
        IsSvgMode = isSvgMode;
        SvgOptions = svgOptions ?? SvgRenderOptions.Legacy;
        _svgContentOnly = svgContentOnly;
        PageSize = size;
        Margin = margin;
        Pages.Add(new StringBuilder());
        CurrentX = Margin;
        CurrentY = Margin;

        UpdateCanvas();
    }

    /// <summary>Low-level SVG canvas options threaded into every <see cref="SvgCanvas"/> this context creates.</summary>
    public SvgRenderOptions SvgOptions { get; } = SvgRenderOptions.Legacy;

    /// <summary>
    /// Shared across every <see cref="SvgCanvas"/> this context creates (main
    /// page canvases and positioned child canvases alike), so <c>clip</c>/<c>grad</c>
    /// ids stay unique across a page composited from several canvases - see
    /// <see cref="SvgIdAllocator"/>.
    /// </summary>
    private readonly SvgIdAllocator _svgIdAllocator = new();

    private readonly bool _svgContentOnly;

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
    /// Gets or sets the PDF/A conformance level used by resource writers.
    /// </summary>
    public PdfAConformance Conformance
    {
        get => Resources.Conformance;
        set => Resources.Conformance = value;
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
    // Always assigned by UpdateCanvas(), called unconditionally from every
    // constructor; null! documents that to the compiler across the call boundary.
    public ICanvas Canvas { get; private set; } = null!;

    /// <summary>
    /// PDF/UA tagging context. Non-null when the document opted into
    /// <c>AccessibilityMode.PdfUa1</c>; null otherwise. Held on the
    /// context so each per-page <see cref="PdfCanvas"/> gets it via
    /// <see cref="UpdateCanvas"/>.
    /// </summary>
    public TaggingContext? Tagging { get; set; }

    /// <summary>
    /// Optional layout-snapshot recorder. Attached only when a caller opted into
    /// <c>CaptureLayout</c> (currently the SVG exporter). When null, all capture
    /// is a guarded no-op and output is byte-identical to the un-instrumented path.
    /// </summary>
    public LayoutRecorder? LayoutRecorder { get; set; }

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
        {
            // Content-only mode emits page-body markup (no <svg> header) so the
            // page export pipeline can wrap each page in a standalone root.
            Canvas = new SvgCanvas(PageSize.Width, PageSize.Height, 0, 0, CurrentStream, !_svgContentOnly,
                ResolveEmbeddedFont)
            {
                Options = SvgOptions,
                FontUsageSink = TrackSvgFontUsage,
                RegisterFontCallback = RegisterFont,
                // Both render paths share one run resolver, so the SVG preview
                // cannot drift from the PDF in which font draws which code point.
                TextRunResolver = ResolveTextRuns,
                IdAllocator = _svgIdAllocator
            };
        }
        else
        {
            var pdfCanvas = new PdfCanvas(CurrentStream, PageSize.Height, 0, 0, RegisterFont, ResolveImageResource,
                ResolveEmbeddedFont, TrackGlyphUsage, RegisterAlphaGraphicsState, ResolveTextRuns, ReportUntaggedImage,
                RegisterShading);
            if (Tagging != null) pdfCanvas.SetTaggingContext(Tagging);
            Canvas = pdfCanvas;
        }
    }

    /// <summary>
    /// Gets a canvas for a specific region or with specific offsets, writing to the current stream.
    /// </summary>
    public ICanvas GetCanvas(float x, float y)
    {
        if (IsSvgMode)
            return new SvgCanvas(PageSize.Width, PageSize.Height, x, y, CurrentStream, false, ResolveEmbeddedFont)
            {
                Options = SvgOptions,
                FontUsageSink = TrackSvgFontUsage,
                RegisterFontCallback = RegisterFont,
                // Both render paths share one run resolver, so the SVG preview
                // cannot drift from the PDF in which font draws which code point.
                TextRunResolver = ResolveTextRuns,
                IdAllocator = _svgIdAllocator
            };

        var pdfCanvas = new PdfCanvas(CurrentStream, PageSize.Height, x, y, RegisterFont, ResolveImageResource,
            ResolveEmbeddedFont, TrackGlyphUsage, RegisterAlphaGraphicsState, ResolveTextRuns, ReportUntaggedImage,
            RegisterShading);
        if (Tagging != null)
        {
            pdfCanvas.SetTaggingContext(Tagging);
            // The new canvas appends to the same stream as the one already
            // rendering, so it has to see the marked-content scopes that canvas
            // has open - otherwise it treats itself as top level and wraps its
            // marks in a redundant /Artifact nested inside them.
            if (Canvas is PdfCanvas active)
                pdfCanvas.InheritOpenScopes(active.OpenTaggedCount, active.ArtifactDepth);
        }
        return pdfCanvas;
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
    internal TtfFont? ResolveEmbeddedFont(string fontIdentifier) => Resources.ResolveEmbeddedFont(fontIdentifier);

    // ── SVG embedded-font usage ─────────────────────────────────────────────────
    // Accumulated across every SvgCanvas this context creates, so the page
    // exporter can embed a minimal subset (only used glyphs) of each font.
    private readonly Dictionary<string, (TtfFont Font, HashSet<int> CodePoints)> _svgFontUsage = new();

    internal void TrackSvgFontUsage(string fontFamily, TtfFont font, int codePoint)
    {
        if (!_svgFontUsage.TryGetValue(fontFamily, out var entry))
        {
            entry = (font, new HashSet<int>());
            _svgFontUsage[fontFamily] = entry;
        }
        entry.CodePoints.Add(codePoint);
    }

    /// <summary>Per embedded font (keyed by PDF font name): the resolved face and
    /// every code point drawn with it, for building minimal SVG font subsets.</summary>
    public IReadOnlyDictionary<string, (TtfFont Font, HashSet<int> CodePoints)> SvgFontUsage => _svgFontUsage;

    /// <summary>
    /// Records the characters in <paramref name="text"/> as used by the specified font,
    /// enabling accurate glyph subsetting when the PDF is written.
    /// </summary>
    internal void TrackGlyphUsage(string fontIdentifier, string text) => Resources.TrackGlyphUsage(fontIdentifier, text);

    /// <summary>
    /// Resolves a draw call's text into one or more runs, each renderable by a
    /// single font, applying glyph fallback. Wired into each per-page
    /// <see cref="PdfCanvas"/> so multi-script text never emits a <c>.notdef</c>
    /// reference.
    /// </summary>
    internal IList<ResolvedTextRun> ResolveTextRuns(string fontIdentifier, string text)
        => Resources.ResolveTextRuns(fontIdentifier, text);

    /// <summary>
    /// Called when an image/XObject reaches the canvas in a tagged document with
    /// no enclosing structure tag or artifact scope, so it was auto-wrapped as an
    /// /Artifact to stay PDF/UA-valid. Surfacing this as a diagnostic keeps a
    /// meaningful image from being demoted to an artifact <em>silently</em> — the
    /// author should tag it (alt text → Figure) or mark it decorative on purpose.
    /// </summary>
    internal void ReportUntaggedImage()
    {
        Resources.Diagnostics.AddWarning(
            DiagnosticCategory.Tagging,
            "An image was drawn in a tagged document without a structure tag or artifact scope; " +
            "it was emitted as an /Artifact. Provide alt text (tags it as /Figure) or mark it decorative to silence this.");
    }

    /// <summary>
    /// Registers a font by its identifier and returns the PDF resource name (e.g. <c>/F1</c>)
    /// to use in content stream operators.
    /// </summary>
    public string RegisterFont(string fontIdentifier) => Resources.RegisterFont(fontIdentifier);

    /// <summary>
    /// Registers a reusable PDF transparency graphics state for the given alpha value.
    /// </summary>
    public string RegisterAlphaGraphicsState(float alpha) => Resources.RegisterAlphaGraphicsState(alpha);

    /// <summary>
    /// Registers a native gradient shading (and luminosity soft mask for translucent
    /// gradients), returning the resource names to reference, or <c>null</c> to fall
    /// back to the strip/disc approximation (translucent gradient under PDF/A-1).
    /// </summary>
    public ShadingResources? RegisterShading(ShadingRequest request) => Resources.RegisterShading(request);

    /// <summary>
    /// Returns <c>true</c> if the given font identifier refers to an embedded TrueType font
    /// rather than one of the 14 standard PDF fonts.
    /// </summary>
    public static bool IsEmbeddedFont(string fontIdentifier) => PdfResourceRegistry.IsEmbeddedFont(fontIdentifier);

    /// <summary>
    /// Returns the <see cref="EmbeddedFontInfo"/> for a font loaded from the given file path,
    /// loading and caching it on first access.
    /// </summary>
    public EmbeddedFontInfo? GetEmbeddedFont(string filePath) => Resources.GetEmbeddedFont(filePath);

    /// <summary>
    /// Creates a new page and resets the cursor position
    /// </summary>
    public void NewPage()
    {
        // Layout capture: the Y where content stopped on the outgoing page, taken
        // before the reset below so open nodes can close their rect at the boundary.
        float outgoingBottomY = CurrentY;

        // Page-break-aware tagging: marked-content scopes (BDC/EMC)
        // cannot cross a page boundary per PDF spec, but structure
        // elements can. Snapshot any marked-content currently open,
        // emit EMC on the outgoing page so the marks are balanced,
        // then on the new page reopen the same roles with fresh MCIDs.
        // All MCIDs (old page + new page) attach to the same structure
        // elements because the StructureTreeBuilder stack is untouched.
        string[]? reopenRoles = null;
        if (Tagging != null && Tagging.OpenMarkedContentCount > 0)
        {
            reopenRoles = Tagging.SnapshotOpenRolesBottomUp();
            for (int i = reopenRoles.Length - 1; i >= 0; i--)
                Canvas.EndMarkedContent();
        }

        Pages.Add(new StringBuilder());
        CurrentPageIndex++;
        CurrentX = Margin;
        CurrentY = Margin;
        // MCIDs restart at 0 per PDF page (ISO 32000-1 §14.7.4).
        // Pass the new page index so any subsequent MCID can be paired
        // with its page for /Pg + /ParentTree emission later.
        Tagging?.ResetPage(CurrentPageIndex);
        UpdateCanvas();

        // Layout capture: split every open node's rect across the page boundary.
        LayoutRecorder?.PageBreak(outgoingBottomY, CurrentPageIndex, CurrentY);

        if (reopenRoles != null)
        {
            foreach (string role in reopenRoles)
            {
                if (ContainerRoleTags.Tags.Contains(role))
                    Canvas.BeginContainerContent(role);
                else
                    Canvas.BeginMarkedContent(role);
            }
        }

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
            throw new ArgumentOutOfRangeException(nameof(index));
        CurrentPageIndex = index;
        // Keep the tagging context's page tracking in lock-step with the
        // physical page. NewPage() does this for automatic overflow breaks;
        // SwitchToPage handles the boundaries between explicit input Page
        // objects (the render pipeline switches here when it starts each new
        // input page). Without it, MCIDs allocated on the new page keep the
        // previous page's index and counter, so the structure tree's /Pg and
        // /ParentTree point at the wrong page - veraPDF then reports the
        // content as untagged real content (PDF/UA 7.1). MCIDs restart at 0
        // per page (ISO 32000-1 §14.7.4).
        Tagging?.ResetPage(index);
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
