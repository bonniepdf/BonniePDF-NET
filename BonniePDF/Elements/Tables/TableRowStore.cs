using System;
using System.Collections;
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// A table's body rows. A row added as plain content (<see cref="Table.AddRow(IElement[])"/>
/// with no <see cref="TableCell"/> among the elements) is kept as that content alone: the
/// element for a one-cell row, a copy of the elements otherwise. Its row and cell objects
/// would hold nothing but the defaults, and the table made them, so no caller holds them.
/// </summary>
/// <remarks>
/// Every public read of a row (the indexer, enumeration, <see cref="CopyTo"/>) replaces a
/// compact entry with the real <see cref="TableRow"/> and keeps it, so a caller always sees
/// the same object and its changes stay. Layout reads rows through <see cref="ForLayout"/>,
/// which builds a temporary row for a compact entry instead: the same values, discarded after
/// use, so a long table does not hold three objects per row for the whole of its layout.
/// </remarks>
internal sealed class TableRowStore : IList<TableRow>
{
    private readonly List<object?> _entries = new();

    public int Count => _entries.Count;

    public bool IsReadOnly => false;

    /// <summary>Adds a row of default cells holding <paramref name="contents"/>, kept compact.</summary>
    // A single null content stays in an array, so it is not mistaken for a null row.
    internal void AddContents(IElement[] contents) =>
        _entries.Add(contents.Length == 1 && contents[0] is not null ? contents[0] : (IElement[])contents.Clone());

    /// <summary>Whether row <paramref name="index"/> is kept as its content alone.</summary>
    internal bool IsCompact(int index) => _entries[index] is not (TableRow or null);

    /// <summary>The number of cells in row <paramref name="index"/>, without materialising it.</summary>
    internal int CellCount(int index) => _entries[index] switch
    {
        TableRow row => row.Cells.Length,
        IElement[] contents => contents.Length,
        _ => 1,
    };

    /// <summary>
    /// Row <paramref name="index"/> for measuring and drawing: the stored row, or for a compact
    /// entry a temporary row equal to the one <see cref="TableRow.From(IElement[])"/> builds.
    /// </summary>
    internal TableRow ForLayout(int index) => _entries[index] is TableRow or null ? (TableRow)_entries[index]! : Build(_entries[index]!);

    /// <summary>The cell contents of row <paramref name="index"/>, in order, without materialising it.</summary>
    internal void AddContentsTo(int index, List<IElement> children)
    {
        switch (_entries[index])
        {
            case TableRow row:
                foreach (TableCell cell in row.Cells)
                    if (cell.Content is not null) children.Add(cell.Content);
                break;
            case IElement[] contents:
                foreach (IElement? content in contents)
                    if (content is not null) children.Add(content);
                break;
            case IElement content:
                children.Add(content);
                break;
        }
    }

    private static TableRow Build(object entry) => entry is IElement[] contents
        ? TableRow.From(contents)
        : TableRow.From((IElement)entry);

    // Null entries are rows added as null, which List<TableRow> accepted; they stay null.
    private TableRow Materialise(int index)
    {
        if (_entries[index] is TableRow or null) return (TableRow)_entries[index]!;
        var row = Build(_entries[index]!);
        _entries[index] = row;
        return row;
    }

    public TableRow this[int index]
    {
        get => Materialise(index);
        set => _entries[index] = value;
    }

    public void Add(TableRow item) => _entries.Add(item);

    public void Insert(int index, TableRow item) => _entries.Insert(index, item);

    public void RemoveAt(int index) => _entries.RemoveAt(index);

    public void Clear() => _entries.Clear();

    // A compact entry's row has never been handed out, so it cannot equal a caller's row.
    public int IndexOf(TableRow item)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (ReferenceEquals(_entries[i], item)) return i;
        return -1;
    }

    public bool Contains(TableRow item) => IndexOf(item) >= 0;

    public bool Remove(TableRow item)
    {
        int index = IndexOf(item);
        if (index < 0) return false;
        _entries.RemoveAt(index);
        return true;
    }

    public void CopyTo(TableRow[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        for (int i = 0; i < _entries.Count; i++) array[arrayIndex + i] = Materialise(i);
    }

    public IEnumerator<TableRow> GetEnumerator()
    {
        for (int i = 0; i < _entries.Count; i++) yield return Materialise(i);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
