using System.Collections.Generic;

namespace BonniePDF.Fluent;

/// <summary>
/// Describes a table row (used for headers and footers).
/// Provides a <see cref="Cell"/> method that returns a container for each cell's content.
/// </summary>
public sealed class TableRowDescriptor
{
    private readonly List<ContentSlot> _cellSlots = [];

    /// <summary>
    /// Returns a container for the next cell in this row.
    /// </summary>
    public IContentContainer Cell()
    {
        var slot = new ContentSlot();
        _cellSlots.Add(slot);
        return slot;
    }

    /// <summary>
    /// Builds a TableRow from the configured cells.
    /// </summary>
    internal TableRow Build()
    {
        var cells = new IElement[_cellSlots.Count];
        for (int i = 0; i < _cellSlots.Count; i++)
        {
            cells[i] = _cellSlots[i].Child ?? new TextBlock { Text = "" };
        }

        return TableRow.From(cells);
    }
}
