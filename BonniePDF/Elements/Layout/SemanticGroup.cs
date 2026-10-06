
namespace BonniePDF;

/// <summary>
/// A layout container that wraps its children in an explicit PDF/UA
/// semantic structure role. Use this when a block of content has a
/// specific role (e.g. Sect, Art, BlockQuote) that isn't expressed
/// by any of the individual child elements.
///
/// When tagging is enabled (<see cref="AccessibilityMode.PdfUa1"/>)
/// the pipeline wraps the whole group in a single structure element
/// with the configured <see cref="SemanticRole"/>. Children that are themselves
/// <see cref="IRoleProvider"/> get their own nested structure elements.
/// </summary>
public class SemanticGroup : VerticalStack, IRoleProvider
{
    private readonly SemanticRole _role;

    /// <summary>
    /// Creates a SemanticGroup with the specified semantic role.
    /// </summary>
    public SemanticGroup(SemanticRole role) => _role = role;

    /// <inheritdoc />
    public SemanticRole SemanticRole => _role;
}
