using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.BillingPayments.BillingDocuments;

/// <summary>
/// The correcting document: what was over-charged on an earlier invoice, why, and
/// how the credit is applied. Every billing system issues them, few example
/// catalogues show one, and the details that matter are the ones an auditor looks
/// for - the original invoice reference, the reason code, the VAT treatment of
/// the reversal, and whether the money is refunded or offset.
///
/// The visual grammar is the invoice's, deliberately: a credit note that does not
/// look like the invoice it corrects is harder to reconcile. The signal that this
/// is a reversal is the colour of the totals and the sign on every amount.
///
/// Sample organisations, references, and amounts throughout.
/// </summary>
[System.ComponentModel.DisplayName("Credit note")]
[System.ComponentModel.Category("Billing & Payments")]
[System.ComponentModel.Description(
    "A credit note against an earlier invoice: corrected lines with their reasons, the VAT reversal, and how the credit is applied to the account. PDF/A-2a + PDF/UA-1.")]
public static class CreditNoteExample
{
    private const decimal VatRate = 0.2m;

    private static readonly DateTime Issued = new(2026, 8, 11);
    private static readonly DateTime InvoiceDated = new(2026, 8, 7);

    private const string Number = "CRN-2026-0118";
    private const string AgainstInvoice = "INV-2026-3340";
    private const string Account = "ACC-004182";
    private const string ReasonCode = "CR-04 — Billed in error";
    private const string Treatment = "Offset against the account balance";
    private const string ApprovedBy = "P. Ahmadi — Credit Control";

    private static readonly string[] Supplier =
    [
        "Sample Supplier Ltd", "12 Sample Street", "Sample City", "SC2 9BD", "VAT GB 000 0000 00",
    ];

    private static readonly string[] Customer =
    [
        "Sample Customer Ltd", "Unit 7, Sample Business Park", "Sample Town", "SA1 4TP",
    ];

    private sealed record CreditLine(
        string OriginalLine,
        string Description,
        string Reason,
        int Quantity,
        decimal UnitPrice);

    private static readonly CreditLine[] Lines =
    [
        new(
            "Line 3",
            "Collaboration module — annual licence",
            "Duplicated on the original invoice; the same licence appears on line 2",
            1,
            280.00m),
        new(
            "Line 6",
            "Additional storage — 500GB",
            "Billed at the list rate; the contracted rate under schedule 2 applies",
            1,
            70.00m),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        decimal net = Lines.Sum(l => l.Quantity * l.UnitPrice);
        decimal vat = net * VatRate;
        decimal total = net + vat;

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Credit note {Number} against invoice {AgainstInvoice}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Credit note {Number} — against invoice {AgainstInvoice}";
        doc.Metadata.Author = "Sample Supplier Ltd";
        doc.Metadata.Subject =
            "Credit note against an earlier invoice: corrected lines with reasons, VAT reversal, and how the credit is applied (sample data).";
        doc.Metadata.Keywords = "credit note, credit memo, billing correction, vat, accounts receivable, pdf/ua, pdf/a";

        // The accent is the reversal colour, not the house colour: a credit note
        // read at a glance must not be mistaken for another invoice.
        doc.Add(AccentBar(Negative));
        doc.Add(Cell("CREDIT NOTE", size: 8.5f, bold: true, color: Negative, marginBottom: 4f));
        doc.Add(H1($"Credit note {Number}"));
        doc.Add(Cell(
            $"Against invoice {AgainstInvoice} dated {GbDate(InvoiceDated)}",
            color: Muted,
            marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Field("Credit note date", GbDate(Issued)),
                Field("Account", Account),
                Field("Original invoice", AgainstInvoice),
                Field("Reason code", ReasonCode),
            ],
            marginBottom: 6f));

        doc.Add(WeightedRow(
            [PartyBlock("From", Supplier), PartyBlock("Credit to", Customer)],
            spacing: 20f));

        doc.Add(H2("Lines credited"));
        doc.Add(Cell(
            "Each line references the line of the original invoice it corrects, and states why.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));

        Table table = new(1f, 2.6f, 4f, 0.7f, 1.2f, 1.2f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Negative,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("Original"),
            Th("Description"),
            Th("Reason for credit"),
            Th("Qty"),
            Th("Unit price", TextAlignment.Right),
            Th("Credit", TextAlignment.Right));
        foreach (CreditLine line in Lines)
        {
            table.AddRow(
                Td(line.OriginalLine, bold: true),
                Td(line.Description),
                Td(line.Reason, color: Muted),
                Td(line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                Td(Money(line.UnitPrice), align: TextAlignment.Right),
                Td(Money(-(line.Quantity * line.UnitPrice)), bold: true, color: Negative, align: TextAlignment.Right));
        }

        doc.Add(table);

        doc.Add(TotalsPanel(
        [
            TotalLine("Net credit", Money(-net)),
            TotalLine($"VAT at {VatRate * 100:0}% reversed", Money(-vat)),
            Divider(),
            TotalLine("Total credited", Money(-total), bold: true, size: 11f, color: Negative),
        ]));

        doc.Add(H2("How this credit is applied"));
        doc.Add(Panel(
            Treatment,
            [
                Cell(
                    $"The credit of {Money(total)} has been applied to account {Account} and appears on the next "
                    + "statement. No refund is due; the balance payable is reduced accordingly.",
                    size: 9f,
                    marginBottom: 3f),
                Cell(
                    "If the original invoice has already been paid in full, the credit may instead be refunded on "
                    + "request to the credit control contact shown below.",
                    size: 9f),
            ],
            Negative));

        doc.Add(H2("VAT"));
        doc.Add(Cell(
            $"This credit note reverses output tax of {Money(vat)} previously accounted for on invoice "
            + $"{AgainstInvoice}. The customer should adjust input tax recovered on that invoice by the same "
            + "amount in the period in which this credit note is received.",
            size: 9f,
            color: Muted,
            marginBottom: 10f));

        doc.Add(Cell(
            $"Approved by {ApprovedBy} on {GbDate(Issued)}. Queries: quote {Number}.",
            size: 8.5f,
            color: Muted));

        return doc;
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Blue[800];

    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Color Negative = ColorPalette.Rose[700];

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

    /// <summary>A labelled address block.</summary>
    private static VerticalStack PartyBlock(string label, IEnumerable<string> address)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 7f, bold: true, color: Muted, marginBottom: 3f));
        stack.Add(AddressLines(address, size: 9f));
        return stack;
    }

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

        row.Add(new Container { BackgroundColor = ColorPalette.Blue[50], CornerRadius = 6, Padding = new Thickness(12), Child = body, }, weight);
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

    private static TextBlock AddressLines(IEnumerable<string> items, float size = 9f) => Cell(string.Join('\n', items), size: size);
}
