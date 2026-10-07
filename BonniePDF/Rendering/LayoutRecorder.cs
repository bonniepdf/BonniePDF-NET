using System.Linq;
using System;
using System.Collections.Generic;

namespace BonniePDF;

// ---------------------------------------------------------------------------
// Layout snapshot capture.
//
// The engine already computes, during the render pass, the page rectangle every
// element occupies — then discards it once content is emitted. The LayoutRecorder
// is an optional, opt-in observer that collects those rectangles into a tree (the
// LayoutSnapshot) without influencing layout. When no recorder is attached to the
// RenderContext, capture is a guarded no-op and output is byte-identical.
//
// Coordinate space: page-local, top-left origin, PDF points — the same space the
// layout engine threads through CurrentX/CurrentY and the same space the exported
// SVG uses. No bottom-left↔top-left flip happens here; the PDF flip is applied
// later, inside the PDF canvas.
//
// This mirrors the TypeScript implementation; both conform to the shared
// layout-snapshot specification and are verified against the shared
// conformance fixtures.
//
// NOTE: This file has NOT been compiled in the authoring environment (no .NET
// toolchain was available). It must be built and run against the
// layout-snapshot fixture before it is considered landed.
// ---------------------------------------------------------------------------

/// <summary>Schema version emitted in every snapshot, so consumers can feature-detect.
/// v2 adds per-node <see cref="NodeAccessibility"/> (<c>A11y</c>) and
/// <see cref="SnapshotMeta"/> (<c>Meta</c>); v3 adds <c>SourceId</c>; v4 adds
/// per-node <see cref="NodePaint"/> (<c>Paint</c>) and the page backdrop facts on
/// <see cref="LayoutSnapshot.PageSize"/>. Consumers that ignore unknown fields
/// remain compatible.</summary>
public static class LayoutSnapshotSchema
{
    public const int Version = 4;
}

/// <summary>A single rectangle an element occupied on one page (top-left origin, points).</summary>
public sealed class LayoutRect
{
    /// <summary>0-based page index, aligned with <see cref="LayoutSnapshot.Pages"/> and SVG page order.</summary>
    public int Page { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
}

/// <summary>
/// Accessibility facts captured for a layout node, mirroring what the tagging
/// pipeline would write into the PDF structure tree for the same element. Present
/// only when the element exposes a role (implements <c>IRoleProvider</c>).
///
/// These are the element's <em>intrinsic</em> semantics, read directly off the
/// element — they do not depend on <c>AccessibilityMode</c> being enabled, so the
/// compliance evaluator can answer "would this be accessible?" for untagged
/// documents too.
/// </summary>
public sealed class NodeAccessibility
{
    /// <summary>Resolved PDF structure tag, e.g. "H1", "P", "Figure", "TH".</summary>
    public string Role { get; init; } = "";
    /// <summary>True when the element contributes real content to the structure tree.</summary>
    public bool Tagged { get; init; }
    /// <summary>True when the element renders as an /Artifact (out of reading order).</summary>
    public bool Artifact { get; init; }
    /// <summary>/Alt alternate text, when the element supplies non-empty alt text.</summary>
    public string? Alt { get; init; }
    /// <summary>Author explicitly marked the element decorative (artifact by intent).</summary>
    public bool? Decorative { get; init; }
    /// <summary>Heading level 1–6 for H1…H6 roles; null otherwise.</summary>
    public int? HeadingLevel { get; init; }
    /// <summary>True when the element is image-like (a figure candidate).</summary>
    public bool? ImageLike { get; init; }
}

/// <summary>
/// Document-level accessibility/compliance context, captured alongside the tree so
/// the compliance evaluator can raise document-scoped findings (missing language,
/// missing title, untagged document) without a second source of truth.
/// </summary>
public sealed class SnapshotMeta
{
    /// <summary>The document's declared AccessibilityMode as an int (0 = None, 1 = PdfUa1).</summary>
    public int AccessibilityMode { get; init; }
    /// <summary>The document's declared PDF/A conformance name (e.g. "None", "PdfA2b").</summary>
    public string Conformance { get; init; } = "None";
    /// <summary>BCP-47 document language (/Lang), when declared.</summary>
    public string? Language { get; init; }
    /// <summary>Document title, when declared.</summary>
    public string? Title { get; init; }
}

/// <summary>
/// What an element paints, captured so the compliance preflight can evaluate
/// colour contrast without a second render pass or any inspection of rendered
/// output.
///
/// Colours are "#rrggbb" strings with alpha carried separately, so an opaque
/// document produces no alpha values at all and fixtures stay readable across
/// languages. Elements that paint nothing relevant produce no NodePaint.
/// </summary>
public sealed class NodePaint
{
    /// <summary>Resolved text colour, "#rrggbb"; null when the element paints no text.</summary>
    public string? Text { get; set; }
    /// <summary>Text alpha in [0,1); null when fully opaque.</summary>
    public float? TextAlpha { get; set; }
    /// <summary>The element's own resolved background fill, "#rrggbb"; null when it has none.</summary>
    public string? Background { get; set; }
    /// <summary>Background alpha in [0,1); null when fully opaque.</summary>
    public float? BackgroundAlpha { get; set; }
    /// <summary>Rendered text size in points; null when the element paints no text.</summary>
    public float? FontSize { get; set; }
    /// <summary>True when the resolved font weight is 700 or heavier.</summary>
    public bool? Bold { get; set; }
    /// <summary>
    /// The node paints something behind its children that cannot be reduced to one
    /// opaque colour — layers or a freely drawn surface. Descendant text is reported
    /// as contrast-unknown rather than measured against a guess.
    /// </summary>
    public bool? BackdropUnknown { get; set; }

