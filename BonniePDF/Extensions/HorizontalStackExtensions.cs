using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for horizontal stacks
/// </summary>
public static class HorizontalStackExtensions
{
    /// <summary>
    /// Adds a child element to the horizontal stack
    /// </summary>
    public static HorizontalStack Item(this HorizontalStack s, IElement element)
    {
        s.AddChild(element);
        return s;
    }

    /// <summary>
    /// Adds a child element with a specific weight to the horizontal stack
    /// </summary>
    public static HorizontalStack Item(this HorizontalStack s, IElement element, float weight)
    {
        s.AddChild(element, weight);
        return s;
    }

    /// <summary>
    /// Sets the spacing between elements in the horizontal stack
    /// </summary>
    public static HorizontalStack Spacing(this HorizontalStack s, float value)
    {
        s.Spacing = value;
        return s;
    }

    /// <summary>
    /// Sets the vertical alignment to the start
    /// </summary>
    public static HorizontalStack AlignStart(this HorizontalStack s)
    {
        s.Alignment = HorizontalAlignment.Start;
        return s;
    }

    /// <summary>
    /// Sets the vertical alignment to center
    /// </summary>
    public static HorizontalStack AlignCenter(this HorizontalStack s)
    {
        s.Alignment = HorizontalAlignment.Center;
        return s;
    }

    /// <summary>
    /// Sets the vertical alignment to the end
    /// </summary>
    public static HorizontalStack AlignEnd(this HorizontalStack s)
    {
        s.Alignment = HorizontalAlignment.End;
        return s;
    }

    /// <summary>
    /// Sets the width distribution to be equal for all children
    /// </summary>
    public static HorizontalStack EqualWidth(this HorizontalStack s)
    {
        s.WidthDistribution = WidthDistribution.Equal;
        return s;
    }

    /// <summary>
    /// Sets the width distribution to be based on weights
    /// </summary>
    public static HorizontalStack WeightedWidth(this HorizontalStack s)
    {
        s.WidthDistribution = WidthDistribution.Weighted;
        return s;
    }

    /// <summary>
    /// Sets the width distribution to be based on the intrinsic width of the children
    /// </summary>
    public static HorizontalStack AutoWidth(this HorizontalStack s)
    {
        s.WidthDistribution = WidthDistribution.Auto;
        return s;
    }
}
