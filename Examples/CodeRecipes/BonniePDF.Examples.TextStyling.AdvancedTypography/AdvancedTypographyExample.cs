using BonniePDF;

namespace BonniePDF.Examples.TextStyling.AdvancedTypography;

/// <summary>
/// Advanced typography: super/subscript baseline shift and
/// decoration styling (overline + underline/strikethrough colour &amp; thickness).
/// These are visual controls that leave glyph advances - and therefore layout -
/// untouched.
/// </summary>
[System.ComponentModel.DisplayName("Advanced Typography")]
[System.ComponentModel.Category("Text Styling")]
[System.ComponentModel.Description("Super/subscript and decoration styling (overline, coloured/thick decorations).")]
public static class AdvancedTypographyExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new();
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle =
            new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[700]);
        doc.Settings.DisplayDocTitle = true;

        doc.Metadata.Title = "Advanced Typography";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Super/subscript and decoration styling";

        doc.Add(new Heading("Advanced Typography")
        {
            TocIdentifier = "title",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 14),
        });

        // ── Superscript / subscript ──
        doc.Add(new Heading("Superscript & subscript")
        {
            TocIdentifier = "sci",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6),
        });

        TextStyle Base() => new TextStyle().WithSize(13).WithColor(ColorPalette.Slate[800]);
        TextStyle Small() => new TextStyle().WithSize(9).WithColor(ColorPalette.Slate[800]);

        var chemistry = new RichText();
        chemistry.Span("Water is H", Base());
        chemistry.Span("2", Small().Subscript());
        chemistry.Span("O; sulfuric acid is H", Base());
        chemistry.Span("2", Small().Subscript());
        chemistry.Span("SO", Base());
        chemistry.Span("4", Small().Subscript());
        chemistry.Span(".", Base());
        doc.Add(chemistry);

        var physics = new RichText();
        physics.Span("Mass-energy equivalence: E = mc", Base());
        physics.Span("2", Small().Superscript());
        physics.Span(". See the appendix.", Base());
        physics.Span("1", Small().Superscript().WithColor(ColorPalette.Blue[600]));
        doc.Add(physics);

        var ordinals = new RichText { Margin = new Thickness(0, 0, 0, 16) };
        ordinals.Span("Finishers: 1", Base());
        ordinals.Span("st", Small().Superscript());
        ordinals.Span(", 2", Base());
        ordinals.Span("nd", Small().Superscript());
        ordinals.Span(", 3", Base());
        ordinals.Span("rd", Small().Superscript());
        ordinals.Span(".", Base());
        doc.Add(ordinals);

        // ── Decoration styling ──
        doc.Add(new Heading("Decoration styling")
        {
            TocIdentifier = "deco",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6),
        });

        // Block-level overline with a custom colour + thickness.
        doc.Add(new TextBlock
        {
            Text = "Overlined section marker",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = ColorPalette.Slate[800],
            Decoration = TextDecoration.Overline,
            DecorationColor = ColorPalette.Blue[500],
            DecorationThickness = 1.5f,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // Inline decorations with independent colour/thickness per span.
        const float size = 13;
        TextStyle Plain() => new TextStyle().WithSize(size).WithColor(ColorPalette.Slate[800]);

        var decorated = new RichText();
        decorated.Span("A ", Plain());
        decorated.Span("red underline",
            new TextStyle().WithSize(size).WithColor(ColorPalette.Slate[800]).Underline()
                .WithDecorationColor(Color.Red).WithDecorationThickness(1.5f));
        decorated.Span(", a ", Plain());
        decorated.Span("struck-through price",
            new TextStyle().WithSize(size).WithColor(ColorPalette.Slate[400]).Strikethrough()
                .WithDecorationColor(ColorPalette.Slate[400]));
        decorated.Span(", and an ", Plain());
        decorated.Span("overline",
            new TextStyle().WithSize(size).WithColor(ColorPalette.Slate[800]).Overline());
        decorated.Span(".", Plain());
        doc.Add(decorated);

        // A wavy (spell-check) underline on a single span.
        var spell = new RichText { Margin = new Thickness(0, 6, 0, 16) };
        spell.Span("Spell-check style: ", new TextStyle().WithSize(13).WithColor(ColorPalette.Slate[800]));
        spell.Span("mispeled",
            new TextStyle().WithSize(13).WithColor(ColorPalette.Slate[800]).WavyUnderline()
                .WithDecorationColor(Color.Red));
        spell.Span(" word.", new TextStyle().WithSize(13).WithColor(ColorPalette.Slate[800]));
        doc.Add(spell);

        // ── Letter & word spacing ──
        doc.Add(new Heading("Letter & word spacing")
        {
            TocIdentifier = "spacing", Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 14,
            Color = ColorPalette.Slate[700], Margin = new Thickness(0, 0, 0, 6),
        });
        doc.Add(new TextBlock
        {
            Text = "WIDE TRACKING FOR EMPHASIS", FontName = StandardFonts.HelveticaBold, FontSize = 12,
            Color = ColorPalette.Slate[800], LetterSpacing = 3, Margin = new Thickness(0, 0, 0, 4),
        });
        doc.Add(new TextBlock
        {
            Text = "Airy   word   spacing   opens   the   line.", FontSize = 12,
            Color = ColorPalette.Slate[700], WordSpacing = 4, Margin = new Thickness(0, 0, 0, 16),
        });

        // ── Line clamp ──
        doc.Add(new Heading("Line clamp")
        {
            TocIdentifier = "clamp", Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 14,
            Color = ColorPalette.Slate[700], Margin = new Thickness(0, 0, 0, 6),
        });
        doc.Add(new TextBlock
        {
            Text =
                "This description is clamped to two lines: however much copy the product team writes, the card stays a fixed height and the overflow is replaced with an ellipsis so the layout never breaks.",
            FontSize = 12, Color = ColorPalette.Slate[700], MaxLines = 2, Margin = new Thickness(0, 0, 0, 16),
        });

        // ── Paragraph controls ──
        doc.Add(new Heading("Paragraph indent & spacing")
        {
            TocIdentifier = "paragraphs", Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 14,
            Color = ColorPalette.Slate[700], Margin = new Thickness(0, 0, 0, 6),
        });
        doc.Add(new TextBlock
        {
            Text =
                "The first line of each paragraph is indented and a little air is inserted between paragraphs, the way a printed book sets body copy.\n" +
                "This second paragraph shows the inter-paragraph spacing and the matching first-line indent, all resolved during layout without manual spacers.",
            FontSize = 12, Color = ColorPalette.Slate[700], FirstLineIndent = 24, ParagraphSpacing = 8,
        });

        return doc;
    }
}
