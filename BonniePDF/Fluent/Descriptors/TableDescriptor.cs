using System;
using System.Collections.Generic;

namespace BonniePDF.Fluent;

/// <summary>
/// Describes a table layout. Provides scoped methods for defining columns, headers, footers, and cells.
/// Maps to a <see cref="Table"/> element internally.
/// </summary>
public sealed class TableDescriptor
{
    private float[] _columnWeights = [1f];
    private readonly List<Action<TableRowDescriptor>> _headerActions = [];
    private readonly List<Action<TableRowDescriptor>> _footerActions = [];
    private readonly List<object> _bodyItems = [];

    // Table styling
    private Color? _headerBackgroundColor;
    private Color? _headerTextColor;
    private float _cellPadding = 5;
    private bool _repeatHeaders = true;
    private Border _border = new(0);
    private BorderColors _borderColors;

    /// <summary>
    /// Defines the column weights for the table.
    /// </summary>
    /// <param name="weights">Column weights (proportional). E.g., (2f, 1f, 1f) creates a 2:1:1 ratio.</param>
    public void Columns(params float[] weights)
    {
        _columnWeights = weights;
    }

    /// <summary>
    /// Sets the default cell padding in points.
    /// </summary>
    public void CellPadding(float padding) => _cellPadding = padding;

    /// <summary>
    /// Sets whether headers repeat on each page. Default is true.
    /// </summary>
    public void RepeatHeaders(bool repeat = true) => _repeatHeaders = repeat;

    /// <summary>
    /// Sets the header background color.
    /// </summary>
    public void HeaderBackground(Color color) => _headerBackgroundColor = color;

    /// <summary>
    /// Sets the header text color.
    /// </summary>
    public void HeaderTextColor(Color color) => _headerTextColor = color;

    /// <summary>
    /// Sets a uniform border for all cells.
    /// </summary>
    public void Border(float width, Color? color = null)
    {
        _border = new Border(width);
        _borderColors = new BorderColors(color ?? Color.Black);
    }

    /// <summary>
    /// Adds a header row to the table.
    /// </summary>
    public void Header(Action<TableRowDescriptor> handler)
    {
        _headerActions.Add(handler);
    }

    /// <summary>
    /// Adds a footer row to the table.
    /// </summary>
    public void Footer(Action<TableRowDescriptor> handler)
    {
        _footerActions.Add(handler);
    }

    /// <summary>
    /// Adds a full row to the table with a specific background color.
    /// </summary>
    public void Row(Color background, Action<TableRowDescriptor> handler)
    {
        _bodyItems.Add(new RowDefinition(background, handler));
    }

    /// <summary>
    /// Adds a full row to the table.
    /// </summary>
    public void Row(Action<TableRowDescriptor> handler)
    {
        _bodyItems.Add(handler);
    }

    /// <summary>
    /// Returns a container for the next cell.
    /// Cells fill rows automatically based on the column count.
    /// </summary>
    public IContentContainer Cell()
    {
        var slot = new ContentSlot();
        _bodyItems.Add(slot);
        return slot;
    }

    private record RowDefinition(Color Background, Action<TableRowDescriptor> Handler);

    /// <summary>
    /// Builds the Table element from the descriptor configuration.
    /// </summary>
    internal Table Build()
    {
        var table = new Table(_columnWeights)
        {
            CellPadding = _cellPadding,
            RepeatHeadersOnNewPage = _repeatHeaders,
            HeaderBackgroundColor = _headerBackgroundColor,
            HeaderTextColor = _headerTextColor,
            Border = _border,
            BorderColors = _borderColors
        };

        // Build header rows
        foreach (var headerAction in _headerActions)
        {
            var rowDesc = new TableRowDescriptor();
            headerAction(rowDesc);
            table.AddHeader(rowDesc.Build());
        }

        // Build footer rows
        foreach (var footerAction in _footerActions)
        {
            var rowDesc = new TableRowDescriptor();
            footerAction(rowDesc);
            table.AddFooter(rowDesc.Build());
        }

        // Build data rows from cell slots and explicit rows
        int colCount = _columnWeights.Length;
        List<ContentSlot> currentSlots = new();

        void FlushCells()
        {
            if (currentSlots.Count == 0) return;

            for (int i = 0; i < currentSlots.Count; i += colCount)
            {
                int cellsInRow = Math.Min(colCount, currentSlots.Count - i);
                var cells = new IElement[cellsInRow];

                for (int j = 0; j < cellsInRow; j++)
                {
                    IElement? child = currentSlots[i + j].Child;
                    cells[j] = child ?? new TextBlock { Text = "" };
                }

                table.AddRow(cells);
            }
            currentSlots.Clear();
        }

        foreach (var item in _bodyItems)
        {
            if (item is ContentSlot slot)
            {
                currentSlots.Add(slot);
            }
            else if (item is RowDefinition rowDef)
            {
                FlushCells();
                var rowDesc = new TableRowDescriptor();
                rowDef.Handler(rowDesc);
                var row = rowDesc.Build();
                row.WithBackground(rowDef.Background);
                table.AddRow(row);
            }
            else if (item is Action<TableRowDescriptor> rowAction)
            {
                FlushCells();
                var rowDesc = new TableRowDescriptor();
                rowAction(rowDesc);
                table.AddRow(rowDesc.Build());
            }
        }

        FlushCells();

        return table;
    }
}
