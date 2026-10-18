using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Integration.ServerEndpoint;

/// <summary>
/// Generating a PDF from a server endpoint.
///
/// The question every technical evaluator asks before adopting a document
/// library: what does this look like inside my API, and what do I have to get
/// right?
///
/// The document below is deliberately ordinary — a receipt built from
/// request-shaped data. What the example is about is the code around it, which
/// is reproduced inside the document so the page and the source cannot drift
/// apart.
///
/// Four things matter in a server:
///
///   1. Use <c>GenerateBytesAsync</c>. The synchronous path is fully supported
///      on .NET, but a request handler should not block a pool thread on layout
///      and font IO.
///   2. Register fonts once at startup, not per request. Registration is
///      process-wide; doing it in the handler re-reads the same bytes every call.
///   3. Declare the licence once at startup, for the same reason.
///   4. Dispose the document when the response is written. It holds the laid-out
///      element tree, and a busy endpoint that leaks them will be noticed.
///
/// Tagged PDF/UA-1 and archival PDF/A-2a.
/// </summary>
[System.ComponentModel.DisplayName("Server Endpoint")]
[System.ComponentModel.Category("Integration")]
[System.ComponentModel.Description(
    "Generating a PDF in an API handler: a receipt built from the request body, covering process-wide setup, the async path, disposal with a using declaration, and content length.")]
public static class ServerEndpointExample
{
    /// <summary>One line of the order, as the request body carries it.</summary>
    private sealed record OrderLine(string Sku, string Description, int Quantity, decimal UnitPrice);

    /// <summary>The shape a request body would carry.</summary>
    private sealed record OrderRequest(
        string OrderId,
        DateTime PlacedAt,
        string Customer,
        string Currency,
        OrderLine[] Lines);

    private static readonly OrderRequest Request = new(
        OrderId: "ORD-2026-771204",
        PlacedAt: new DateTime(2026, 9, 11, 9, 14, 0, DateTimeKind.Utc),
        Customer: "Sample Customer",
        Currency: "GBP",
        Lines:
        [
            new OrderLine("SKU-1180", "Annual subscription — standard plan", 1, 240.00m),
            new OrderLine("SKU-2240", "Additional user seats", 5, 36.00m),
            new OrderLine("SKU-3310", "Priority support upgrade", 1, 120.00m),
        ]);

    /// <summary>The handler as it would be written, shown in the document it produces.</summary>
    private static readonly string HandlerSource = string.Join(
        '\n',
        "// Program.cs - once per process, not per request",
        "BonniePdfLicense.Tier = LicenseTier.Professional;   // your tier",
        "FontManager.RegisterFont(File.ReadAllBytes(\"./fonts/Inter-Regular.ttf\"),",
        "                         family: \"Inter\");",
        string.Empty,
        "var app = WebApplication.Create(args);",
        string.Empty,
        "// per request",
        "app.MapPost(\"/orders/{id}/receipt\", async (string id, OrderRequest body,",
        "                                           CancellationToken ct) =>",
        "{",
        "    using var doc = BuildReceipt(body);    // pure: data in, Document out",
        "    byte[] bytes = await doc.GenerateBytesAsync(ct);",
        "    return Results.File(bytes, \"application/pdf\", $\"receipt-{id}.pdf\");",
        "});                                        // `using` disposes the document",
        string.Empty,
        "app.Run();");

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        decimal net = Request.Lines.Sum(l => l.Quantity * l.UnitPrice);
        decimal vat = net * 0.2m;

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Generated server-side for order {Request.OrderId}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Receipt {Request.OrderId}";
        doc.Metadata.Author = "Sample Supplier Ltd";
        doc.Metadata.Subject = "A receipt generated from a server endpoint, with the handler pattern that produced it.";
        doc.Metadata.Keywords = "server, api, endpoint, asp.net core, http response, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("GENERATED FROM A SERVER ENDPOINT", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1($"Receipt {Request.OrderId}"));
        doc.Add(Cell($"{Request.Customer} · placed {GbDate(Request.PlacedAt)}", color: Muted, marginBottom: 14f));

        doc.Add(WeightedRow(
        [
            Field("Order", Request.OrderId),
            Field("Placed", GbDate(Request.PlacedAt)),
            Field("Currency", Request.Currency),
            Field("Customer", Request.Customer),
        ]));

