using System.Linq;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Elements.Tables;

/// <summary>
/// A table row with support for custom styling and background color
/// </summary>
public class TableRow : IBackgroundColor, IRoleProvider
{
    /// <inheritdoc />
    public SemanticRole SemanticRole => Rendering.Tagging.SemanticRole.TR;

    /// <summary>
    /// The cells in this row
    /// </summary>
    public required TableCell[] Cells { get; set; }

    /// <summary>
    /// TextBlock color for this row (applied to cells without their own text color)
    /// </summary>
    public Color? Color { get; set; }

    /// <summary>
    /// Custom padding for this row (applied to cells without their own padding)
    /// </summary>
    public Thickness? Padding { get; set; }

    /// <summary>
    /// Whether this is a header row
    /// </summary>
    public bool IsHeader { get; set; }

    /// <summary>
    /// Whether this is a footer row
    /// </summary>
    public bool IsFooter { get; set; }

    /// <summary>
    /// Minimum height for this row (optional)
    /// </summary>
    public float? MinHeight { get; set; }

    /// <summary>
    /// Background color for this row (applied to cells without their own background)
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// Creates a TableRow from an array of IElements
    /// If an element is already a TableCell, it's used directly.
    /// Otherwise, it's wrapped in a TableCell.
    /// </summary>
    public static TableRow From(params IElement[] elements)
    {
        return new TableRow
        {
            Cells = elements.Select(e => e is TableCell tc ? tc : new TableCell { Content = e }).ToArray()
        };
    }

    /// <summary>
    /// Creates a TableRow from an array of TableCells
    /// </summary>
    public static TableRow From(params TableCell[] cells)
    {
        return new TableRow { Cells = cells };
    }

    /// <summary>
    /// Marks this row as a header row
    /// </summary>
    public TableRow AsHeader()
    {
        IsHeader = true;
        return this;
    }

    /// <summary>
    /// Marks this row as a footer row
    /// </summary>
    public TableRow AsFooter()
    {
        IsFooter = true;
        return this;
    }

    /// <summary>
    /// Sets the background color for this row
    /// </summary>
    public TableRow WithBackground(Color color)
    {
        BackgroundColor = color;
        return this;
    }

    /// <summary>
    /// Sets the text color for this row
    /// </summary>
    public TableRow WithColor(Color color)
    {
        Color = color;
        return this;
    }

    /// <summary>
    /// Sets the padding for this row
    /// </summary>
    public TableRow WithPadding(float padding)
    {
        Padding = new Thickness(padding);
        return this;
    }

    /// <summary>
    /// Sets the padding for this row with individual values
    /// </summary>
    public TableRow WithPadding(float left, float top, float right, float bottom)
    {
        Padding = new Thickness(left, top, right, bottom);
        return this;
    }

    /// <summary>
    /// Sets the minimum height for this row
    /// </summary>
    public TableRow WithMinHeight(float height)
    {
        MinHeight = height;
        return this;
    }
}