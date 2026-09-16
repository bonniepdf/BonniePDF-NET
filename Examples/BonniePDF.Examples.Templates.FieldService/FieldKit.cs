using System.Globalization;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.FieldService;

/// <summary>
/// Shared building blocks for the field and site showcases.
///
/// A job sheet, a site inspection report, a permit to work, and an inventory
/// schedule of condition are different documents with different jobs, but they
/// are the same *kind* of document: raised on site, about a physical asset,
/// structured as a register of items, and finished by someone signing it. They
/// share a visual grammar - a reference band, a fielded metadata grid, item
/// registers with a severity or status column, and signature blocks - so that
/// grammar lives here and each document carries only what makes it itself.
/// </summary>
internal static class FieldKit
{
    // ── Palette ───────────────────────────────────────────────────────────────
    // Slate ink with an indigo accent, per the shared example design tokens.

    public static readonly Color Ink = ColorPalette.Slate[700];
    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    public static readonly Color Muted = ColorPalette.Slate[600];
    public static readonly Color HeadingColor = ColorPalette.Slate[800];
    public static readonly Color Accent = ColorPalette.Indigo[700];
    public static readonly Color Rule = ColorPalette.Slate[200];
    public static readonly Color PanelFill = ColorPalette.Slate[50];

    /// <summary>
    /// Status ramp shared by all of these documents.
    ///
    /// Deliberately not the traffic-light set a designer would reach for first:
    /// the amber is 700 and the emerald 700 because 500/600 fall below 4.5:1 as
    /// small text on white, and every one of these appears as a table cell.
    /// </summary>
    public static readonly Color StatusCritical = ColorPalette.Rose[700];
    public static readonly Color StatusHigh = ColorPalette.Amber[700];
    public static readonly Color StatusMedium = ColorPalette.Blue[700];
    public static readonly Color StatusLow = ColorPalette.Slate[600];
    public static readonly Color StatusDone = ColorPalette.Emerald[700];

    // ── Text ──────────────────────────────────────────────────────────────────

