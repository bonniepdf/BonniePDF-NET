
namespace BonniePDF.Fluent;

/// <summary>
/// A slot that can hold a single child element.
/// All fluent extension methods operate on this interface.
/// This is the core of the BonniePDF fluent API chain pattern.
/// </summary>
public interface IContentContainer
{
    /// <summary>The wrapped element, if any.</summary>
    IElement? Child { get; set; }
}
