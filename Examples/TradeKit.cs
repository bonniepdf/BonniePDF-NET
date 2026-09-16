using System.Globalization;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Shared;

/// <summary>
/// Shared building blocks for the documents built on an invoice's grammar - the
/// account statement, purchase order, credit note, delivery note, remittance
/// advice and dunning letter, and beyond them the bill of costs, exhibit bundle
/// index, property particulars, discharge summary and timesheet.
///
/// These are the documents either side of an invoice in a real trading
/// relationship, and they share its grammar: a reference band, a parties block, a
/// line table, and a totals or acknowledgement block. Separate from
/// <c>InvoiceKit</c> in the regional-invoice project, which carries the grammar
/// of the invoice itself.
///
/// Linked into each consuming example project rather than copied, so the
/// documents cannot drift apart. It is deliberately not linked by
/// <c>Examples/Directory.Build.props</c>: that would compile it into all sixty
/// example projects, most of which have no use for it.
/// </summary>
internal static class TradeKit
{
    public static readonly Color Ink = ColorPalette.Slate[700];
    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    public static readonly Color Muted = ColorPalette.Slate[600];
    public static readonly Color Heading = ColorPalette.Slate[800];
    public static readonly Color Accent = ColorPalette.Indigo[700];
    public static readonly Color Rule = ColorPalette.Slate[200];
    public static readonly Color Positive = ColorPalette.Emerald[700];
    public static readonly Color Negative = ColorPalette.Rose[700];

    /// <summary>A styled text cell with trade-document defaults.</summary>
    public static TextBlock Cell(
        string text,
        float size = 9f,
        bool bold = false,
        Color? color = null,
        TextAlignment align = TextAlignment.Left,
        float marginTop = 0f,
        float marginBottom = 0f)
        => new()
        {
            Text = text,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            TextColor = color ?? Ink,
            Alignment = align,
            Margin = new Thickness(0, marginTop, 0, marginBottom),
        };

    /// <summary>A block of plain lines (an address), tagged as one paragraph.</summary>
    public static TextBlock Lines(
        IEnumerable<string> items,
        float size = 9f,
        Color? color = null,
        TextAlignment align = TextAlignment.Left)
        => Cell(string.Join('\n', items), size: size, color: color, align: align);

    /// <summary>Table header cell - white on the accent band.</summary>
    public static TextBlock Th(string text, TextAlignment align = TextAlignment.Left)
        => Cell(text, size: 8.5f, bold: true, color: Color.White, align: align);

    /// <summary>Table body cell.</summary>
    public static TextBlock Td(
        string text,
        float size = 8.5f,
        bool bold = false,
        Color? color = null,
        TextAlignment align = TextAlignment.Left)
        => Cell(text, size: size, bold: bold, color: color, align: align);

    public static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 21,
        TextColor = Heading,
        Margin = new Thickness(0, 0, 0, 4),
    };

    public static Heading H2(string text, Color? color = null) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        TextColor = color ?? Accent,
        Margin = new Thickness(0, 16, 0, 8),
    };

    public static Heading H3(string text) => new(text)
    {
        Level = 3,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 10.5f,
        TextColor = Heading,
        Margin = new Thickness(0, 10, 0, 5),
    };

    /// <summary>The full-width accent rule at the top of each document.</summary>
    public static Container AccentBar(Color? color = null, float height = 4f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 14),
    };

    /// <summary>A labelled address block.</summary>
    public static VerticalStack PartyBlock(string label, IEnumerable<string> address)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 7f, bold: true, color: Muted, marginBottom: 3f));
        stack.Add(Lines(address, size: 9f));
        return stack;
    }

    /// <summary>One label/value pair for the reference grid.</summary>
    public static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 2f));
        stack.Add(Cell(value, size: 9.5f));
        return stack;
    }

    /// <summary>A row of evenly weighted elements.</summary>
    public static HorizontalStack WeightedRow(
        IEnumerable<IElement> items,
        float spacing = 14f,
        float marginTop = 0f,
        float marginBottom = 14f)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = spacing,
            Margin = new Thickness(0, marginTop, 0, marginBottom),
        };
        foreach (IElement item in items)
        {
            row.Add(item, 1);
        }

        return row;
    }

    /// <summary>A "label ....... value" totals line, weighted so the value hugs the right.</summary>
    public static HorizontalStack TotalLine(
        string label,
        string value,
        bool bold = false,
        float size = 9f,
        Color? color = null)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 3, 0, 3),
        };
        row.Add(Cell(label, size: size, bold: bold, color: color ?? Muted), 3);
        row.Add(Cell(value, size: size, bold: bold, color: color ?? Ink, align: TextAlignment.Right), 2);
        return row;
    }

    /// <summary>A right-hand totals panel, with the body already assembled.</summary>
    public static HorizontalStack TotalsPanel(IEnumerable<IElement> children, float weight = 1.6f)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 10, 0, 4),
        };
        row.Add(new Container(), 2);

        VerticalStack body = new();
        foreach (IElement child in children)
        {
            body.Add(child);
        }

        row.Add(
            new Container
            {
                BackgroundColor = ColorPalette.Indigo[50],
                CornerRadius = 6,
                Padding = new Thickness(12),
                Child = body,
            },
            weight);
        return row;
    }

    /// <summary>A bordered panel with a heading - terms, notes, declarations.</summary>
    public static Container Panel(string title, IEnumerable<IElement> children, Color? accent = null)
    {
        VerticalStack body = new() { Spacing = 4 };
        body.Add(Cell(title, size: 9.5f, bold: true, color: accent ?? Accent, marginBottom: 4f));
        foreach (IElement child in children)
        {
            body.Add(child);
        }

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 12),
            Child = body,
        };
    }

    /// <summary>A summary tile.</summary>
    public static Container Tile(string value, string label, Color? accent = null)
    {
        VerticalStack body = new();
        body.Add(Cell(value, size: 14f, bold: true, color: accent ?? Accent, marginBottom: 2f));
        body.Add(Cell(label, size: 8f, color: Muted));

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(11, 10),
            Child = body,
        };
    }

    /// <summary>A thin divider, used above a grand total.</summary>
    public static Container Divider() => new()
    {
        BackgroundColor = Rule,
        Height = 0.75f,
        Margin = new Thickness(0, 5, 0, 5),
    };

    /// <summary>The running header / footer band.</summary>
    public static HorizontalStack Band(string left, string right)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(Cell(left, size: 7.5f, color: Muted), 2.5f);
        row.Add(Cell(right, size: 7.5f, color: Muted, align: TextAlignment.Right), 2);
        return row;
    }

    /// <summary>
    /// Sterling, with the sign outside the symbol.
    ///
    /// On a statement or a credit note, where the sign is the most important
    /// character in the cell, it belongs in front of the symbol rather than after
    /// it - "-£420.00", not "£-420.00".
    /// </summary>
    public static string Money(decimal value, string symbol = "£", string culture = "en-GB")
        => (value < 0 ? "-" : string.Empty)
            + symbol
            + Math.Abs(value).ToString("N2", CultureInfo.GetCultureInfo(culture));

    public static string GbDate(DateTime date)
        => date.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
}