        Table table = new(1.4f, 4.4f, 0.8f, 1.2f, 1.3f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(Th("SKU"), Th("Description"), Th("Qty"), Th("Unit price", TextAlignment.Right),
            Th("Amount", TextAlignment.Right));
        foreach (OrderLine line in Request.Lines)
        {
            table.AddRow(
                Td(line.Sku),
                Td(line.Description),
                Td(line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                Td(Money(line.UnitPrice), align: TextAlignment.Right),
                Td(Money(line.Quantity * line.UnitPrice), bold: true, align: TextAlignment.Right));
        }

        doc.Add(table);

        doc.Add(TotalsPanel(
        [
            TotalLine("Net", Money(net)),
            TotalLine("VAT at 20%", Money(vat)),
            Divider(),
            TotalLine("Total paid", Money(net + vat), bold: true, size: 11f, color: Accent),
        ]));

        doc.Add(H2("The handler"));
        doc.Add(Cell(
            "This document was produced by the pattern below. The build step is a pure function of the request body, "
            + "which is what makes it testable without an HTTP server in the loop.",
            size: 9.5f,
            color: Muted,
            marginBottom: 8f));
        doc.Add(CodeBlock(HandlerSource));

        // The heading travels with the first point it introduces.
        VerticalStack firstPoint = new() { BreakAllowed = false };
        firstPoint.Add(H2("What to get right"));
        firstPoint.Add(Panel("Once per process, not per request",
        [
            Cell(
                "Font registration and the licence declaration are process-wide. Doing either inside the handler "
                + "re-reads the same font bytes on every call, and shows up as latency under load.",
                size: 9f),
        ]));
        doc.Add(firstPoint);
        doc.Add(Panel("Await the async path",
        [
            Cell(
                "Use GenerateBytesAsync. The synchronous path is fully supported on .NET, but a request handler "
                + "should not block a pool thread on layout and font IO in any case.",
                size: 9f),
        ]));
        doc.Add(Panel("Dispose when the response is written",
        [
            Cell(
                "A document holds its laid-out element tree. Declare it with using, so a thrown serialisation "
                + "error does not leak one per failed request.",
                size: 9f),
        ]));
        doc.Add(Panel("Set the length; stream when the document is large",
        [
            Cell(
                "GenerateBytesAsync returns a byte array, so the content length is known before the first byte goes "
                + "out. For documents large enough to matter, call SaveAsync with the response body stream "
                + "rather than buffering the whole response.",
                size: 9f),
        ]));

        return doc;
    }

    /// <summary>Monospaced source on a tinted panel. <c>PreserveWhitespace</c> keeps the listing's indentation.</summary>
    private static Container CodeBlock(string source) => new()
    {
        BackgroundColor = ColorPalette.Slate[50],
        Border = new Border(0.75f),
        BorderColors = new BorderColors(Rule),
        CornerRadius = 6,
        Padding = new Thickness(12),
        Margin = new Thickness(0, 0, 0, 12),
        Child = new TextBlock
        {
            Text = source,
            FontName = StandardFonts.Courier,
            FontSize = 7.6f,
            Color = Ink,
            PreserveWhitespace = true,
        },
    };

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

    /// <summary>A block of plain lines (an address), tagged as one paragraph.</summary>
    private static TextBlock Lines(IEnumerable<string> items, float size = 9f, Color? color = null, TextAlignment align = TextAlignment.Left) => Cell(string.Join('\n', items), size: size, color: color, align: align);

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

    /// <summary>One label/value pair for the reference grid.</summary>
    private static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 2f));
        stack.Add(Cell(value, size: 9.5f));
        return stack;
    }

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

    /// <summary>A "label ....... value" totals line, weighted so the value hugs the right.</summary>
    private static HorizontalStack TotalLine(string label, string value, bool bold = false, float size = 9f, Color? color = null)
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
    private static HorizontalStack TotalsPanel(IEnumerable<IElement> children, float weight = 1.6f)
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

        row.Add(new Container { BackgroundColor = ColorPalette.Indigo[50], CornerRadius = 6, Padding = new Thickness(12), Child = body, }, weight);
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

    /// <summary>A thin divider, used above a grand total.</summary>
    private static Container Divider() => new()
    {
        BackgroundColor = Rule,
        Height = 0.75f,
        Margin = new Thickness(0, 5, 0, 5),
    };

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
