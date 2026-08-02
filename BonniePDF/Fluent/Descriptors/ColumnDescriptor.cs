using BonniePDF.Elements.Layout;

namespace BonniePDF.Fluent.Descriptors;

/// <summary>
/// Describes a vertical column layout. Provides scoped methods for adding items and configuring spacing.
/// Maps to a <see cref="VerticalStack"/> element internally.
/// </summary>
public sealed class ColumnDescriptor
{
    internal VerticalStack Stack { get; } = new();

    /// <summary>
    /// Sets the vertical spacing between items in points.
    /// </summary>
    public void Spacing(float value) => Stack.Spacing = value;

    /// <summary>
    /// Adds an item to the column and returns a container for configuring its content.
    /// </summary>
    /// <returns>An <see cref="IContentContainer"/> for chaining content and styling.</returns>
    public IContentContainer Item()
    {
        var slot = new ContentSlot();
        Stack.Add(slot);
        return slot;
    }
}
