using System;
using System.Collections.Generic;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;

namespace BonniePDF.Fluent.Descriptors;

/// <summary>
/// Describes a table layout. Provides scoped methods for defining columns, headers, footers, and cells.
/// Maps to a <see cref="Table"/> element internally.
/// </summary>
public sealed class TableDescriptor
{
    private float[] _columnWeights = [1f];
    private readonly List<Action<TableRowDescriptor>> _headerActions = [];
    private readonly List<Action<TableRowDescriptor>> _footerActions = [];
    private readonly List<ContentSlot> _cellSlots = [];

    // Table styling
    private Color? _headerBackgroundColor;
    private Color? _headerTextColor;
    private float _cellPadding = 5;
    private bool _repeatHeaders = true;
    private Core.Border _border = new(0);
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
        _border = new Core.Border(width);
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
    /// Returns a container for the next cell.
    /// Cells fill rows automatically based on the column count.
    /// </summary>
    public IContentContainer Cell()
    {
        var slot = new ContentSlot();
        _cellSlots.Add(slot);
        return slot;
    }

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

        // Build data rows from cell slots
        int colCount = _columnWeights.Length;
        for (int i = 0; i < _cellSlots.Count; i += colCount)
        {
            int cellsInRow = Math.Min(colCount, _cellSlots.Count - i);
            var cells = new IElement[cellsInRow];

            for (int j = 0; j < cellsInRow; j++)
            {
                IElement? child = _cellSlots[i + j].Child;
                cells[j] = child ?? new TextBlock { Text = "" };
            }

            table.AddRow(cells);
        }

        return table;
    }
}
