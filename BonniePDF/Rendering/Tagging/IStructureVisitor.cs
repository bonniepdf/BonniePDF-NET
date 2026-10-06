namespace BonniePDF;

/// <summary>
/// Visitor over a structure tree. <see cref="StructureElement.Accept"/>
/// (and the equivalent <see cref="StructureTreeBuilder.Walk"/>) traverse
/// the tree depth-first, calling <see cref="VisitEnter"/> as each node
/// is entered and <see cref="VisitLeave"/> as it is left.
///
/// The writer uses this to translate the in-memory tree into the
/// PDF object graph: assign object IDs on enter, populate <c>/K</c>
/// arrays on leave.
/// </summary>
internal interface IStructureVisitor
{
    /// <summary>Called when traversal enters <paramref name="element"/>,
    /// before any of its children are visited.</summary>
    void VisitEnter(StructureElement element);

    /// <summary>Called when traversal leaves <paramref name="element"/>,
    /// after all its children (if any) have been visited.</summary>
    void VisitLeave(StructureElement element);
}
