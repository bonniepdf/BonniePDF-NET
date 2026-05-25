using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Translates the in-memory <see cref="StructureTreeBuilder"/> tree
/// into the PDF object graph: one object per structure element plus a
/// <c>/StructTreeRoot</c> dictionary that points to the document
/// element.
///
/// Object IDs are allocated via the writer's <c>GetNewId</c>; emission
/// is post-order (leaf-first) so each parent already knows its
/// children's IDs when its <c>/K</c> array is written.
///
/// this feature scope: emits the tree skeleton (roles, parent/child refs,
/// optional /Alt /ActualText /Lang /T metadata, MCID arrays).
/// Per-MCID page references (<c>/Pg</c>) and the <c>/ParentTree</c>
/// auxiliary structure are deferred — they arrive once the canvas
/// starts emitting marked-content and once annotations
/// participate in tagging.
/// </summary>
public sealed class StructureTreeWriter
{
    private readonly IPdfWriter _writer;
    private readonly IAsyncPdfWriter _asyncWriter;

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
    /// Emit the structure tree contained in <paramref name="builder"/>
    /// and return the object ID of the <c>/StructTreeRoot</c>. The
    /// caller is responsible for wiring that ID into
    /// <c>CatalogOptions.StructTreeRootId</c>.
    /// </summary>
    public int Write(StructureTreeBuilder builder)
    {
        int structTreeRootId = _writer.GetNewId();
        var elementIds = new Dictionary<StructureElement, int>(ReferenceEqualityComparer.Instance);

        WriteSubtree(builder.Root, parentId: structTreeRootId, elementIds);
        _writer.WriteObject(structTreeRootId, BuildStructTreeRootDict(builder.Root, elementIds));

        return structTreeRootId;
    }

    private void WriteSubtree(
        StructureElement element,
        int parentId,
        IDictionary<StructureElement, int> elementIds)
    {
        int id = _writer.GetNewId();
        elementIds[element] = id;

        foreach (var child in element.Children)
            WriteSubtree(child, parentId: id, elementIds);

        _writer.WriteObject(id, BuildStructElemDict(element, parentId, elementIds));
    }

    // ── Async ─────────────────────────────────────────────────────────

    /// <summary>
    /// Async counterpart to <see cref="Write(StructureTreeBuilder)"/>.
    /// </summary>
    public async Task<int> WriteAsync(StructureTreeBuilder builder, CancellationToken cancellationToken = default)
    {
        int structTreeRootId = _asyncWriter.GetNewId();
        var elementIds = new Dictionary<StructureElement, int>(ReferenceEqualityComparer.Instance);

        await WriteSubtreeAsync(builder.Root, parentId: structTreeRootId, elementIds, cancellationToken)
            .ConfigureAwait(false);
        await _asyncWriter
            .WriteObjectAsync(structTreeRootId, BuildStructTreeRootDict(builder.Root, elementIds), null, cancellationToken)
            .ConfigureAwait(false);

        return structTreeRootId;
    }

    private async Task WriteSubtreeAsync(
        StructureElement element,
        int parentId,
        IDictionary<StructureElement, int> elementIds,
        CancellationToken cancellationToken)
    {
        int id = _asyncWriter.GetNewId();
        elementIds[element] = id;

        foreach (var child in element.Children)
            await WriteSubtreeAsync(child, parentId: id, elementIds, cancellationToken).ConfigureAwait(false);

        await _asyncWriter
            .WriteObjectAsync(id, BuildStructElemDict(element, parentId, elementIds), null, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Shared dict construction ──────────────────────────────────────

    private static Dictionary<string, string> BuildStructElemDict(
        StructureElement element,
        int parentObjectId,
        IDictionary<StructureElement, int> elementIds)
    {
        var dict = new Dictionary<string, string>
        {
            ["/Type"] = "/StructElem",
            ["/S"] = "/" + element.Role.ToPdfTag(),
            ["/P"] = $"{parentObjectId} 0 R",
        };

        string kids = BuildKidsArray(element, elementIds);
        if (kids != null)
            dict["/K"] = kids;

        if (!string.IsNullOrEmpty(element.Alt))
            dict["/Alt"] = $"({BonniePDF.Rendering.Pdf.PdfSyntax.Escape(element.Alt)})";
        if (!string.IsNullOrEmpty(element.ActualText))
            dict["/ActualText"] = $"({BonniePDF.Rendering.Pdf.PdfSyntax.Escape(element.ActualText)})";
        if (!string.IsNullOrEmpty(element.Lang))
            dict["/Lang"] = $"({BonniePDF.Rendering.Pdf.PdfSyntax.Escape(element.Lang)})";
        if (!string.IsNullOrEmpty(element.Title))
            dict["/T"] = $"({BonniePDF.Rendering.Pdf.PdfSyntax.Escape(element.Title)})";

        return dict;
    }

    private static Dictionary<string, string> BuildStructTreeRootDict(
        StructureElement documentRoot,
        IDictionary<StructureElement, int> elementIds)
    {
        return new Dictionary<string, string>
        {
            ["/Type"] = "/StructTreeRoot",
            // /K references the document StructElem (the in-memory root).
            ["/K"] = $"[ {elementIds[documentRoot]} 0 R ]",
            // /ParentTree and /ParentTreeNextKey are deferred until the
            // canvas wires MCIDs through. Without them
            // VeraPDF still sees "structure tree present" but can't resolve
            // MCID→StructElem ownership for content that has MCIDs.
        };
    }

    /// <summary>
    /// Build the <c>/K</c> array string for an element: child structure
    /// element references first, then bare MCIDs. Returns <c>null</c>
    /// when the element has neither children nor MCIDs (the spec allows
    /// /K to be absent on a leaf without content).
    /// </summary>
    private static string BuildKidsArray(StructureElement element, IDictionary<StructureElement, int> ids)
    {
        if (element.Children.Count == 0 && element.McIds.Count == 0)
            return null;

        var parts = new List<string>(element.Children.Count + element.McIds.Count);
        foreach (var child in element.Children)
            parts.Add($"{ids[child]} 0 R");
        foreach (var mcid in element.McIds)
            parts.Add(mcid.ToString(CultureInfo.InvariantCulture));

        return "[ " + string.Join(" ", parts) + " ]";
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
    public bool Equals(StructureElement x, StructureElement y) => ReferenceEquals(x, y);
    public int GetHashCode(StructureElement obj) =>
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
