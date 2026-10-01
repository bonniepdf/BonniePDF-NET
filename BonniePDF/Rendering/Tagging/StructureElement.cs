using System.Collections.Generic;
using System.Text;

namespace BonniePDF.Rendering.Tagging;

/// <summary>
/// A node in the PDF structure tree. Carries a <see cref="SemanticRole"/>,
/// a parent reference, an ordered list of children, and the list of
/// marked-content identifiers (MCIDs) this element claims from the
/// content stream.
///
/// Optional metadata (<see cref="Alt"/>, <see cref="ActualText"/>,
/// <see cref="Lang"/>, <see cref="Title"/>) is written into the PDF
/// structure dictionary when set.
/// </summary>
internal sealed class StructureElement
{
    /// <summary>
    /// Creates a new structure element with the given role. Parent is
    /// assigned by <see cref="StructureTreeBuilder.Begin"/>; do not set
    /// it directly.
    /// </summary>
    public StructureElement(SemanticRole role)
    {
        Role = role;
        Children = new List<StructureElement>();
        McIds = new List<int>();
    }

    /// <summary>The semantic role (PDF tag) for this element.</summary>
    public SemanticRole Role { get; }

    /// <summary>
    /// The parent structure element. <c>null</c> only on the document
    /// root.
    /// </summary>
    public StructureElement? Parent { get; internal set; }

    /// <summary>Child structure elements in document order.</summary>
    public IList<StructureElement> Children { get; }

    /// <summary>
    /// Marked-content identifiers (MCIDs) for content runs that belong
    /// to this structure element. Populated by the canvas as text and
    /// images are drawn inside an open structure element.
    /// </summary>
    public IList<int> McIds { get; }

    /// <summary>
    /// Zero-based page index for each entry in <see cref="McIds"/>,
    /// parallel to that list (<c>McIdPages[i]</c> is the page on which
    /// <c>McIds[i]</c> was emitted). The structure-tree writer uses
    /// this to emit <c>/Pg</c> on the StructElem (when all MCIDs share
    /// a page) or <c>/MCR</c> dicts with explicit <c>/Pg</c> per entry
    /// (when MCIDs span multiple pages - which happens whenever a
    /// tagged element overflows past a page break).
    /// </summary>
    public IList<int> McIdPages { get; } = new List<int>();

    /// <summary>
    /// Annotation object-reference bindings for this element. Each entry
    /// is an (annotObjectId, pageObjectId) pair that the structure-tree
    /// writer emits as an <c>/OBJR</c> child in this element's <c>/K</c>
    /// array and registers in the <c>/ParentTree</c>.
    /// </summary>
    public IList<(int annotId, int pageObjectId)> ObjrAnnotations { get; } = new System.Collections.Generic.List<(int, int)>();

    /// <summary>
    /// <c>/Alt</c> alternate text - read by assistive tech in place of
    /// the visible content. Required by PDF/UA on <see cref="SemanticRole.Figure"/>
    /// elements that aren't <see cref="SemanticRole.Artifact"/>.
    /// </summary>
    public string? Alt { get; set; }

    /// <summary>
    /// <c>/ActualText</c> - the literal text equivalent of the
    /// rendered content. Used for ligatures, decorative letterforms, and
    /// figures whose visible content is itself words (e.g. a logo).
    /// </summary>
    public string? ActualText { get; set; }

    /// <summary>
    /// <c>/Lang</c> - BCP-47 language tag for this subtree.
    /// Overrides the document-level language for content underneath.
    /// </summary>
    public string? Lang { get; set; }

    /// <summary>
    /// <c>/T</c> - a short title for the structure element. Used by
    /// some viewers' navigation panes.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// <c>/A</c> - raw structure-element attributes dictionary as PDF
    /// syntax (e.g. <c>"&lt;&lt; /O /Table /ColSpan 2 &gt;&gt;"</c>).
    /// Used for table cell column/row spans, list label characteristics,
    /// and other attribute-owner-namespaced metadata per ISO 32000-1
    /// §14.7.5. Emitted verbatim by the structure-tree writer.
    /// </summary>
    public string? Attributes { get; set; }

    /// <summary>
    /// Visit this element and (recursively) all of its descendants in
    /// document order.
    /// </summary>
    public void Accept(IStructureVisitor visitor)
    {
        visitor.VisitEnter(this);
        foreach (var child in Children)
            child.Accept(visitor);
        visitor.VisitLeave(this);
    }

    /// <summary>
    /// Returns a human-readable tree dump, mainly for test golden
    /// comparison. Format:
    /// <code>
    /// Document
    ///   H1 [mcids: 0]
    ///   P [mcids: 1] /Alt=cover-image
    ///     Figure [mcids: 2]
    /// </code>
    /// </summary>
    public string ToTreeString()
    {
        var sb = new StringBuilder();
        Append(sb, this, depth: 0);
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, StructureElement node, int depth)
    {
        for (int i = 0; i < depth; i++) sb.Append("  ");
        sb.Append(node.Role.ToPdfTag());
        if (node.McIds.Count > 0)
        {
            sb.Append(" [mcids: ");
            for (int i = 0; i < node.McIds.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(node.McIds[i]);
            }
            sb.Append(']');
        }
        if (!string.IsNullOrEmpty(node.Alt)) { sb.Append(" /Alt="); sb.Append(node.Alt); }
        if (!string.IsNullOrEmpty(node.ActualText)) { sb.Append(" /ActualText="); sb.Append(node.ActualText); }
        if (!string.IsNullOrEmpty(node.Lang)) { sb.Append(" /Lang="); sb.Append(node.Lang); }
        if (!string.IsNullOrEmpty(node.Title)) { sb.Append(" /T="); sb.Append(node.Title); }
        sb.Append('\n');
        foreach (var child in node.Children)
            Append(sb, child, depth + 1);
    }
}