    /// <summary>True when nothing was captured, so the node should carry no Paint.</summary>
    internal bool IsEmpty =>
        Text is null && Background is null && FontSize is null && BackdropUnknown is null;
}

/// <summary>One node in the layout tree: a rendered element and the area(s) it occupied.</summary>
public sealed class LayoutNode
{
    /// <summary>Stable within one snapshot; assigned in deterministic render order.</summary>
    public string Id { get; init; } = "";
    /// <summary>Canonical element type name, e.g. "TextBlock", "Table", "Page".</summary>
    public string Type { get; init; } = "";
    /// <summary>Optional author-supplied name/key; null when unset.</summary>
    public string? Name { get; init; }
    /// <summary>Optional opaque source ID supplied by an editor integration.</summary>
    public string? SourceId { get; set; }
    /// <summary>The rectangles this node occupied — one per page it touched (may be empty).</summary>
    public IList<LayoutRect> Rects { get; } = new List<LayoutRect>();
    /// <summary>Intrinsic accessibility facts, when the element declares a role; null otherwise.</summary>
    public NodeAccessibility? A11y { get; set; }
    /// <summary>Intrinsic colour/typography facts, when the element paints any; null otherwise.</summary>
    public NodePaint? Paint { get; set; }
    public IList<LayoutNode> Children { get; } = new List<LayoutNode>();
}

/// <summary>The captured layout of a whole document.</summary>
public sealed class LayoutSnapshot
{
    public int Version { get; init; } = LayoutSnapshotSchema.Version;
    /// <summary>Page sizes in points, index-aligned with the rendered pages.</summary>
    public IReadOnlyList<PageSize> Pages { get; init; } = Array.Empty<PageSize>();
    /// <summary>Root node (the Document). Its children form the element tree in render order.</summary>
    public LayoutNode Root { get; init; } = new();
    /// <summary>Document-level accessibility/compliance context; null when unset.</summary>
    public SnapshotMeta? Meta { get; init; }

    /// <summary>
    /// The page's dimensions in points, as laid out.
    /// </summary>
    public sealed class PageSize
    {
        public float Width { get; init; }
        public float Height { get; init; }
        /// <summary>Resolved page fill as "#rrggbb", when one is set; null otherwise.</summary>
        public string? Color { get; init; }
        /// <summary>The page paints a background element, so its backdrop is unmodellable.</summary>
        public bool BackdropUnknown { get; init; }
    }
}

/// <summary>
/// Collects a <see cref="LayoutSnapshot"/> during a render pass. See the file
/// header and the TypeScript reference for the lifecycle contract. Mirrors
/// <c>LayoutRecorder</c> in the TypeScript core.
/// </summary>
internal sealed class LayoutRecorder
{
    internal BonniePDF.Drawing.PaintTransform? ElementTransform { get; set; }
    private sealed class OpenFrame
    {
        public BonniePDF.Drawing.PaintTransform? Transform;
        public required LayoutNode Node;
        public float X;
        public float Width;
        public int Page;
        public float StartY;
    }

