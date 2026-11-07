using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.SalesSupplyChain.TradeDocuments;

/// <summary>
/// What travels with the goods. It completes the fulfilment story the shipping
/// label starts: the label gets the parcel to the door, the delivery note tells
/// goods-inwards what is supposed to be inside it and gives them somewhere to
/// record what actually was.
///
/// The detail that makes it a real document rather than a table: quantities are
/// ordered, despatched, and back-ordered as three separate columns, because a
/// part-shipment is the normal case and a delivery note that cannot express one
/// is useless. It ends with a goods-received signature and a discrepancy notice
/// period.
///
/// Sample organisations and references throughout.
/// </summary>
[System.ComponentModel.DisplayName("Delivery note")]
[System.ComponentModel.Category("Sales & Supply Chain")]
[System.ComponentModel.Description(
    "A delivery note and packing slip carrying ordered, despatched and back-ordered quantities as separate columns, a package breakdown, and a goods-received signature. PDF/A-2a + PDF/UA-1.")]
public static class DeliveryNoteExample
{
    private const string Number = "DN-2026-018844";
    private const string OrderNumber = "PO-2026-004871";
    private const string Account = "ACC-004182";
    private const string Carrier = "Sample Carrier — next working day";
    private const string Consignment = "C-8841-2026";
    private const int Packages = 4;
    private const string GrossWeight = "38.6 kg";
    private const string Hours = "Deliveries accepted 07:00–15:00, working days only";

    private static readonly DateTime Despatched = new(2026, 9, 18);

    private static readonly string[] From =
    [
        "Sample Supplier Ltd", "Despatch Bay 2", "Sample Industrial Estate", "Sample City", "SC2 9BD",
    ];

    private static readonly string[] To =
    [
        "Sample Buyer Ltd", "Goods Inwards, Gate 3", "Sample Distribution Centre", "Sample Town", "ST9 2LD",
    ];

    private sealed record DeliveryLine(
        int OrderLine,
        string PartCode,
        string Description,
        int Ordered,
        int Despatched,
        string Unit,
        string PackedIn);

    private static readonly DeliveryLine[] Lines =
    [
        new(1, "FLT-G4-592", "Panel filter G4, 592 × 592 × 48mm", 48, 48, "each", "Pallet 1"),
        new(2, "BRG-6204-2RS", "Sealed deep-groove bearing, 20mm bore", 24, 24, "each", "Carton 1"),
        new(3, "BLT-SPZ-1287", "Drive belt, SPZ section, 1287mm", 12, 8, "each", "Carton 1"),
        new(4, "SEAL-AHU-12", "Access door seal, 12mm closed cell", 60, 60, "metre", "Carton 2"),
        new(5, "LUB-SYN-5L", "Synthetic bearing lubricant, 5 litre", 4, 0, "each", "—"),
        new(6, "PPE-GLV-CR5", "Cut-resistant gloves, level 5, size L", 40, 40, "pair", "Carton 3"),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        int complete = Lines.Count(l => l.Despatched == l.Ordered);
        DeliveryLine[] backOrdered = [.. Lines.Where(l => l.Despatched < l.Ordered)];
        int units = Lines.Sum(l => l.Despatched);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Delivery note {Number} · order {OrderNumber} · this is not an invoice",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Delivery note {Number} — order {OrderNumber}";
        doc.Metadata.Author = "Sample Supplier Ltd";
        doc.Metadata.Subject =
            "Delivery note and packing slip: ordered, despatched and back-ordered quantities, package breakdown, and goods-received signature (sample data).";
        doc.Metadata.Keywords = "delivery note, packing slip, despatch, goods inwards, logistics, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("DELIVERY NOTE · PACKING SLIP", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1($"Delivery {Number}"));
        doc.Add(Cell(
            "This is not an invoice. No payment is due against this document.",
            color: Muted,
            marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Field("Despatched", GbDate(Despatched)),
                Field("Your order", OrderNumber),
                Field("Account", Account),
                Field("Consignment", Consignment),
            ],
            marginBottom: 6f));
        doc.Add(WeightedRow(
        [
            Field("Carrier", Carrier),
            Field("Packages", Packages.ToString(CultureInfo.InvariantCulture)),
            Field("Gross weight", GrossWeight),
            Field("Delivery hours", Hours),
        ]));

