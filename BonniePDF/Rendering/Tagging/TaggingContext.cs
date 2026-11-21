using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Carries the per-document tagging state through the render pipeline:
/// the in-memory <see cref="StructureTreeBuilder"/> being populated, a
/// per-page MCID counter that the canvas uses to label each
/// marked-content sequence, and a stack of currently-open
/// marked-content roles used by the page-break handler to keep
/// BDC/EMC balanced per page.
///
/// One instance per <c>Document</c> render; <see cref="RenderContext"/>
/// hands it to each page's <c>PdfCanvas</c>. When tagging is disabled,
/// the context is null and the canvas's
/// <c>BeginMarkedContent/EndMarkedContent</c> calls are no-ops.
/// </summary>
internal sealed class TaggingContext
{
    private int _nextMcidOnPage;
    private readonly Stack<string> _openRoles = new();

    /// <summary>Creates a new tagging context wrapping <paramref name="tree"/>.</summary>
    public TaggingContext(StructureTreeBuilder tree)
    {
        Tree = tree;
    }

    /// <summary>
    /// True for PDF/UA-2: the tree is written in the PDF 2.0 structure namespace,
    /// lists carry ListNumbering, TOC entries carry /Ref, and in-document
    /// destinations are structure destinations (spec 04 §3.4).
    /// </summary>
    public bool Pdf20 { get; init; }

    /// <summary>The structure element each element object opened first (spec 04 §3.4, anchor targets).</summary>
    internal Dictionary<IElement, StructureElement> ElementStructure { get; } = new(ReferenceEqualityComparer<IElement>.Instance);

    /// <summary>The element object that registered each anchor name: a heading's TOC identifier or anchor id, or a section's anchor id.</summary>
    internal Dictionary<string, IElement> AnchorOwners { get; } = new(System.StringComparer.Ordinal);

    /// <summary>In-document go-to actions reserved while pages were written, filled in once the structure tree has object numbers.</summary>
    internal List<PendingGoTo> PendingGoTos { get; } = new();

    /// <summary>Records the anchor <paramref name="name"/> as registered by <paramref name="owner"/>.</summary>
    internal void RecordAnchor(string name, IElement owner) => AnchorOwners[name] = owner;

    /// <summary>The structure-tree builder being populated.</summary>
    public StructureTreeBuilder Tree { get; }

    /// <summary>
    /// Zero-based index of the page currently being rendered. Set by
    /// <see cref="RenderContext"/> in <c>NewPage</c> and at construction.
    /// The canvas reads this when allocating MCIDs so each MCID can be
    /// later paired with its page for /Pg / ParentTree emission.
    /// </summary>
    public int CurrentPageIndex { get; set; }

    /// <summary>
    /// Allocate the next MCID for the current page. MCIDs restart at 0
    /// on every new page per ISO 32000-1 §14.7.4.
    /// </summary>
    public int AllocateMcid() => _nextMcidOnPage++;

    /// <summary>
    /// Reset the per-page MCID counter to 0 and update the current
    /// page index. Called by <c>RenderContext</c> on every
    /// <c>NewPage</c>.
    /// </summary>
    public void ResetPage(int pageIndex)
    {
        _nextMcidOnPage = 0;
        CurrentPageIndex = pageIndex;
    }

    /// <summary>
    /// Back-compat overload - resets the MCID counter without changing
    /// the current page index. Used by tests that don't care about
    /// per-MCID page tracking.
    /// </summary>
    public void ResetPage() => _nextMcidOnPage = 0;

    /// <summary>
    /// Number of marked-content sequences currently open on the active
    /// page. The page-break handler uses this to know how many BDC/EMC
    /// pairs to close on the outgoing page and reopen on the incoming
    /// page.
    /// </summary>
    public int OpenMarkedContentCount => _openRoles.Count;

    /// <summary>Push <paramref name="role"/> onto the open-marked-content stack.</summary>
    internal void PushOpenRole(string role) => _openRoles.Push(role);

    /// <summary>Pop the topmost open-marked-content role.</summary>
    internal void PopOpenRole()
    {
        if (_openRoles.Count > 0) _openRoles.Pop();
    }

    /// <summary>
    /// Returns the currently-open marked-content roles, bottom-up.
    /// Used by the page-break handler to reopen marked-content scopes
    /// on the new page in the same nesting order they had on the old
    /// one.
    /// </summary>
    internal string[] SnapshotOpenRolesBottomUp()
    {
        // Stack.ToArray() returns top-first; reverse to bottom-first.
        var arr = _openRoles.ToArray();
        System.Array.Reverse(arr);
        return arr;
    }
}

/// <summary>An in-document go-to action whose object number was reserved: target page, optional anchor, and view (<c>/Fit</c>, <c>/FitH 0</c>, ...).</summary>
internal sealed record PendingGoTo(int ActionId, int PageIndex, string? Anchor, string View);

/// <summary>Reference equality for element keys, which may override Equals.</summary>
internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
{
    public static readonly ReferenceEqualityComparer<T> Instance = new();
    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
    public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
