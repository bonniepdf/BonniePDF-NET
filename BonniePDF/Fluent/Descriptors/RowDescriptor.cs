using BonniePDF.Core;
using BonniePDF.Elements.Layout;

namespace BonniePDF.Fluent.Descriptors;

/// <summary>
/// Describes a horizontal row layout. Provides scoped methods for adding items with
/// relative, constant, or auto sizing. Maps to a <see cref="HorizontalStack"/> element internally.
/// </summary>
public sealed class RowDescriptor
{
    internal HorizontalStack Stack { get; } = new();
    private bool _hasRelativeOrConstant;

    /// <summary>
    /// Sets the horizontal spacing between items in points.
    /// </summary>
    public void Spacing(float value) => Stack.Spacing = value;

    /// <summary>
    /// Sets the vertical alignment of items within the row.
    /// </summary>
    public void AlignItems(HorizontalAlignment alignment) => Stack.Alignment = alignment;

    /// <summary>
    /// Adds an item that takes proportional space (like CSS flex-grow).
    /// Multiple relative items share available space according to their weights.
    /// </summary>
    /// <param name="weight">The proportional weight (default 1). Higher values take more space.</param>
    /// <returns>An <see cref="IContentContainer"/> for chaining content and styling.</returns>
    public IContentContainer RelativeItem(float weight = 1f)
    {
        EnsureWeightedDistribution();
        var slot = new ContentSlot();
        Stack.Add(slot, weight);
        return slot;
    }

    /// <summary>
    /// Adds an item with a fixed width in points.
    /// </summary>
    /// <param name="width">The fixed width in points.</param>
    /// <returns>An <see cref="IContentContainer"/> for chaining content and styling.</returns>
    public IContentContainer ConstantItem(float width)
    {
        EnsureWeightedDistribution();
        // Use a container with fixed width wrapping a slot
        var slot = new ContentSlot();
        var container = new Container { Width = width, Child = slot };
        Stack.Add(container, 0f); // Weight 0 = uses preferred width (the fixed Width)
        return slot;
    }

    /// <summary>
    /// Adds an item that sizes to its content.
    /// </summary>
    /// <returns>An <see cref="IContentContainer"/> for chaining content and styling.</returns>
    public IContentContainer AutoItem()
    {
        var slot = new ContentSlot();
        if (_hasRelativeOrConstant)
        {
            // In weighted mode, auto items get weight 0 (use preferred width)
            Stack.Add(slot, 0f);
        }
        else
        {
            Stack.Add(slot);
        }

        return slot;
    }

    private void EnsureWeightedDistribution()
    {
        if (!_hasRelativeOrConstant)
        {
            Stack.WidthDistribution = WidthDistribution.Weighted;
            _hasRelativeOrConstant = true;
        }
    }
}
