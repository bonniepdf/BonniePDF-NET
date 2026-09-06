using System;
using System.Collections.Generic;

namespace BonniePDF.Rendering;

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
/// <see cref="SnapshotMeta"/> (<c>Meta</c>); v1 consumers that ignore unknown fields
/// remain compatible.</summary>
public static class LayoutSnapshotSchema
{
    public const int Version = 3;
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
    public string? SourceId { get; init; }
    /// <summary>The rectangles this node occupied — one per page it touched (may be empty).</summary>
    public IList<LayoutRect> Rects { get; } = new List<LayoutRect>();
    /// <summary>Intrinsic accessibility facts, when the element declares a role; null otherwise.</summary>
    public NodeAccessibility? A11y { get; set; }
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
    }
}

/// <summary>
/// Collects a <see cref="LayoutSnapshot"/> during a render pass. See the file
/// header and the TypeScript reference for the lifecycle contract. Mirrors
/// <c>LayoutRecorder</c> in the TypeScript core.
/// </summary>
public sealed class LayoutRecorder
{
    private sealed class OpenFrame
    {
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

    /// <summary>Records the page sizes (index-aligned with rendered pages).</summary>
    public void SetPages(IEnumerable<(float Width, float Height)> pages)
    {
        var list = new List<LayoutSnapshot.PageSize>();
        foreach (var (w, h) in pages) list.Add(new LayoutSnapshot.PageSize { Width = w, Height = h });
        _pageSizes = list;
    }

    /// <summary>Records the document-level accessibility/compliance context.</summary>
    public void SetMeta(SnapshotMeta meta) => _meta = meta;

    /// <summary>Opens (or resumes) a node for <paramref name="element"/>.</summary>
    public void OpenNode(object element, string type, string? name, int page, float x, float y, float width)
    {
        if (!Capturing) return;
        if (_open.ContainsKey(element)) return; // resumed segment of an open element

        var node = new LayoutNode { Id = $"n{_nextId++}", Type = type, Name = name };
        node.A11y = AccessibilityOf(element, type);
        _stack[^1].Node.Children.Add(node);

        var frame = new OpenFrame { Node = node, X = x, Width = width, Page = page, StartY = y };
        _stack.Add(frame);
        _open[element] = frame;
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
        frame.Node.Rects.Add(new LayoutRect
        {
            Page = frame.Page,
            X = frame.X,
            Y = frame.StartY,
            Width = frame.Width,
            Height = height
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
        if (element is Elements.ICaptureMetadata meta) return meta.CaptureType;
        return element.GetType().Name;
    }

    /// <summary>Reads an element's optional author capture name.</summary>
    public static string? CaptureNameOf(object element)
    {
        return element is Elements.ICaptureMetadata meta ? meta.CaptureName : null;
    }

    private static readonly HashSet<string> ImageLikeTypes = new()
    {
        "Image", "QRCode", "Barcode", "DataMatrix", "CanvasElement",
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
        if (element is not Tagging.IRoleProvider roleProvider) return null;

        string role = roleProvider.SemanticRole.ToString();
        bool artifact = roleProvider.SemanticRole == Tagging.SemanticRole.Artifact;

        int? headingLevel = role switch
        {
            "H1" => 1, "H2" => 2, "H3" => 3, "H4" => 4, "H5" => 5, "H6" => 6,
            _ => (int?)null
        };

        string? alt = null;
        bool? decorative = null;
        if (element is Tagging.IAltTextProvider altProvider)
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
}
