using System.Diagnostics;
using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Integration.BatchGeneration;

/// <summary>
/// Batch generation — many documents in one process.
///
/// The other half of the server question: what happens when you generate ten
/// thousand of these rather than one?
///
/// This example generates a run of statements, measures it, and reports the run
/// in the document it produces — so the numbers on the page are the numbers from
/// the run that made it, not a claim typed into a template.
///
/// What it demonstrates, and what a batch job has to get right:
///
///   · One process, many documents. Fonts and the licence declaration are
///     process-wide, so the per-document cost excludes them after the first.
///   · Dispose each document as it is written. The peak memory of a batch is the
///     size of one document, not of the run, if you release each one.
///   · Reuse the data pipeline, not the Document. A Document is not reusable
///     after generation; build a fresh one per record.
///   · The licence notice fires once per process, not once per document — which
///     is what makes it acceptable in a job that runs nightly.
///
/// Tagged PDF/UA-1 and archival PDF/A-2a.
/// </summary>
[System.ComponentModel.DisplayName("Batch Generation")]
[System.ComponentModel.Category("Integration")]
[System.ComponentModel.Description(
    "Generating 250 documents in one process. The output reports the timings of its own run and covers per-document lifecycle, disposal in the loop, and one-time setup.")]
public static class BatchGenerationExample
{
    private const int RunSize = 250;

    /// <summary>One record in the run.</summary>
    private sealed record Statement(string Account, string Customer, decimal Balance);

    /// <summary>A deterministic synthetic ledger, so the run is repeatable.</summary>
    private static Statement[] Records(int count)
        =>
        [
            .. Enumerable.Range(0, count).Select(i => new Statement(
                Account: $"ACC-{(4000 + i).ToString(CultureInfo.InvariantCulture).PadLeft(6, '0')}",
                Customer: $"Sample Customer {i + 1}",
                // Deterministic pseudo-amounts: no randomness, so two runs compare.
                Balance: Math.Round(((i * 7919) % 45000) + 250m + ((i * 31) % 100)) / 100m))
        ];

    /// <summary>Builds one small statement. A fresh Document per record, by design.</summary>
    private static Document BuildOne(Statement record)
    {
        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10f).WithColor(Ink);
        doc.Metadata.Title = $"Statement {record.Account}";
        doc.Metadata.Author = "Sample Supplier Ltd";