    /// <summary>Small caps-style label above a title - the document's own classification.</summary>
    public static TextBlock Eyebrow(string text, Color? color = null) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 8.5f,
        TextColor = color ?? Accent,
        Margin = new Thickness(0, 0, 0, 4),
    };

    public static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 21,
        TextColor = HeadingColor,
        Margin = new Thickness(0, 0, 0, 6),
    };

    public static Heading H2(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        TextColor = Accent,
        Margin = new Thickness(0, 14, 0, 8),
    };

    public static Heading H3(string text) => new(text)
    {
        Level = 3,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 10.5f,
        TextColor = HeadingColor,
        Margin = new Thickness(0, 8, 0, 5),
    };

    public static TextBlock Body(
        string text,
        Color? color = null,
        float marginTop = 0f,
        float marginBottom = 8f,
        float size = 9.5f)
        => new()
        {
            Text = text,
            FontName = StandardFonts.Helvetica,
            FontSize = size,
            TextColor = color ?? Ink,
            Margin = new Thickness(0, marginTop, 0, marginBottom),
        };

    /// <summary>Table header cell - white on the accent header band.</summary>
    public static TextBlock Th(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 8.5f,
        TextColor = Color.White,
    };

    /// <summary>Table body cell.</summary>
    public static TextBlock Td(string text, Color? color = null, bool bold = false, float size = 8.5f) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        TextColor = color ?? Ink,
    };

    // ── Structure ─────────────────────────────────────────────────────────────

    /// <summary>A full-width accent rule; the visual top of every one of these documents.</summary>
    public static Container AccentBar(Color? color = null, float height = 3f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 14),
    };

    /// <summary>One label/value pair in the metadata grid.</summary>
    public static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            FontName = StandardFonts.HelveticaBold,
            FontSize = 6.5f,
            TextColor = Muted,
            Margin = new Thickness(0, 0, 0, 2),
        });
        stack.Add(new TextBlock
        {
            Text = value,
            FontName = StandardFonts.Helvetica,
            FontSize = 9.5f,
            TextColor = Ink,
        });
        return stack;
    }

    /// <summary>
    /// A row of label/value fields, evenly weighted.
    ///
    /// Field documents are read by someone standing up holding a clipboard or a
    /// phone, so the reference data goes in a scannable grid at the top rather
    /// than in prose.
    /// </summary>
    public static HorizontalStack FieldRow(IEnumerable<IElement> fields, float marginBottom = 14f)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 14,
            Margin = new Thickness(0, 2, 0, marginBottom),
        };
        foreach (IElement f in fields)
        {
            row.Add(f, 1);
        }

        return row;
    }

    /// <summary>A bordered panel with a heading - used for notes, scope, and declarations.</summary>
    public static Container Panel(string title, IEnumerable<IElement> children, Color? accent = null)
    {
        VerticalStack stack = new() { Spacing = 4 };
        stack.Add(new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9.5f,
            TextColor = accent ?? Accent,
            Margin = new Thickness(0, 0, 0, 4),
        });
        foreach (IElement child in children)
        {
            stack.Add(child);
        }

        return new Container
        {
            BackgroundColor = PanelFill,
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = stack,
        };
    }

    /// <summary>A coloured status/severity pill for a table cell.</summary>
    public static Container StatusCell(string text, Color color) => new()
    {
        Child = new TextBlock
        {
            Text = text,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            TextColor = color,
        },
    };

    // ── Signature blocks ──────────────────────────────────────────────────────

    /// <summary>
    /// A single ruled signature slot: name, role, a rule to sign on, and a date.
    ///
    /// This is drawn, not cryptographic. A drawn signature block is what these
    /// documents carry in the field - the customer signs the engineer's tablet or
    /// the printed sheet. Digital signing is a separate, separately-licensed
    /// concern and nothing here should be read as claiming it.
    /// </summary>
    public static VerticalStack SignatureSlot(string role, string name, string? dated = null)
    {
        VerticalStack stack = new() { Spacing = 0 };
        stack.Add(new TextBlock
        {
            Text = role.ToUpperInvariant(),
            FontName = StandardFonts.HelveticaBold,
            FontSize = 6.5f,
            TextColor = Muted,
            Margin = new Thickness(0, 0, 0, 18),
        });

        // The rule people actually sign on.
        stack.Add(new Container
        {
            BackgroundColor = ColorPalette.Slate[400],
            Height = 0.75f,
            Margin = new Thickness(0, 0, 0, 5),
        });

        stack.Add(new TextBlock
        {
            Text = name,
            FontName = StandardFonts.Helvetica,
            FontSize = 9,
            TextColor = Ink,
        });
        stack.Add(new TextBlock
        {
            Text = dated is not null ? $"Date: {dated}" : "Date:",
            FontName = StandardFonts.Helvetica,
            FontSize = 8,
            TextColor = Muted,
            Margin = new Thickness(0, 3, 0, 0),
        });
        return stack;
    }

    /// <summary>A row of signature slots, evenly weighted.</summary>
    public static HorizontalStack SignatureRow(IEnumerable<IElement> slots)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 22,
            Margin = new Thickness(0, 10, 0, 4),
        };
        foreach (IElement s in slots)
        {
            row.Add(s, 1);
        }

        return row;
    }

    // ── Formatting ────────────────────────────────────────────────────────────

    private static readonly CultureInfo GbCulture = CultureInfo.GetCultureInfo("en-GB");

    public static string ShortDate(DateTime d) => d.ToString("dd MMM yyyy", GbCulture);

    public static string ClockTime(DateTime d) => d.ToString("HH:mm", GbCulture);

    /// <summary>Sterling, to two places.</summary>
    public static string Gbp(decimal value) => value.ToString("C2", GbCulture);

    /// <summary>Decimal hours, as engineers record them: 1.5, 0.75.</summary>
    public static string Hours(decimal value) => $"{value.ToString("F2", CultureInfo.InvariantCulture)} h";
}