        doc.Add(WeightedRow(
            [PartyBlock("Despatched from", From), PartyBlock("Delivered to", To)],
            spacing: 20f,
            marginBottom: 4f));

        doc.Add(WeightedRow(
            [
                Tile(Lines.Length.ToString(CultureInfo.InvariantCulture), "Order lines"),
                Tile(complete.ToString(CultureInfo.InvariantCulture), "Complete", Positive),
                Tile(
                    backOrdered.Length.ToString(CultureInfo.InvariantCulture),
                    "Part or back-ordered",
                    backOrdered.Length > 0 ? Negative : Positive),
                Tile(units.ToString(CultureInfo.InvariantCulture), "Units despatched"),
            ],
            spacing: 10f,
            marginBottom: 4f));

        doc.Add(H2("Contents"));

        Table table = new(0.6f, 1.5f, 3.6f, 0.9f, 1.05f, 1.05f, 0.8f, 1f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("#"),
            Th("Part code"),
            Th("Description"),
            Th("Ordered", TextAlignment.Right),
            Th("Despatched", TextAlignment.Right),
            Th("To follow", TextAlignment.Right),
            Th("Unit"),
            Th("Packed in"));
        foreach (DeliveryLine line in Lines)
        {
            int toFollow = line.Ordered - line.Despatched;
            table.AddRow(
                Td(line.OrderLine.ToString(CultureInfo.InvariantCulture)),
                Td(line.PartCode),
                Td(line.Description),
                Td(line.Ordered.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                Td(line.Despatched.ToString(CultureInfo.InvariantCulture), bold: true, align: TextAlignment.Right),
                Td(
                    toFollow == 0 ? "—" : toFollow.ToString(CultureInfo.InvariantCulture),
                    bold: toFollow > 0,
                    color: toFollow > 0 ? Negative : Muted,
                    align: TextAlignment.Right),
                Td(line.Unit, color: Muted),
                Td(line.PackedIn, color: Muted));
        }

        doc.Add(table);

        // What to report sits beside what is still to come, so the note stays on one page.
        Container discrepancies = Panel("Discrepancies",
        [
            Cell(
                "Shortages, damage, or incorrect items must be noted on this delivery note at the point of delivery "
                + "and reported to the supplier within three working days, quoting the delivery note number.",
                size: 9f),
        ]);
        if (backOrdered.Length > 0)
        {
            List<IElement> notes = [];
            foreach (DeliveryLine line in backOrdered)
            {
                notes.Add(Cell(
                    $"Line {line.OrderLine} · {line.PartCode} — {line.Ordered - line.Despatched} {line.Unit} to "
                    + "follow on a later delivery against the same order.",
                    size: 9f,
                    marginBottom: 3f));
            }

            notes.Add(Cell(
                "Back-ordered items are not invoiced until they are despatched.",
                size: 9f,
                color: Muted));
            doc.Add(WeightedRow([Panel("Items to follow", notes, Negative), discrepancies], marginBottom: 0f));
        }
        else
        {
            doc.Add(discrepancies);
        }

        doc.Add(H2("Goods received"));
        doc.Add(Cell(
            "Please check the contents against this note before signing. Signing acknowledges the number of "
            + "packages received and their apparent condition; it is not acceptance of the contents until they "
            + "are checked.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));

        doc.Add(WeightedRow(
            [
                ReceiptSlot("Received by", "Name in capitals"),
                ReceiptSlot("Signature", string.Empty),
                ReceiptSlot("Date and time", string.Empty),
            ],
            spacing: 22f,
            marginBottom: 6f));

        return doc;
    }

    private static VerticalStack ReceiptSlot(string role, string hint)
    {
        VerticalStack stack = new();
        stack.Add(Cell(role.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 14f));
        stack.Add(new Container
        {
            BackgroundColor = Rule,
            Height = 0.75f,
            Margin = new Thickness(0, 0, 0, 5),
        });
        stack.Add(Cell(hint, size: 8f, color: Muted));
        return stack;
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Indigo[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Color Positive = ColorPalette.Emerald[700];

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

    private static string GbDate(DateTime date) => date.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));

    private static TextBlock AddressLines(IEnumerable<string> items, float size = 9f) => Cell(string.Join('\n', items), size: size);
}