    /// <summary>Gates all capture; turned off while rendering page chrome.</summary>
    public bool Capturing { get; set; } = true;

    private readonly LayoutNode _root = new() { Id = "n0", Type = "Document" };
    private readonly List<OpenFrame> _stack;
    // Keyed by element identity so a resumed (paginating) element coalesces.
    private readonly Dictionary<object, OpenFrame> _open =
        new(ReferenceEqualityComparer.Instance);
    private int _nextId = 1;
    private IReadOnlyList<LayoutSnapshot.PageSize> _pageSizes = Array.Empty<LayoutSnapshot.PageSize>();
    private SnapshotMeta? _meta;

    public LayoutRecorder()
    {
        _stack = new List<OpenFrame> { new() { Node = _root } };
    }

    /// <summary>Records the page sizes and backdrop facts (index-aligned with rendered pages).</summary>
    public void SetPages(IEnumerable<LayoutSnapshot.PageSize> pages)
    {
        var list = new List<LayoutSnapshot.PageSize>();
        foreach (LayoutSnapshot.PageSize page in pages)
        {
            list.Add(new LayoutSnapshot.PageSize
            {
                Width = page.Width,
                Height = page.Height,
                Color = page.Color,
                BackdropUnknown = page.BackdropUnknown
            });
        }
        _pageSizes = list;
    }

    /// <summary>Records the document-level accessibility/compliance context.</summary>
    public void SetMeta(SnapshotMeta meta) => _meta = meta;

    /// <summary>Opens (or resumes) a node for <paramref name="element"/>.</summary>
    public void OpenNode(object element, string type, string? name, int page, float x, float y, float width)
    {
        if (!Capturing) return;
        if (_open.ContainsKey(element)) return; // resumed segment of an open element

        var node = new LayoutNode { Id = $"n{_nextId++}", Type = type, Name = name, SourceId = DebugSource.Of(element) };
        node.A11y = AccessibilityOf(element, type);
        node.Paint = PaintOf(element, type);
        _stack[^1].Node.Children.Add(node);

        var frame = new OpenFrame { Node = node, X = x, Width = width, Page = page, StartY = y, Transform = ElementTransform };
        _stack.Add(frame);
        _open[element] = frame;
    }

    /// <summary>
    /// Merges additional paint facts into an already-open node.
    ///
    /// Needed where the value a node paints is not one of its own properties: a
    /// table cell's background comes from the header/footer/row cascade resolved
    /// in the row renderer, so the cell element itself has no BackgroundColor to
    /// read. Without this the cell would appear unpainted and its text would be
    /// measured against the page instead of its actual fill.
    ///
    /// A no-op when no node is open for <paramref name="element"/>, so callers
    /// need no guard.
    /// </summary>
    public void AnnotatePaint(object element, NodePaint paint)
    {
        if (!Capturing) return;
        if (!_open.TryGetValue(element, out OpenFrame? frame)) return;

        NodePaint target = frame.Node.Paint ??= new NodePaint();
        if (paint.Text is not null) target.Text = paint.Text;
        if (paint.TextAlpha is not null) target.TextAlpha = paint.TextAlpha;
        if (paint.Background is not null) target.Background = paint.Background;
        if (paint.BackgroundAlpha is not null) target.BackgroundAlpha = paint.BackgroundAlpha;
        if (paint.FontSize is not null) target.FontSize = paint.FontSize;
        if (paint.Bold is not null) target.Bold = paint.Bold;
        if (paint.BackdropUnknown is not null) target.BackdropUnknown = paint.BackdropUnknown;
    }

    /// <summary>Closes a node. When <paramref name="complete"/> is false it stays open for resumption.</summary>
    public void CloseNode(object element, int page, float endY, bool complete)
    {
        if (!Capturing) return;
        if (!_open.TryGetValue(element, out OpenFrame? frame)) return;
        if (!complete) return; // overflowed; PageBreak finalises the open rect, resume later

        PushRect(frame, endY);
        _open.Remove(element);
        int idx = _stack.IndexOf(frame);
        if (idx >= 0) _stack.RemoveRange(idx, _stack.Count - idx);
    }

