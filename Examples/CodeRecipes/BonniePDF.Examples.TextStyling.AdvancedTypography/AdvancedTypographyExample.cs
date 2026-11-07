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
[System.ComponentModel.Description("A type-specimen sheet: super/subscript, decoration styling, letter and word spacing, line clamping and paragraph indents, one ruled row per feature.")]
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

        // Each feature is one row of the specimen sheet: what it is on the left, examples on the right.

        // ── Superscript / subscript ──
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

        var physics = new RichText();
        physics.Span("Mass-energy equivalence: E = mc", Base());
        physics.Span("2", Small().Superscript());
        physics.Span(". See the appendix.", Base());
        physics.Span("1", Small().Superscript().WithColor(ColorPalette.Indigo[600]));

        var ordinals = new RichText();
        ordinals.Span("Finishers: 1", Base());
        ordinals.Span("st", Small().Superscript());
        ordinals.Span(", 2", Base());
        ordinals.Span("nd", Small().Superscript());
        ordinals.Span(", 3", Base());
        ordinals.Span("rd", Small().Superscript());
        ordinals.Span(".", Base());
        doc.Add(Specimen("Superscript & subscript", "Formulae, powers, footnote marks and ordinals.", chemistry, physics, ordinals));

        // ── Decoration styling ──
        // Block-level overline with a custom colour + thickness.
        var overlined = new TextBlock
        {
            Text = "Overlined section marker",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = ColorPalette.Slate[800],
            Decoration = TextDecoration.Overline,
            DecorationColor = ColorPalette.Indigo[500],
            DecorationThickness = 1.5f,
        };

        // Inline decorations with independent colour/thickness per span.
        const float size = 13;
        TextStyle Plain() => new TextStyle().WithSize(size).WithColor(ColorPalette.Slate[800]);

        var decorated = new RichText();
        decorated.Span("A ", Plain());
        decorated.Span("rose underline",
            new TextStyle().WithSize(size).WithColor(ColorPalette.Slate[800]).Underline()
                .WithDecorationColor(ColorPalette.Rose[600]).WithDecorationThickness(1.5f));
        decorated.Span(", a ", Plain());
        decorated.Span("struck-through price",
            new TextStyle().WithSize(size).WithColor(ColorPalette.Slate[400]).Strikethrough()
                .WithDecorationColor(ColorPalette.Slate[400]));
        decorated.Span(", and an ", Plain());
        decorated.Span("overline",
            new TextStyle().WithSize(size).WithColor(ColorPalette.Slate[800]).Overline());
        decorated.Span(".", Plain());

        // A wavy (spell-check) underline on a single span.
        var spell = new RichText();
        spell.Span("Spell-check style: ", new TextStyle().WithSize(13).WithColor(ColorPalette.Slate[800]));
        spell.Span("mispeled",
            new TextStyle().WithSize(13).WithColor(ColorPalette.Slate[800]).WavyUnderline()
                .WithDecorationColor(ColorPalette.Rose[600]));
        spell.Span(" word.", new TextStyle().WithSize(13).WithColor(ColorPalette.Slate[800]));
        doc.Add(Specimen("Decoration styling", "Overline, underline and strikethrough, each with its own colour and weight.",
            overlined, decorated, spell));

        // ── Letter & word spacing ──
        doc.Add(Specimen("Letter & word spacing", "Tracking for labels and headings; wider word spacing for an open line.",
            new TextBlock
            {
                Text = "WIDE TRACKING FOR EMPHASIS", FontName = StandardFonts.HelveticaBold, FontSize = 12,
                Color = ColorPalette.Slate[800], LetterSpacing = 3,
            },
            new TextBlock
            {
                Text = "Airy   word   spacing   opens   the   line.", FontSize = 12,
                Color = ColorPalette.Slate[700], WordSpacing = 4,
            }));

        // ── Line clamp ──
        doc.Add(Specimen("Line clamp", "Fixed-height cards: the overflow ends in an ellipsis.",
            new TextBlock
            {
                Text =
                    "This description is clamped to two lines: however much copy the product team writes, the card stays a fixed height and the overflow is replaced with an ellipsis so the layout never breaks.",
                FontSize = 12, Color = ColorPalette.Slate[700], MaxLines = 2,
            }));

        // ── Paragraph controls ──
        doc.Add(Specimen("Paragraph indent & spacing", "Book-style first-line indents and space between paragraphs.",
            new TextBlock
            {
                Text =
                    "The first line of each paragraph is indented and a little air is inserted between paragraphs, the way a printed book sets body copy.\n" +
                    "This second paragraph shows the inter-paragraph spacing and the matching first-line indent, all resolved during layout without manual spacers.",
                FontSize = 12, Color = ColorPalette.Slate[700], FirstLineIndent = 24, ParagraphSpacing = 8,
            }));

        return doc;
    }

    /// <summary>One row of the specimen sheet: the feature's name and a note on the left, its examples on the right.</summary>
    private static Container Specimen(string label, string note, params IElement[] examples)
    {
        VerticalStack left = new() { Spacing = 4 };
        left.Add(new Heading(label) { Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 11, Color = ColorPalette.Indigo[700] });
        left.Add(new TextBlock(note) { FontSize = 9, Color = ColorPalette.Slate[500] });

        VerticalStack right = new() { Spacing = 6 };
        foreach (IElement example in examples) right.Add(example);

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 20 };
        row.Add(left, 1);
        row.Add(right, 2.6f);
        return new Container
        {
            Border = Border.TopOnly(0.75f),
            BorderColors = BorderColors.TopOnly(ColorPalette.Slate[200]),
            Padding = new Thickness(0, 14, 0, 14),
            Child = row,
        };
    }
}
