using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Translates the in-memory <see cref="StructureTreeBuilder"/> tree
/// into the PDF object graph: one object per structure element, a
/// <c>/ParentTree</c> number tree for MCID→StructElem reverse lookup
/// (ISO 32000-1 §14.7.4), and a <c>/StructTreeRoot</c> dictionary
/// that points to both.
///
/// Object IDs are allocated via the writer's <c>GetNewId</c>; emission
/// is post-order (leaf-first) so each parent already knows its
/// children's IDs when its <c>/K</c> array is written.
///
/// Each StructElem with MCIDs gets either:
/// <list type="bullet">
///   <item><description><c>/K [ int int … ]</c> + <c>/Pg &lt;page-ref&gt;</c>
///     when all its MCIDs are on the same page (the common case for
///     elements that don't overflow).</description></item>
///   <item><description><c>/K [ &lt;&lt;/Type /MCR /Pg p1 /MCID m1&gt;&gt; … ]</c>
///     when MCIDs span pages (an element that page-broke after the
///     this feature close+reopen fix runs).</description></item>
/// </list>
/// </summary>
public sealed class StructureTreeWriter
{
    // Exactly one of these is non-null, depending on which constructor was used;
    // the sync members (Write/WriteSubtree/...) only ever run on an instance built
    // via the sync constructor, and likewise for the async members.
    private readonly IPdfWriter? _writer;
    private readonly IAsyncPdfWriter? _asyncWriter;

    /// <summary>Creates a structure-tree writer that emits synchronously.</summary>
    public StructureTreeWriter(IPdfWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _asyncWriter = null;
    }

    /// <summary>Creates a structure-tree writer that emits asynchronously.</summary>
    public StructureTreeWriter(IAsyncPdfWriter asyncWriter)
    {
        _asyncWriter = asyncWriter ?? throw new ArgumentNullException(nameof(asyncWriter));
        _writer = null;
    }

    // ── Sync ──────────────────────────────────────────────────────────

    /// <summary>
    /// Emit the structure tree from <paramref name="builder"/> and
    /// return the object ID of the <c>/StructTreeRoot</c>. Caller
    /// wires that ID into <c>CatalogOptions.StructTreeRootId</c> and
    /// also sets <c>/StructParents</c> on each page dict to its
    /// zero-based page index (matches the keys used in
    /// <c>/ParentTree</c>).
    /// </summary>
    /// <param name="builder">Populated structure tree.</param>
    /// <param name="pageIds">
    /// Page object IDs in document order (index = page index). Empty
    /// or null when no page-aware output is needed (test fixtures).
    /// </param>
    /// <param name="annotBindings">
    /// Annotation /StructParent bindings collected during serialization
    ///. Each entry wires a link annotation into the parent tree
    /// and emits an /OBJR child on the owning Link struct element.
    /// </param>
    public int Write(
        StructureTreeBuilder builder,
        IList<int>? pageIds = null,
        IList<(StructureElement structElem, int annotId, int pageObjectId, int structParentKey)>? annotBindings = null)
    {
        int structTreeRootId = _writer!.GetNewId();
        int parentTreeId = (pageIds != null && pageIds.Count > 0) ? _writer!.GetNewId() : 0;

        var elementIds = new Dictionary<StructureElement, int>(ReferenceEqualityComparer.Instance);
        var parentTreeNums = pageIds != null
            ? BuildEmptyParentTreeNums(pageIds.Count)
            : null;

        WriteSubtree(builder.Root, structTreeRootId, elementIds, pageIds, parentTreeNums);

        if (parentTreeId > 0)
            _writer!.WriteObject(parentTreeId, BuildParentTreeDict(parentTreeNums, elementIds, annotBindings));

        _writer!.WriteObject(structTreeRootId,
            BuildStructTreeRootDict(builder.Root, elementIds, parentTreeId, parentTreeNums, annotBindings));

        return structTreeRootId;
    }