    internal void AbortNode(object element)
    {
        if (!_open.TryGetValue(element, out var frame)) return;
        int index = _stack.IndexOf(frame);
        if (index < 0) return;
        var removed = new HashSet<OpenFrame>(_stack.GetRange(index, _stack.Count - index));
        _stack.RemoveRange(index, _stack.Count - index);
        foreach (var key in _open.Where(pair => removed.Contains(pair.Value)).Select(pair => pair.Key).ToArray()) _open.Remove(key);
    }

    /// <summary>Splits every open node's rect across a page boundary.</summary>
    public void PageBreak(float outgoingBottomY, int newPage, float newTopY)
    {
        if (!Capturing) return;
        // Skip the root frame (index 0); it carries no geometry.
        for (int i = 1; i < _stack.Count; i++)
        {
            OpenFrame frame = _stack[i];
            PushRect(frame, outgoingBottomY);
            frame.Page = newPage;
            frame.StartY = newTopY;
        }
    }

    private static void PushRect(OpenFrame frame, float endY)
    {
        float height = endY - frame.StartY;
        if (height <= 0) return; // an element that drew nothing on this page gets no rect here
        var box = frame.Transform?.MapBox(frame.X, frame.StartY, frame.X + frame.Width, endY)
            ?? (X0: frame.X, Y0: frame.StartY, X1: frame.X + frame.Width, Y1: endY);
        frame.Node.Rects.Add(new LayoutRect
        {
            Page = frame.Page,
            X = box.X0,
            Y = box.Y0,
            Width = box.X1 - box.X0,
            Height = box.Y1 - box.Y0
        });
    }

    /// <summary>Produces the snapshot. Safe to call once rendering has finished.</summary>
    public LayoutSnapshot Snapshot() => new()
    {
        Version = LayoutSnapshotSchema.Version,
        Pages = _pageSizes,
        Root = _root,
        Meta = _meta
    };

    /// <summary>Reads an element's canonical capture type without depending on ElementBase.</summary>
    public static string CaptureTypeOf(object element)
    {
        if (element is ICaptureMetadata meta) return meta.CaptureType;
        return element.GetType().Name;
    }

    /// <summary>Reads an element's optional author capture name.</summary>
    public static string? CaptureNameOf(object element)
    {
        return element is ICaptureMetadata meta ? meta.CaptureName : null;
    }

    private static readonly HashSet<string> ImageLikeTypes = new()
    {
        "Image", "Svg", "QRCode", "Barcode", "DataMatrix", "CanvasElement",
        "BarChart", "GroupedBarChart", "LineChart", "AreaChart", "PieChart",
        "DotPlotChart", "LollipopChart", "ChartElement", "GroupedBarChartElement"
    };

    /// <summary>
    /// Derives <see cref="NodeAccessibility"/> from an element by reading its intrinsic
    /// <c>SemanticRole</c> / <c>AltText</c> (the same properties the tagging pipeline
    /// consumes via <c>IRoleProvider</c> / <c>IAltTextProvider</c>). Returns null for
    /// elements that declare no role.
    /// </summary>
    public static NodeAccessibility? AccessibilityOf(object element, string type)
    {
        if (element is not IRoleProvider roleProvider) return null;

        string role = roleProvider.SemanticRole.ToString();
        bool artifact = roleProvider.SemanticRole == SemanticRole.Artifact;

        int? headingLevel = role switch
        {
            "H1" => 1, "H2" => 2, "H3" => 3, "H4" => 4, "H5" => 5, "H6" => 6,
            _ => (int?)null
        };

        string? alt = null;
        bool? decorative = null;
        if (element is IAltTextProvider altProvider)
        {
            if (!string.IsNullOrEmpty(altProvider.AltText)) alt = altProvider.AltText;
            if (altProvider.IsDecorative) decorative = true;
        }

        return new NodeAccessibility
        {
            Role = role,
            Tagged = !artifact,
            Artifact = artifact,
            Alt = alt,
            Decorative = decorative,
            HeadingLevel = headingLevel,
            ImageLike = ImageLikeTypes.Contains(type) ? true : (bool?)null
        };
    }

