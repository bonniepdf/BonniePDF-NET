namespace BonniePDF;

/// <summary>
/// Marker interface for elements that declare a PDF structure role
/// (heading level, paragraph, figure, …). When tagging is enabled, the
/// render pipeline reads this role, pushes a corresponding structure
/// element onto the tree, wraps the element's render in a
/// marked-content sequence, and pops the element afterwards.
///
/// Elements that don't implement this interface render as before with
/// no structure-tree contribution. Implementing the interface is
/// strictly additive - untagged documents are unaffected because the
/// pipeline checks <c>RenderContext.Tagging</c> first.
/// </summary>
public interface IRoleProvider
{
    /// <summary>
    /// The PDF structure role this element claims (e.g.
    /// <see cref="SemanticRole.H1"/>, <see cref="SemanticRole.P"/>,
    /// <see cref="SemanticRole.Figure"/>).
    /// </summary>
    SemanticRole SemanticRole { get; }
}
