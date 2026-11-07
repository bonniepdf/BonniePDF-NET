using System.Linq;

namespace BonniePDF;

/// <summary>
/// A table row with support for custom styling and background color
/// </summary>
public class TableRow : IBackgroundColor, IRoleProvider
{
    // Text colour and padding (and, on a row, minimum height and background), shared with every
    // cell or row that has the same values.
    private CellOptions _cell = CellOptions.Default;

    /// <summary>Always <c>TR</c>.</summary>
    public SemanticRole SemanticRole => SemanticRole.TR;

    /// <summary>
    /// The cells in this row
    /// </summary>
    public required TableCell[] Cells { get; set; }

    /// <summary>
    /// TextBlock color for this row (applied to cells without their own text color)
    /// </summary>
    public Color? Color
    {
        get => _cell.Color;
        set => _cell = _cell.With(value, static (o, v) => o with { Color = v });
    }

    /// <summary>
    /// Custom padding for this row (applied to cells without their own padding)
    /// </summary>
    public Thickness? Padding
    {
        get => _cell.Padding;
        set => _cell = _cell.With(value, static (o, v) => o with { Padding = v });
    }

    /// <summary>
    /// Whether this is a header row
    /// </summary>
    public bool IsHeader { get; set; }

    /// <summary>
    /// Whether this is a footer row
    /// </summary>
    public bool IsFooter { get; set; }

    /// <summary>
    /// The amount this body row adds to the table's running total, when the table has a
    /// <see cref="Table.RunningTotalColumn"/>. Rows without one add nothing.
    /// </summary>
    public decimal? RunningValue { get; set; }

    /// <summary>
    /// Minimum height for this row (optional)
    /// </summary>
    public float? MinHeight
    {
        get => _cell.MinHeight;
        set => _cell = _cell.With(value, static (o, v) => o with { MinHeight = v });
    }

    /// <summary>
    /// Background color for this row (applied to cells without their own background)
    /// </summary>
    public Color? BackgroundColor
    {
        get => _cell.BackgroundColor;
        set => _cell = _cell.With(value, static (o, v) => o with { BackgroundColor = v });
    }

    /// <summary>
    /// Creates a TableRow from an array of IElements
    /// If an element is already a TableCell, it's used directly.
    /// Otherwise, it's wrapped in a TableCell.
    /// </summary>
    public static TableRow From(params IElement[] elements)
    {
        var cells = new TableCell[elements.Length];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = elements[i] is TableCell cell ? cell : new TableCell { Content = elements[i] };
        return new TableRow { Cells = cells };
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