    /// <summary>
    /// Element types that draw arbitrary content behind whatever is stacked on
    /// them, so a descendant's backdrop cannot be reduced to one opaque colour.
    /// Image-like elements are leaves — text is never their descendant — so they
    /// are not listed here.
    /// </summary>
    private static readonly HashSet<string> UnmodelledBackdropTypes = new()
    {
        "Layers", "CanvasElement"
    };

    /// <summary>Formats a colour as "#rrggbb" (alpha is carried separately).</summary>
    public static string ColorToHex(Color color) =>
        $"#{color.R:x2}{color.G:x2}{color.B:x2}";

    /// <summary>Alpha in [0,1], rounded to 3 decimals so fixtures compare cleanly.</summary>
    private static float AlphaOf(Color color) =>
        (float)Math.Round(color.ScA, 3, MidpointRounding.AwayFromZero);

    /// <summary>Builds a <see cref="NodePaint"/> carrying just a resolved background colour,
    /// for callers that compute a fill outside the element (see <see cref="AnnotatePaint"/>).</summary>
    public static NodePaint PaintForBackground(Color color)
    {
        var paint = new NodePaint { Background = ColorToHex(color) };
        float alpha = AlphaOf(color);
        if (alpha < 1f) paint.BackgroundAlpha = alpha;
        return paint;
    }

    /// <summary>
    /// Derives <see cref="NodePaint"/> from an element by reading its already
    /// resolved text colour / size / weight and background fill — the same values
    /// the canvas is about to draw with. Returns null when the element paints
    /// nothing the contrast rules care about, so most nodes stay unannotated.
    ///
    /// Purely observational: it reads existing properties and never triggers layout.
    /// </summary>
    public static NodePaint? PaintOf(object element, string type)
    {
        var paint = new NodePaint();

        if (element is TextElementBase text)
        {
            paint.Text = ColorToHex(text.Color);
            float textAlpha = AlphaOf(text.Color);
            if (textAlpha < 1f) paint.TextAlpha = textAlpha;
            paint.FontSize = text.FontSize;
            if (text.FontWeight >= FontWeight.Bold || FontNameLooksBold(text.FontName)) paint.Bold = true;
        }
        else if (element is Hyperlink link)
        {
            // Hyperlink paints text but does not derive from TextElementBase — it
            // carries its own Color/FontSize. Gating the text branch on the base
            // class alone would leave every link unannotated, and link text on a
            // coloured backdrop is exactly the contrast case authors get wrong.
            // It exposes no Weight property at all, so FontNameLooksBold is the
            // only signal available for it.
            paint.Text = ColorToHex(link.Color);
            float linkAlpha = AlphaOf(link.Color);
            if (linkAlpha < 1f) paint.TextAlpha = linkAlpha;
            paint.FontSize = link.FontSize;
            if (FontNameLooksBold(link.FontName)) paint.Bold = true;
        }

        Color? ownBackground = element switch
        {
            IBackgroundColor fill => fill.BackgroundColor,
            Barcode symbol => symbol.BackgroundColor,
            QRCode symbol => symbol.BackgroundColor,
            DataMatrix symbol => symbol.BackgroundColor,
            ChartBase chart => chart.BackgroundColor,
            _ => null
        };
        if (ownBackground is { } background)
        {
            paint.Background = ColorToHex(background);
            float backgroundAlpha = AlphaOf(background);
            if (backgroundAlpha < 1f) paint.BackgroundAlpha = backgroundAlpha;
        }

        if (UnmodelledBackdropTypes.Contains(type)) paint.BackdropUnknown = true;

        return paint.IsEmpty ? null : paint;
    }

    /// <summary>
    /// Secondary bold signal for elements styled by setting <c>FontName</c>
    /// directly to a bold PostScript name (e.g. "Helvetica-Bold") rather than
    /// through the Weight/FontFamily builder — the idiom most examples in this
    /// codebase still use, and the only one <see cref="Hyperlink"/> exposes
    /// at all. Every bold standard-14 name contains "Bold" (see
    /// <see cref="StandardFonts"/>), so a case-insensitive substring
    /// check is a reliable fallback where <c>Weight</c> was never touched.
    /// </summary>
    private static bool FontNameLooksBold(string? fontName) =>
        fontName is not null && fontName.Contains("Bold", StringComparison.OrdinalIgnoreCase);
}
