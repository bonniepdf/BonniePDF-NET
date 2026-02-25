using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Tables;

namespace BonniePDF.Extensions;

public static class TableCellExtensions
{    
    /// <summary>
    /// Converts an IElement to a TableCell
    /// </summary>
    public static TableCell AsCell(this IElement e)
    {
        return new TableCell { Content = e };
    }

    /// <summary>
    /// Sets the background color for a TableCell
    /// </summary>
    public static TableCell Background(this TableCell c, Color color)
    {
        c.BackgroundColor = color;
        return c;
    }

    /// <summary>
    /// Sets the text color for a TableCell
    /// </summary>
    public static TableCell TextColor(this TableCell c, Color color)
    {
        c.TextColor = color;
        return c;
    }

    /// <summary>
    /// Sets the padding for a TableCell
    /// </summary>
    public static TableCell Padding(this TableCell c, float padding)
    {
        c.Padding = new Thickness(padding);
        return c;
    }

    /// <summary>
    /// Sets the padding for a TableCell with individual values
    /// </summary>
    public static TableCell Padding(this TableCell c, float left, float top, float right, float bottom)
    {
        c.Padding = new Thickness(left, top, right, bottom);
        return c;
    }

    /// <summary>
    /// Sets the border for a TableCell
    /// </summary>
    public static TableCell Border(this TableCell c, Color color, float width = 1)
    {
        c.Border = new Border(width);
        c.BorderColors = new BorderColors(color);
        return c;
    }

    /// <summary>
    /// Sets the column span for a TableCell
    /// </summary>
    public static TableCell ColSpan(this TableCell c, int span)
    {
        c.ColumnSpan = span;
        return c;
    }
}