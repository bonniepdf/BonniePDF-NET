using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>A generation-check report: the diagnostics a draft build raised, how they were fixed, and why diagnostics are not validation.</summary>
public static class DocumentDiagnosticsExample
{
    /// <summary>
    /// The warnings the draft build raises, exactly as <c>Document.Diagnostics</c> reports them.
    /// <see cref="VerifyDiagnosticsAsync"/> rebuilds the draft and fails if the library ever reports anything else.
    /// </summary>
    public static readonly (string Severity, string Category, string Source, string Message)[] DraftDiagnostics =
    [
        ("warning", "font", "Brand Sans",
            "Font family 'Brand Sans' is not a document font, a registered family or a standard font; its text was drawn in Helvetica."),
        ("warning", "font", "Helvetica",
            "Dropped 1 code point(s) with no glyph in the selected or fallback fonts (U+2713)."),
    ];

    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings
        {
            Language = "en-GB",
            Margin = 48,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(ColorPalette.Slate[700])
        })
        { Metadata = new DocumentMetadata { Title = "Check the generated document" } };

        doc.Add(new TextBlock("GENERATION CHECK  ·  ORDER CONFIRMATION ORD-58213")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            LetterSpacing = 1.2f,
            Color = ColorPalette.Indigo[700],
            Margin = new Thickness(0, 0, 0, 6)
        });
        doc.Add(new Heading("Check the generated document")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 6)
        });
        doc.Add(new TextBlock("The draft build finished and produced a PDF, but document.diagnostics reported two warnings: some of its content was not drawn as written. The draft was held back and a corrected document was built from scratch.")
        {
            LineHeight = 1.45f,
            Margin = new Thickness(0, 0, 0, 16)
        });

        HorizontalStack status = new() { Spacing = 12, WidthDistribution = WidthDistribution.Equal, Margin = new Thickness(0, 0, 0, 20) };
        status.Add(StatusCard("Draft build", "2 warnings", "Held back: content was dropped or substituted.", amber: true));
        status.Add(StatusCard("Corrected build", "0 warnings", "Released to the customer.", amber: false));
        doc.Add(status);

        doc.Add(SectionHeading("Diagnostics from the draft build"));
        Table table = new(1.1f, 0.9f, 1.3f, 4.2f)
        {
            CellPadding = 7,
            HeaderBackgroundColor = ColorPalette.Slate[100],
            HeaderTextColor = ColorPalette.Slate[900]
        };
        table.AddHeader(Cell("Severity", bold: true), Cell("Category", bold: true), Cell("Source", bold: true), Cell("Message", bold: true));
        foreach (var (severity, category, source, message) in DraftDiagnostics)
            table.AddRow(Pill(severity), Cell(category), Cell(source), Cell(message));
        doc.Add(table);

        doc.Add(SectionHeading("What changed in the corrected build", 20));
        OrderedList changes = new() { ItemSpacing = 4 };
        changes.AddItem("The heading asked for the Brand Sans family, which the document never registered. The corrected build adds the font files to doc.fonts.");
        changes.AddItem("The status line ended in a check mark (U+2713) that no available font draws. The corrected build writes \"Dispatched\" instead; registering a symbol font would also work.");
        changes.AddItem("With missingGlyphs set to throw, the draft would have failed at generation instead of producing a PDF with a gap.");
        doc.Add(changes);

        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Slate[100],
            CornerRadius = 6,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 20, 0, 0),
            Child = new TextBlock("Diagnostics report what the library noticed while building. They are not a PDF/A or PDF/UA certificate: validate the final file with an external validator, such as veraPDF, before you rely on a conformance claim.")
            {
                FontSize = 9.5f,
                LineHeight = 1.4f
            }
        });
        return doc;
    }

    /// <summary>The draft order confirmation: a heading in an unregistered family and a character no font covers.</summary>
    public static Document BuildDraft()
    {
        var draft = new Document(new DocumentSettings { Language = "en-GB" }) { Metadata = new DocumentMetadata { Title = "Order confirmation ORD-58213" } };
        draft.Add(new Heading("Order confirmation") { Level = 1, FontFamily = "Brand Sans" });
        draft.Add(new TextBlock("Order ORD-58213: Dispatched ✓"));
        return draft;
    }

    /// <summary>Exercise the real warnings and a clean retry, without returning the incomplete PDF.</summary>
    public static async Task VerifyDiagnosticsAsync()
    {
        using (var draft = BuildDraft())
        {
            await draft.GenerateBytesAsync();
            string[] reported = draft.Diagnostics
                .Select(d => string.Join(" | ", d.Severity.ToString().ToLowerInvariant(), d.Category.ToString().ToLowerInvariant(), d.Source, d.Message))
                .Order(StringComparer.Ordinal).ToArray();
            string[] printed = DraftDiagnostics
                .Select(d => string.Join(" | ", d.Severity, d.Category, d.Source, d.Message))
                .Order(StringComparer.Ordinal).ToArray();
            if (!reported.SequenceEqual(printed))
                throw new InvalidOperationException($"The draft reported [{string.Join("; ", reported)}], not the diagnostics the report prints.");
        }
        using var corrected = Generate();
        await corrected.GenerateBytesAsync();
        if (corrected.Diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
            throw new InvalidOperationException("The corrected document still reports a problem.");
    }

    private static Heading SectionHeading(string text, float top = 0) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        Color = ColorPalette.Slate[900],
        Margin = new Thickness(0, top, 0, 8)
    };

    private static TextBlock Cell(string text, bool bold = false) => new(text)
    {
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 9,
        LineHeight = 1.35f
    };

    /// <summary>A rounded severity label.</summary>
    private static Container Pill(string severity) => new()
    {
        BackgroundColor = ColorPalette.Amber[100],
        CornerRadius = 8,
        Padding = new Thickness(6, 3, 6, 3),
        Child = new TextBlock(severity.ToUpperInvariant())
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 7,
            LetterSpacing = 0.8f,
            Color = ColorPalette.Amber[900],
            Alignment = TextAlignment.Center
        }
    };

    /// <summary>A tinted card with a build's outcome.</summary>
    private static Container StatusCard(string label, string result, string note, bool amber)
    {
        VerticalStack stack = new() { Spacing = 3 };
        stack.Add(new TextBlock(label.ToUpperInvariant()) { FontName = StandardFonts.HelveticaBold, FontSize = 7.5f, LetterSpacing = 1, Color = ColorPalette.Slate[600] });
        stack.Add(new TextBlock(result) { FontName = StandardFonts.HelveticaBold, FontSize = 16, Color = amber ? ColorPalette.Amber[900] : ColorPalette.Emerald[900] });
        stack.Add(new TextBlock(note) { FontSize = 9 });
        return new Container
        {
            BackgroundColor = amber ? ColorPalette.Amber[50] : ColorPalette.Emerald[50],
            Border = new Border(4, 0, 0, 0),
            BorderColors = new BorderColors(amber ? ColorPalette.Amber[500] : ColorPalette.Emerald[600]),
            Padding = new Thickness(12),
            Child = stack
        };
    }
}