    private void WriteSubtree(
        StructureElement element,
        int parentId,
        IDictionary<StructureElement, int> elementIds,
        IList<int>? pageIds,
        IList<List<(int mcid, StructureElement elem)>>? parentTreeNums)
    {
        int id = _writer!.GetNewId();
        elementIds[element] = id;

        foreach (var child in element.Children)
            WriteSubtree(child, id, elementIds, pageIds, parentTreeNums);

        // Record this element's contribution to /ParentTree for every
        // (page, mcid) pair it claims.
        if (parentTreeNums != null)
            RecordParentTreeContributions(element, parentTreeNums);

        _writer!.WriteObject(id, BuildStructElemDict(element, parentId, elementIds, pageIds));
    }

    // ── Async ─────────────────────────────────────────────────────────

    /// <summary>Async counterpart to <see cref="Write"/>.</summary>
    public async Task<int> WriteAsync(
        StructureTreeBuilder builder,
        IList<int>? pageIds = null,
        IList<(StructureElement structElem, int annotId, int pageObjectId, int structParentKey)>? annotBindings = null,
        CancellationToken cancellationToken = default)
    {
        int structTreeRootId = _asyncWriter!.GetNewId();
        int parentTreeId = (pageIds != null && pageIds.Count > 0) ? _asyncWriter!.GetNewId() : 0;

        var elementIds = new Dictionary<StructureElement, int>(ReferenceEqualityComparer.Instance);
        var parentTreeNums = pageIds != null
            ? BuildEmptyParentTreeNums(pageIds.Count)
            : null;

        await WriteSubtreeAsync(builder.Root, structTreeRootId, elementIds, pageIds, parentTreeNums, cancellationToken)
            .ConfigureAwait(false);

        if (parentTreeId > 0)
        {
            await _asyncWriter!
                .WriteObjectAsync(parentTreeId, BuildParentTreeDict(parentTreeNums, elementIds, annotBindings), null, cancellationToken)
                .ConfigureAwait(false);
        }

        await _asyncWriter!
            .WriteObjectAsync(structTreeRootId,
                BuildStructTreeRootDict(builder.Root, elementIds, parentTreeId, parentTreeNums, annotBindings), null, cancellationToken)
            .ConfigureAwait(false);

        return structTreeRootId;
    }

