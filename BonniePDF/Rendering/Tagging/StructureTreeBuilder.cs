using System.Collections.Generic;

namespace BonniePDF.Rendering.Tagging;

/// <summary>
/// Stack-based builder for the PDF structure tree. The renderer pushes
/// a structure element when it starts emitting a tagged region (heading,
/// paragraph, figure, …) and pops it when the region ends. The canvas
/// attaches MCIDs to the current top-of-stack element as marked-content
/// is written.
///
/// A new builder starts with an implicit <see cref="SemanticRole.Document"/>
/// root already open and at the top of the stack — every element added
/// is a descendant of Document.
/// </summary>
public sealed class StructureTreeBuilder
{
    private readonly Stack<StructureElement> _stack;
    private readonly StructureElement _root;

    /// <summary>Creates a new builder with an implicit Document root.</summary>
    public StructureTreeBuilder()
    {
        _root = new StructureElement(SemanticRole.Document);
        _stack = new Stack<StructureElement>();
        _stack.Push(_root);
    }

    /// <summary>The document root. Always returns the same instance for
    /// the lifetime of this builder.</summary>
    public StructureElement Root => _root;

    /// <summary>
    /// The element currently at the top of the open-element stack —
    /// the element that <see cref="AddMcid"/> attaches to and that new
    /// <see cref="Begin"/> calls nest under.
    /// </summary>
    public StructureElement Current => _stack.Peek();

    /// <summary>
    /// Open a new structure element under the current one and make it
    /// the new current. Must be paired with a subsequent <see cref="End"/>.
    /// </summary>
    /// <returns>The newly created element, in case the caller wants to
    /// set metadata (Alt, Lang, …) directly.</returns>
    public StructureElement Begin(SemanticRole role)
    {
        var element = new StructureElement(role) { Parent = Current };
        Current.Children.Add(element);
        _stack.Push(element);
        return element;
    }

    /// <summary>
    /// Close the current structure element, returning to its parent.
    /// Throws <see cref="StructureTreeException"/> if called more times
    /// than <see cref="Begin"/> (the document root cannot be closed —
    /// it is owned by the builder).
    /// </summary>
    public void End()
    {
        if (_stack.Count <= 1)
            throw new StructureTreeException(
                "End() called with no open structure element. The document root cannot be closed; check for an End() without a matching Begin().");
        _stack.Pop();
    }

    /// <summary>
    /// Attach a marked-content identifier (MCID) to the current
    /// structure element. Called by the canvas when marked-content is
    /// emitted in the content stream.
    /// </summary>
    public void AddMcid(int mcid)
    {
        Current.McIds.Add(mcid);
    }

    /// <summary>Set <c>/Alt</c> on the current element.</summary>
    public void SetAlt(string text) => Current.Alt = text;

    /// <summary>Set <c>/ActualText</c> on the current element.</summary>
    public void SetActualText(string text) => Current.ActualText = text;

    /// <summary>Set <c>/Lang</c> on the current element.</summary>
    public void SetLang(string lang) => Current.Lang = lang;

    /// <summary>Set <c>/T</c> on the current element.</summary>
    public void SetTitle(string title) => Current.Title = title;

    /// <summary>
    /// Traverse the entire tree from the root depth-first, invoking
    /// <paramref name="visitor"/> on each node.
    /// </summary>
    public void Walk(IStructureVisitor visitor) => _root.Accept(visitor);
}
