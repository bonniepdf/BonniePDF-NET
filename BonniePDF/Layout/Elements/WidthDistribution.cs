namespace BonniePDF.Layout.Elements;

/// <summary>
/// Width distribution mode for HorizontalStack children
/// </summary>
public enum WidthDistribution
{
    /// <summary>Each child takes equal width</summary>
    Equal,

    /// <summary>Children are sized based on their weight (specified when adding)</summary>
    Weighted,

    /// <summary>Children use their preferred width (falls back to equal if not available)</summary>
    Auto
}