
namespace BonniePDF;

/// <summary>
/// A table cell with support for custom styling, background color, and padding overrides
/// </summary>
public class TableCell : BorderedElementBase, IRoleProvider
{
    // Text colour and padding (and, on a row, minimum height and background), shared with every
    // cell or row that has the same values.
    private CellOptions _cell = CellOptions.Default;

    /// <summary>
    /// The content element to display in this cell
    /// </summary>
    public required IElement Content { get; set; }

    /// <summary>
    /// TextBlock color for this cell
    /// </summary>
    public Color? Color
    {
        get => _cell.Color;
        set => _cell = _cell.With(value, static (o, v) => o with { Color = v });
    }

    /// <summary>
    /// Custom padding for this cell (overrides table default if set)
    /// </summary>
    public Thickness? Padding
    {
        get => _cell.Padding;
        set => _cell = _cell.With(value, static (o, v) => o with { Padding = v });
    }

    /// <summary>
    /// Number of columns this cell spans (default: 1)
    /// </summary>
    public int ColumnSpan { get; set; } = 1;

    /// <summary>
    /// Whether this cell is a header cell (renders as <c>/TH</c> in
    /// PDF/UA tagged output instead of <c>/TD</c>). Set automatically
    /// by <see cref="Table.AddHeader(BonniePDF.IElement[])"/> on each cell in a header row,
    /// and at render time from the owning row's <c>IsHeader</c> flag
    /// for cells added directly to a row. May also be set explicitly
    /// for individual cells that act as headers (e.g. row headers in
    /// the first column of a data row).
    /// </summary>
    public bool IsHeader { get; set; }

    /// <summary><c>TH</c> for a header cell, otherwise <c>TD</c>.</summary>
    public SemanticRole SemanticRole =>
        IsHeader ? SemanticRole.TH : SemanticRole.TD;

    protected override float MeasureContentHeight(float contentWidth)
    {
        Thickness padding = Padding ?? new Thickness(5);
        return Content.MeasureHeight(contentWidth - padding.Left - padding.Right) + padding.Top + padding.Bottom;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        // This is used when TableCell is used as a standalone element (not in a table)
        // For table rendering, the Table class handles the rendering directly
        float totalHeight = MeasureContentHeight(contentWidth);

        if (BackgroundColor.HasValue)
            DrawBackground(ctx, ctx.CurrentX, ctx.CurrentY, contentWidth, totalHeight, BackgroundColor.Value);

        Thickness padding = Padding ?? new Thickness(5);
        float originalX = ctx.CurrentX;
        float originalY = ctx.CurrentY;
        ctx.CurrentX += padding.Left;
        ctx.CurrentY += padding.Top;

        Content.Render(ctx, contentWidth - padding.Left - padding.Right);

        ctx.CurrentX = originalX;
        ctx.CurrentY = originalY + totalHeight;
    }

    /// <summary>
    /// Creates a TableCell from an IElement with optional styling
    /// </summary>
    public static TableCell From(IElement content)
    {
        return new TableCell { Content = content };
    }

    /// <summary>
    /// Sets the background color for this cell
    /// </summary>
    public TableCell WithBackground(Color color)
    {
        BackgroundColor = color;
        return this;
    }

    /// <summary>
    /// Sets the text color for this cell
    /// </summary>
    public TableCell WithColor(Color color)
    {
        Color = color;
        return this;
    }

    /// <summary>
    /// Sets the padding for this cell
    /// </summary>
    public TableCell WithPadding(float padding)
    {
        Padding = new Thickness(padding);
        return this;
    }

    /// <summary>
    /// Sets the padding for this cell with individual values
    /// </summary>
    public TableCell WithPadding(float left, float top, float right, float bottom)
    {
        Padding = new Thickness(left, top, right, bottom);
        return this;
    }

    /// <summary>
    /// Sets the border for this cell (uniform on all sides)
    /// </summary>
    public TableCell WithBorder(Color color, float width = 1)
    {
        Border = new Border(width);
        BorderColors = new BorderColors(color);
        return this;
    }

    /// <summary>
    /// Sets the border with per-side widths and colors
    /// </summary>
    public TableCell WithBorder(Border border, BorderColors borderColors)
    {
        Border = border;
        BorderColors = borderColors;
        return this;
    }

    /// <summary>
    /// Sets the border with per-side widths (single color)
    /// </summary>
    public TableCell WithBorder(Border border, Color color)
    {
        Border = border;
        BorderColors = new BorderColors(color);
        return this;
    }

    /// <summary>
    /// Sets the column span for this cell
    /// </summary>
    public TableCell WithColumnSpan(int span)
    {
        ColumnSpan = span;
        return this;
    }

}