        doc.Add(AccentBar());
        doc.Add(H1($"Statement {record.Account}"));
        doc.Add(Cell(record.Customer, color: Muted, marginBottom: 10f));
        doc.Add(Cell($"Balance: {Money(record.Balance)}", size: 12f, bold: true));
        return doc;
    }

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        // ── The run ───────────────────────────────────────────────────────────
        //
        // Synchronous here so the example stays a plain Generate() like every other
        // entry in the catalogue. A real batch would use GenerateBytesAsync and a
        // bounded concurrency limit; the lifecycle points below are the same either
        // way.

        Statement[] batch = Records(RunSize);
        Stopwatch clock = Stopwatch.StartNew();
        long totalBytes = 0;
        long largest = 0;

        foreach (Statement record in batch)
        {
            // The peak memory of a batch is one document, not the run - but only if
            // each one is released as it is written.
            using Document one = BuildOne(record);
            byte[] bytes = one.GenerateBytes();
            totalBytes += bytes.Length;
            if (bytes.Length > largest) largest = bytes.Length;
        }

        double elapsedMs = clock.Elapsed.TotalMilliseconds;
        double perDoc = elapsedMs / RunSize;
        double perSecond = 1000 / perDoc;

        // ── The report ────────────────────────────────────────────────────────

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            "Figures measured during the run that produced this document",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Batch generation — {RunSize} documents in one process";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject =
            "Generating many documents in one process: lifecycle, disposal, and the measured throughput of the run that produced this report.";
        doc.Metadata.Keywords = "batch, throughput, bulk generation, server, performance, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("BATCH GENERATION", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1($"{RunSize} documents in one process"));
        doc.Add(Cell(
            "Every figure below was measured while producing this page. Run it on your own hardware and the numbers "
            + "change; the shape of the result does not.",
            color: Muted,
            marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Tile(RunSize.ToString(CultureInfo.InvariantCulture), "Documents generated"),
                Tile($"{elapsedMs.ToString("F0", CultureInfo.InvariantCulture)} ms", "Total elapsed"),
                Tile($"{perDoc.ToString("F2", CultureInfo.InvariantCulture)} ms", "Per document"),
                Tile($"{perSecond.ToString("F0", CultureInfo.InvariantCulture)}/s", "Throughput"),
            ],
            spacing: 10f,
            marginBottom: 6f));

        doc.Add(H2("What was measured"));

        Table table = new(3f, 2.2f, 4.6f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(Th("Measure"), Th("Value", TextAlignment.Right), Th("Note"));
        table.AddRow(
            Td("Documents in the run"),
            Td(RunSize.ToString(CultureInfo.InvariantCulture), bold: true, align: TextAlignment.Right),
            Td("Each a fresh document built from its own record", color: Muted));
        table.AddRow(
            Td("Total elapsed"),
            Td($"{elapsedMs.ToString("F1", CultureInfo.InvariantCulture)} ms", bold: true, align: TextAlignment.Right),
            Td("Build, lay out, and serialise; excludes writing to disk", color: Muted));
        table.AddRow(
            Td("Mean per document"),
            Td($"{perDoc.ToString("F3", CultureInfo.InvariantCulture)} ms", bold: true, align: TextAlignment.Right),
            Td("Font registration is process-wide and is not repaid per document", color: Muted));
        table.AddRow(
            Td("Total bytes produced"),
            Td($"{(totalBytes / 1024.0).ToString("F0", CultureInfo.InvariantCulture)} KiB", bold: true,
                align: TextAlignment.Right),
            Td("Sum of the serialised output across the run", color: Muted));
        table.AddRow(
            Td("Largest single document"),
            Td($"{(largest / 1024.0).ToString("F1", CultureInfo.InvariantCulture)} KiB", bold: true,
                align: TextAlignment.Right),
            Td("Peak working set tracks this, not the run total, when each is disposed", color: Muted));
        doc.Add(table);

        // The caveat sits with the figures it qualifies, not alone at the end of the document.
        doc.Add(Cell(
            $"Run completed {GbDate(DateTime.Now)} on the machine that generated this document. "
            + "Figures are a single sample, not a benchmark: they include process warm-up and whatever else the machine "
            + "was doing.",
            size: 8.5f,
            color: Muted,
            marginTop: 8f));

        doc.Add(H2("The lifecycle"));
        doc.Add(Panel("A document is not reusable after generation",
        [
            Cell(
                "Build a fresh document per record. The serialiser assumes a frozen tree, so mutating and re-generating one "
                + "is not a supported shape — and sharing one across records would serialise the same content repeatedly.",
                size: 9f),
        ]));
        doc.Add(Panel("Dispose inside the loop",
        [
            Cell(
                "Releasing each document as it is written keeps peak memory at the size of one document rather than the "
                + "whole run. Declare it with using inside the loop so a failed record does not leak.",
                size: 9f),
        ]));
        doc.Add(Panel("Process-wide work happens once",
        [
            Cell(
                "Font registration and the licence declaration are set up before the loop. The licence notice in "
                + "particular fires at most once per process, not once per document, which is what makes it "
                + "acceptable in a job that runs nightly.",
                size: 9f),
        ]));
        doc.Add(Panel("Bound the concurrency, do not remove it",
        [
            Cell(
                "A real batch would use the async path with a bounded worker pool, such as Parallel.ForEachAsync with "
                + "MaxDegreeOfParallelism. Unbounded parallelism on a document workload trades throughput for peak "
                + "memory, because every in-flight document holds its own laid-out tree.",
                size: 9f),
        ]));

        return doc;
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Indigo[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    /// <summary>A styled text cell with trade-document defaults.</summary>
    private static TextBlock Cell(string text, float size = 9f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left, float marginTop = 0f, float marginBottom = 0f) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        Color = color ?? Ink,
        Alignment = align,
        Margin = new Thickness(0, marginTop, 0, marginBottom),
    };

    /// <summary>Table header cell - white on the accent band.</summary>
    private static TextBlock Th(string text, TextAlignment align = TextAlignment.Left) => Cell(text, size: 8.5f, bold: true, color: Color.White, align: align);

    /// <summary>Table body cell.</summary>
    private static TextBlock Td(string text, float size = 8.5f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left) => Cell(text, size: size, bold: bold, color: color, align: align);

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 21,
        Color = Heading,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Heading H2(string text, Color? color = null) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        Color = color ?? Accent,
        Margin = new Thickness(0, 16, 0, 8),
    };

    /// <summary>The full-width accent rule at the top of each document.</summary>
    private static Container AccentBar(Color? color = null, float height = 4f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 14),
    };

    /// <summary>A row of evenly weighted elements.</summary>
    private static HorizontalStack WeightedRow(IEnumerable<IElement> items, float spacing = 14f, float marginTop = 0f, float marginBottom = 14f)
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

    /// <summary>A bordered panel with a heading - terms, notes, declarations.</summary>
    private static Container Panel(string title, IEnumerable<IElement> children, Color? accent = null)
    {
        VerticalStack body = new()
        {
            Spacing = 4
        };
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
    private static Container Tile(string value, string label, Color? accent = null)
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

    /// <summary>The running header / footer band.</summary>
    private static HorizontalStack Band(string left, string right)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted
        };
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
    private static string Money(decimal value, string symbol = "£", string culture = "en-GB") => (value < 0 ? "-" : string.Empty) + symbol + Math.Abs(value).ToString("N2", CultureInfo.GetCultureInfo(culture));

    private static string GbDate(DateTime date) => date.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
}
