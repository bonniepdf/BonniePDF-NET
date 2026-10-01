using System.Globalization;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.RegionalInvoices;

/// <summary>
/// Shared building blocks for the regional invoice showcases (US / UK / EU / Asia).
///
/// Each regional invoice differs in its tax and compliance story - US sales tax,
/// UK VAT, EU reverse-charge / EN 16931, India GST with an IRN + QR - but they
/// share the same visual grammar: a header band, a bill-to / details block, a
/// line-items table, and a totals stack. This kit captures that grammar so the
/// four files stay tight and only carry what actually makes them regional.
/// </summary>
internal static class InvoiceKit
{
    public static readonly Color Ink = ColorPalette.Slate[700];
    // 600, not 500: reads against tinted panels in the invoices that use it, and
    // 500 falls to 4.35-4.38:1 there against the 4.5:1 WCAG AA threshold.
    public static readonly Color Muted = ColorPalette.Slate[600];
    public static readonly Color HeaderBg = ColorPalette.Slate[100];
    public static readonly Color Rule = ColorPalette.Slate[200];

    /// <summary>A styled text cell with invoice-friendly defaults (9pt Helvetica, slate ink).</summary>
    public static TextBlock Cell(
        string text,
        float size = 9f,
        bool bold = false,
        Color? color = null,
        TextAlignment align = TextAlignment.Left,
        Thickness? margin = null)
    {
        TextBlock b = new()
        {
            Text = text,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color ?? Ink,
            Alignment = align,
        };
        if (margin.HasValue)
        {
            b.Margin = margin.Value;
        }

        return b;
    }

    /// <summary>A monetary string using the region's symbol and culture.</summary>
    public static string Money(decimal value, string symbol, string culture = "en-US")
        => symbol + value.ToString("N2", CultureInfo.GetCultureInfo(culture));

    /// <summary>A block of plain text lines, e.g. a postal address. A single TextBlock
    /// using hard <c>\n</c> breaks, so the block tags as one paragraph under PDF/UA.</summary>
    public static TextBlock Lines(Color color, params string[] items)
        => Cell(string.Join('\n', items), color: color);

    /// <summary>Appends plain text lines to an existing stack (e.g. after a bold company name).</summary>
    public static void AddLines(VerticalStack stack, Color color, params string[] items)
        => stack.Add(Lines(color, items));

    /// <summary>A labelled block: a bold eyebrow label over a body element.</summary>
    public static Container Labelled(string label, IElement body, Color accent)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label, size: 8f, bold: true, color: accent, margin: new Thickness(0, 0, 0, 5)));
        stack.Add(body);
        return new Container { Child = stack };
    }

    /// <summary>A single "label ....... value" totals line, weighted so the value hugs the right edge.</summary>
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

    /// <summary>A thin horizontal divider used between totals and the grand total.</summary>
    public static Container Divider(Color color, float thickness = 0.6f) => new()
    {
        BackgroundColor = color,
        Height = thickness,
        Margin = new Thickness(0, 5, 0, 5),
    };

    /// <summary>A full-width coloured accent bar - the top rule that anchors each invoice.</summary>
    public static Container AccentBar(Color color, float height = 5f) => new()
    {
        BackgroundColor = color,
        Height = height,
    };
}

/// <summary>A single invoice line item shared by the regional invoice showcases.</summary>
internal sealed record InvoiceItem(string Description, int Qty, decimal UnitPrice, string? Detail = null);
