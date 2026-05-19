namespace BonniePDF.Fluent;

/// <summary>
/// A reusable document fragment that can compose content into a container.
/// Implement this interface to create reusable building blocks for your documents.
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Composes content into the given container using the fluent API.
    /// </summary>
    void Compose(IContentContainer container);
}
