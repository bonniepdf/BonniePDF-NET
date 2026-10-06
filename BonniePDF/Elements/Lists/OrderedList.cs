using System.Collections.Generic;
using System.Text;

namespace BonniePDF;

/// <summary>
/// An ordered list element that displays numbered items with various numbering formats
/// </summary>
public class OrderedList : ListElementBase
{
    private readonly List<string> _items = [];
    private readonly List<OrderedList> _nestedLists = [];

    /// <summary>
    /// Number format for the list (Decimal, Roman, Alpha, etc.)
    /// </summary>
    public NumberFormat NumberFormat { get; set; } = NumberFormat.Decimal;

    /// <summary>
    /// Starting number for the list (default: 1)
    /// </summary>
    public int StartNumber { get; set; } = 1;

    /// <summary>
    /// The items in this list (exposed for collection initializer syntax)
    /// </summary>
    public IList<string> Items => _items;

    /// <summary>
    /// Adds a text item to this list
    /// </summary>
    /// <example>
    /// <code>
    /// var list = new OrderedList { NumberFormat = NumberFormat.LowerRoman };
    /// list.AddItem("Preliminaries");
    /// list.AddItem("Main text");
    /// </code>
    /// </example>
    public void AddItem(string item)
    {
        _items.Add(item ?? string.Empty);
    }

    /// <summary>
    /// Adds a nested list to this list
    /// </summary>
    public void AddNestedList(OrderedList nestedList)
    {
        nestedList.Indent = Indent;
        _nestedLists.Add(nestedList);
    }

    public override IReadOnlyList<ListElementBase> GetNestedLists()
    {
        return _nestedLists;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        return MeasureListHeight(_items, _nestedLists, contentWidth);
    }

    protected override void RenderItems(RenderContext ctx, float contentWidth)
    {
        float itemWidth = contentWidth - Indent;

        int currentItemNumber = StartNumber;
        float localY = ctx.CurrentY;

        // Render regular items
        foreach (string item in _items)
        {
            IList<string> lines = ComputeLines(item, itemWidth);
            string numberText = FormatNumber(currentItemNumber);
            float numberWidth = MeasureTextWidth(numberText + ".");

            // Render each line with the number
            RenderListItem(
                ctx,
                lines,
                ref localY,
                (x, y) =>
                {
                    // Draw number
                    ctx.Canvas.DrawText(x, y, numberText + ".", Color, FontSize, FontName, TextAlignment.Left);
                },
                numberWidth
            );

            currentItemNumber++;
        }

        // Update Y position
        ctx.CurrentY = localY;

        RenderNestedLists(ctx, _nestedLists, contentWidth);
    }

    private string FormatNumber(int number)
    {
        return NumberFormat switch
        {
            NumberFormat.Decimal => number.ToString(),
            NumberFormat.UpperRoman => ToRomanNumeral(number),
            NumberFormat.LowerRoman => ToRomanNumeral(number).ToLower(),
            NumberFormat.UpperAlpha => ToAlpha(number),
            NumberFormat.LowerAlpha => ToAlpha(number).ToLower(),
            _ => number.ToString()
        };
    }

    private static string ToRomanNumeral(int number)
    {
        if (number <= 0 || number > 3999)
            return number.ToString();

        string[] thousands = { "", "M", "MM", "MMM" };
        string[] hundreds = { "", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM" };
        string[] tens = { "", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC" };
        string[] ones = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

        return thousands[number / 1000] +
               hundreds[number % 1000 / 100] +
               tens[number % 100 / 10] +
               ones[number % 10];
    }

    private static string ToAlpha(int number)
    {
        if (number <= 0)
            return number.ToString();

        StringBuilder result = new();
        number--;

        do
        {
            result.Insert(0, (char)('A' + number % 26));
            number /= 26;
        } while (number > 0);

        return result.ToString();
    }
}