    private async Task WriteSubtreeAsync(
        StructureElement element,
        int parentId,
        IDictionary<StructureElement, int> elementIds,
        IList<int>? pageIds,
        IList<List<(int mcid, StructureElement elem)>>? parentTreeNums,
        CancellationToken cancellationToken)
    {
        int id = _asyncWriter!.GetNewId();
        elementIds[element] = id;

        foreach (var child in element.Children)
            await WriteSubtreeAsync(child, id, elementIds, pageIds, parentTreeNums, cancellationToken).ConfigureAwait(false);

        if (parentTreeNums != null)
            RecordParentTreeContributions(element, parentTreeNums);

        await _asyncWriter!
            .WriteObjectAsync(id, BuildStructElemDict(element, parentId, elementIds, pageIds), null, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Shared dict construction ──────────────────────────────────────

    private static Dictionary<string, string> BuildStructElemDict(
        StructureElement element,
        int parentObjectId,
        IDictionary<StructureElement, int> elementIds,
        IList<int>? pageIds)
    {
        var dict = new Dictionary<string, string>
        {
            ["/Type"] = "/StructElem",
            ["/S"] = "/" + element.Role.ToPdfTag(),
            ["/P"] = $"{parentObjectId} 0 R",
        };

        // /K + /Pg emission. Three cases:
        //   1. No children, no MCIDs => omit /K (allowed by spec).
        //   2. Children only, or MCIDs that all share a single page =>
        //      /K is a mix of struct refs (children) and bare ints (MCIDs);
        //      /Pg references the single shared page.
        //   3. MCIDs span multiple pages => /K entries become /MCR dicts
        //      with explicit /Pg + /MCID per entry; no /Pg on the elem.
        BuildKidsAndPg(element, elementIds, pageIds, out string? kids, out int? singlePageObjectId);
        if (kids != null) dict["/K"] = kids;
        if (singlePageObjectId.HasValue) dict["/Pg"] = $"{singlePageObjectId.Value} 0 R";

        // PDF text strings: use EncodeMetadataString so non-Latin-1 chars
        // (em-dash, smart quotes, CJK, etc.) become a UTF-16BE hex literal
        // <FEFF…> rather than getting byte-truncated by the Latin-1 writer.
        if (!string.IsNullOrEmpty(element.Alt))
            dict["/Alt"] = BonniePDF.Rendering.Pdf.PdfSyntax.EncodeMetadataString(element.Alt);
        if (!string.IsNullOrEmpty(element.ActualText))
            dict["/ActualText"] = BonniePDF.Rendering.Pdf.PdfSyntax.EncodeMetadataString(element.ActualText);
        if (!string.IsNullOrEmpty(element.Lang))
            dict["/Lang"] = BonniePDF.Rendering.Pdf.PdfSyntax.EncodeMetadataString(element.Lang);
        if (!string.IsNullOrEmpty(element.Title))
            dict["/T"] = BonniePDF.Rendering.Pdf.PdfSyntax.EncodeMetadataString(element.Title);

        // /A - attributes dictionary (raw PDF syntax provided by caller).
        // Tables use this for /ColSpan; future stages may use it for
        // /RowSpan and list characteristics.
        if (!string.IsNullOrEmpty(element.Attributes))
            dict["/A"] = element.Attributes;

        return dict;
    }

    private static Dictionary<string, string> BuildStructTreeRootDict(
        StructureElement documentRoot,
        IDictionary<StructureElement, int> elementIds,
        int parentTreeId,
        IList<List<(int mcid, StructureElement elem)>>? parentTreeNums,
        IList<(StructureElement structElem, int annotId, int pageObjectId, int structParentKey)>? annotBindings = null)
    {
        var dict = new Dictionary<string, string>
        {
            ["/Type"] = "/StructTreeRoot",
            ["/K"] = $"[ {elementIds[documentRoot]} 0 R ]",
        };

        if (parentTreeId > 0)
        {
            dict["/ParentTree"] = $"{parentTreeId} 0 R";
            // /ParentTreeNextKey = max key used + 1.
            // For pages it's the page count; if there are annotation
            // bindings with higher keys (this feature uses 10_000+), use those.
            int nextKey = parentTreeNums?.Count ?? 0;
            if (annotBindings != null && annotBindings.Count > 0)
            {
                int maxAnnotKey = annotBindings.Max(b => b.structParentKey);
                nextKey = Math.Max(nextKey, maxAnnotKey + 1);
            }
            dict["/ParentTreeNextKey"] = nextKey.ToString(CultureInfo.InvariantCulture);
        }

        return dict;
    }

    private static Dictionary<string, string> BuildParentTreeDict(
        IList<List<(int mcid, StructureElement elem)>>? parentTreeNums,
        IDictionary<StructureElement, int> elementIds,
        IList<(StructureElement structElem, int annotId, int pageObjectId, int structParentKey)>? annotBindings = null)
    {
        // PDF number-tree form: /Nums [ key1 value1 key2 value2 … ].
        // Value for each page key is an array indexed by MCID, where
        // entry i is the StructElem ref owning MCID i on that page.
        var parts = new List<string>();
        for (int pageKey = 0; pageKey < parentTreeNums!.Count; pageKey++)
        {
            var entries = parentTreeNums[pageKey];
            parts.Add(pageKey.ToString(CultureInfo.InvariantCulture));

            if (entries.Count == 0)
            {
                // Empty page-level array - keeps the key→value mapping
                // contiguous even for pages with no marked content.
                parts.Add("[ ]");
                continue;
            }

            // Build an array slot for every MCID 0..max(mcid). Most
            // pages have a dense 0..N-1, but be defensive.
            int maxMcid = entries.Max(e => e.mcid);
            var slots = new string[maxMcid + 1];
            for (int i = 0; i < slots.Length; i++) slots[i] = "null";
            foreach (var (mcid, elem) in entries)
                slots[mcid] = $"{elementIds[elem]} 0 R";

            parts.Add("[ " + string.Join(" ", slots) + " ]");
        }

        // Annotation struct parent entries: each link annotation gets
        // its own key mapping directly to its containing Link struct element.
        if (annotBindings != null)
        {
            foreach (var (structElem, _, _, structParentKey) in annotBindings)
            {
                if (elementIds.TryGetValue(structElem, out int elemId))
                {
                    parts.Add(structParentKey.ToString(CultureInfo.InvariantCulture));
                    parts.Add($"{elemId} 0 R");
                }
            }
        }

        return new Dictionary<string, string>
        {
            ["/Nums"] = "[ " + string.Join(" ", parts) + " ]",
        };
    }

    // ── Per-element /K + /Pg computation ────────────────────────────

    private static void BuildKidsAndPg(
        StructureElement element,
        IDictionary<StructureElement, int> elementIds,
        IList<int>? pageIds,
        out string? kids,
        out int? singlePageObjectId)
    {
        singlePageObjectId = null;

        if (element.Children.Count == 0 && element.McIds.Count == 0 && element.ObjrAnnotations.Count == 0)
        {
            kids = null;
            return;
        }

        var parts = new List<string>(element.Children.Count + element.McIds.Count + element.ObjrAnnotations.Count);

        foreach (var child in element.Children)
            parts.Add($"{elementIds[child]} 0 R");

        if (element.McIds.Count > 0)
        {
            // Determine whether all MCIDs share a single page.
            bool allSamePage = true;
            int firstPage = element.McIdPages.Count > 0 ? element.McIdPages[0] : 0;
            for (int i = 1; i < element.McIdPages.Count; i++)
            {
                if (element.McIdPages[i] != firstPage) { allSamePage = false; break; }
            }

            if (allSamePage && pageIds != null && firstPage < pageIds.Count)
            {
                singlePageObjectId = pageIds[firstPage];
                foreach (var mcid in element.McIds)
                    parts.Add(mcid.ToString(CultureInfo.InvariantCulture));
            }
            else if (pageIds != null)
            {
                // MCIDs span pages - emit /MCR dicts with explicit /Pg.
                for (int i = 0; i < element.McIds.Count; i++)
                {
                    int mcid = element.McIds[i];
                    int pageIndex = i < element.McIdPages.Count ? element.McIdPages[i] : 0;
                    if (pageIndex < pageIds.Count)
                    {
                        parts.Add($"<< /Type /MCR /Pg {pageIds[pageIndex]} 0 R /MCID {mcid} >>");
                    }
                    else
                    {
                        // Defensive fallback - bare int.
                        parts.Add(mcid.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }
            else
            {
                // Test mode (no page IDs) - bare ints, no /Pg.
                foreach (var mcid in element.McIds)
                    parts.Add(mcid.ToString(CultureInfo.InvariantCulture));
            }
        }

        // /OBJR children for annotation-backed elements.
        foreach (var (annotId, pageObjId) in element.ObjrAnnotations)
            parts.Add($"<< /Type /OBJR /Pg {pageObjId} 0 R /Obj {annotId} 0 R >>");

        kids = "[ " + string.Join(" ", parts) + " ]";
    }

    // ── /ParentTree bookkeeping ─────────────────────────────────────

    private static List<List<(int mcid, StructureElement elem)>> BuildEmptyParentTreeNums(int pageCount)
    {
        var nums = new List<List<(int mcid, StructureElement elem)>>(pageCount);
        for (int i = 0; i < pageCount; i++)
            nums.Add(new List<(int mcid, StructureElement elem)>());
        return nums;
    }

    private static void RecordParentTreeContributions(
        StructureElement element,
        IList<List<(int mcid, StructureElement elem)>> parentTreeNums)
    {
        for (int i = 0; i < element.McIds.Count; i++)
        {
            int mcid = element.McIds[i];
            int pageIndex = i < element.McIdPages.Count ? element.McIdPages[i] : 0;
            if (pageIndex < parentTreeNums.Count)
                parentTreeNums[pageIndex].Add((mcid, element));
        }
    }
}

/// <summary>
/// Reference-equality comparer for <see cref="StructureElement"/>
/// keys. The BCL provides a non-generic
/// <c>ReferenceEqualityComparer.Instance</c>; this generic shim avoids
/// the boxing that would otherwise happen when used as a dictionary
/// key comparer.
/// </summary>
internal sealed class ReferenceEqualityComparer : IEqualityComparer<StructureElement>
{
    public static readonly ReferenceEqualityComparer Instance = new();
    public bool Equals(StructureElement? x, StructureElement? y) => ReferenceEquals(x, y);
    public int GetHashCode(StructureElement obj) =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
