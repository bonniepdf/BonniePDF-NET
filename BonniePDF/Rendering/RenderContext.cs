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
        CurrentY = ContentTop;

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
    /// Per-page header/footer measurement, installed by the render pipeline via
    /// <see cref="SetChromeReservation"/>. <c>null</c> for a document with no
    /// page chrome, in which case the reservation is a constant zero and the
    /// content band is the margin box.
    /// </summary>
    private Func<int, ChromeReservation>? _chromeProvider;

    /// <summary>Memoised per page index, so a template is measured once per page.</summary>
    private readonly Dictionary<int, ChromeReservation> _chromeCache = new();

    private ChromeReservation _chrome = ChromeReservation.None;

    /// <summary>
    /// While <c>true</c>, <see cref="ContentTop"/> and <see cref="MaxY"/> ignore
    /// the chrome reservation. Set only while the engine paints the chrome
    /// itself: a header is not laid out inside the band it reserves for the body.
    /// </summary>
    public bool ChromeReservationSuspended { get; set; }

    /// <summary>
    /// While a table draws a cell whose content is a <see cref="BonniePDF.Elements.Layout.Container"/>
    /// (seen through pass-through wrappers), that container. It fills <see cref="CellStretchHeight"/>, so a
    /// border or background applied to a cell's content spans the whole row rather than only the cell's own
    /// text. <c>null</c> everywhere else; no other container is affected.
    /// </summary>
    public BonniePDF.Elements.Layout.Container? CellStretchTarget { get; set; }

    /// <summary>The height <see cref="CellStretchTarget"/> fills: the row height less the cell's padding.</summary>
    public float CellStretchHeight { get; set; }

    /// <summary>
    /// Installs (or clears) the per-page header/footer measurement.
    /// </summary>
    /// <remarks>
    /// Deliberately does not measure here: the pipeline assigns this run's page
    /// size and margin per page and then switches to the page, which is what
    /// triggers the first measurement against the right content width. Clearing
    /// the memo keeps a context that is re-driven for a second pass - the
    /// streaming pipeline's compositing loop, a TOC dry run - from reusing
    /// heights measured against a different page size or margin.
    /// See the shared specification, "Layout &amp; Pagination" §5.1.
    /// </remarks>
    public void SetChromeReservation(Func<int, ChromeReservation>? provider)
    {
        _chromeProvider = provider;
        _chromeCache.Clear();
        _chrome = ChromeReservation.None;
    }

    /// <summary>
    /// Re-measures the current page's chrome. Called on every page switch and
    /// every page break; the pipelines also call it after assigning a new page
    /// size or margin, because those change the width the chrome is measured
    /// against.
    /// </summary>
    public void RefreshChromeReservation()
    {
        if (_chromeProvider == null)
        {
            _chrome = ChromeReservation.None;
            return;
        }

        if (!_chromeCache.TryGetValue(CurrentPageIndex, out ChromeReservation reservation))
        {
            reservation = _chromeProvider(CurrentPageIndex + 1);
            _chromeCache[CurrentPageIndex] = reservation;
        }

        _chrome = reservation;
        AssertUsableContentBand();
    }

    /// <summary>Measured header height for the current page, in points (zero when there is none).</summary>
    public float ReservedHeaderHeight => _chrome.Header;

    /// <summary>Measured footer height for the current page, in points (zero when there is none).</summary>
    public float ReservedFooterHeight => _chrome.Footer;

    /// <summary>
    /// Gets the top of the body content band: <c>max(Margin, headerHeight)</c>.
    /// Chrome that fits inside the top margin reserves nothing beyond the margin
    /// the page already had, so this reduces to <see cref="Margin"/> for the
    /// overwhelming majority of documents.
    /// </summary>
    public float ContentTop =>
        ChromeReservationSuspended ? Margin : MathF.Max(Margin, _chrome.Header);

    /// <summary>
    /// Gets the bottom of the body content band:
    /// <c>PageSize.Height - max(Margin, footerHeight)</c>.
    /// </summary>
    public float ContentBottom => ChromeReservationSuspended
        ? PageSize.Height - Margin
        : PageSize.Height - MathF.Max(Margin, _chrome.Footer);

    /// <summary>
    /// Gets the maximum Y coordinate before the bottom of the content band is
    /// reached. Elements that exceed this value trigger a page break.
    /// </summary>
    public float MaxY => ContentBottom;

    /// <summary>
    /// Gets the usable body height on the current page: the full content band,
    /// not just what is left below the cursor. Elements use it to ask "would
    /// this fit on a fresh page at all?" before deciding to break rather than
    /// split.
    /// </summary>
    public float ContentHeight => ContentBottom - ContentTop;

    /// <summary>Moves the cursor to the top-left of the current page's body content band.</summary>
    public void ResetCursorToContentTop()
    {
        CurrentX = Margin;
        CurrentY = ContentTop;
    }

    /// <summary>
    /// Honest failure rather than silent clipping: a header and footer that
    /// between them consume the whole page leave nowhere for body content, and
    /// every subsequent break decision would be made against a negative band.
    /// </summary>
    private void AssertUsableContentBand()
    {
        float top = MathF.Max(Margin, _chrome.Header);
        float bottom = PageSize.Height - MathF.Max(Margin, _chrome.Footer);
        if (bottom > top) return;

        throw new InvalidOperationException(
            $"Page {CurrentPageIndex + 1}: the header ({Round(_chrome.Header)}pt) and footer " +
            $"({Round(_chrome.Footer)}pt) leave no room for content on a {Round(PageSize.Height)}pt-tall " +
            $"page with a {Round(Margin)}pt margin. Reduce the header/footer height, reduce the margin, " +
            "or use a larger page.");
    }

    /// <summary>Formats a point measurement for a diagnostic message (2 dp, no trailing zeros).</summary>
    private static string Round(float value) =>
        MathF.Round(value, 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

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
                RegisterShading, Resources.NoteImagePlacement);
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
            // A positioned child canvas. It draws in its own coordinate space,
            // into its own buffer, and records where it sits; FlushExternalCanvas
            // composites it into the page inside a translate group. Baking the
            // offset into every coordinate instead would put the same marks in
            // the same place, but the TypeScript exporter emits the group, and
            // the shared specification names SVG as the artefact the conformance
            // tooling compares - so the structure is part of the contract.
            return new SvgCanvas(PageSize.Width, PageSize.Height, 0, 0, null, false, ResolveEmbeddedFont)
            {
                ChildOffsetX = x,
                ChildOffsetY = y,
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
            RegisterShading, Resources.NoteImagePlacement);
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

    /// <summary>
    /// Composites a canvas obtained from <see cref="GetCanvas"/> into the
    /// currently active canvas.
    /// </summary>
    /// <remarks>
    /// Only the SVG path needs this. A PDF child canvas appends to the same
    /// stream as it draws, with its offset already applied to each operator, so
    /// there is nothing left to composite.
    /// </remarks>
    public void FlushExternalCanvas(ICanvas canvas)
    {
        if (!IsSvgMode || canvas is not SvgCanvas child || Canvas is not SvgCanvas page) return;

        string inner = child.GetInnerSvg();
        if (string.IsNullOrEmpty(inner)) return;

        float tx = child.ChildOffsetX;
        float ty = child.ChildOffsetY;
        string open = tx != 0 || ty != 0
            ? $"<g transform=\"translate({SvgNumber(tx)} {SvgNumber(ty)})\">"
            : "<g>";
        page.AppendRawMarkup(open + "\n" + inner + "</g>");
    }

    /// <summary>
    /// Formats a translate component the way <see cref="SvgCanvas"/> formats a
    /// coordinate, so a composited group's offset matches the numbers inside it.
    /// </summary>
    private string SvgNumber(float v) => SvgOptions.Precision is int p
        ? Math.Round((double)v, p).ToString(System.Globalization.CultureInfo.InvariantCulture)
        : v.ToString(System.Globalization.CultureInfo.InvariantCulture);

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
        // Header and footer chrome is painted at a fixed position in the margin band, which lies
        // outside the body's content area. An element in it that checks "does this fit?" (a
        // Container, say) would see the band as overflow and break the page in the middle of
        // painting the chrome, losing the content. Chrome never paginates.
        if (ChromeReservationSuspended) return;

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

        // Graphics state gets the same treatment, and for the same reason: a q
        // without its Q (or a <g> without its </g>) may not cross a page boundary.
        // An element that breaks the page from inside its own Render leaves the
        // caller's SaveState open here - the caller's finally will RestoreState
        // against the *new* page, which would otherwise silently drop the close and
        // leave the outgoing page unbalanced. Close them here, reopen below, so both
        // pages balance and the caller's RestoreState still has a scope to close.
        int reopenStateDepth = Canvas.GraphicsStateDepth;
        for (int i = 0; i < reopenStateDepth; i++)
            Canvas.RestoreState();

        Pages.Add(new StringBuilder());
        CurrentPageIndex++;
        // The new page measures its own chrome: header/footer templates are
        // functions of the page number, so a taller header on page 2 moves page
        // 2's content band down. Must precede the cursor reset, which reads
        // ContentTop.
        RefreshChromeReservation();
        ResetCursorToContentTop();
        // MCIDs restart at 0 per PDF page (ISO 32000-1 §14.7.4).
        // Pass the new page index so any subsequent MCID can be paired
        // with its page for /Pg + /ParentTree emission later.
        Tagging?.ResetPage(CurrentPageIndex);
        UpdateCanvas();

        // Layout capture: split every open node's rect across the page boundary.
        LayoutRecorder?.PageBreak(outgoingBottomY, CurrentPageIndex, CurrentY);

        for (int i = 0; i < reopenStateDepth; i++)
            Canvas.SaveState();
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
    /// <remarks>
    /// In SVG mode this writes straight into the page stream rather than taking
    /// the positioned-child route <see cref="GetCanvas"/> uses. It is the page's
    /// own canvas at its own origin, not a child sitting somewhere on it, so
    /// there is nothing to translate and no group to composite - and the
    /// TypeScript exporter emits page chrome the same way, ungrouped.
    /// </remarks>
    public ICanvas GetFixedCanvas()
    {
        if (!IsSvgMode) return GetCanvas(0, 0);

        return new SvgCanvas(PageSize.Width, PageSize.Height, 0, 0, CurrentStream, false, ResolveEmbeddedFont)
        {
            Options = SvgOptions,
            FontUsageSink = TrackSvgFontUsage,
            RegisterFontCallback = RegisterFont,
            TextRunResolver = ResolveTextRuns,
            IdAllocator = _svgIdAllocator
        };
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
        RefreshChromeReservation();
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
