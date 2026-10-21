using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Templates.FieldService;

/// <summary>
/// A facilities job sheet for an emergency call-out: priority banner, barcode and QR code, site and job cards,
/// work carried out, parts table, cost summary, and engineer and client sign-off, on one page.
/// </summary>
[System.ComponentModel.DisplayName("Reactive work order")]
[System.ComponentModel.Category("Field service")]
[System.ComponentModel.Description(
    "A one-page reactive work order for an emergency facilities call-out, with barcode and QR code, priority banner, site and job details, parts, costs and sign-off. PDF/A-2a + PDF/UA-1.")]
public static class ReactiveWorkOrderExample
{
    // ── Palette ──────────────────────────────────────────────────────────────
    private static readonly Color Teal = Color.FromHex("#0e7c86");
    private static readonly Color Navy = Color.FromHex("#0f2f3d");
    private static readonly Color Ink = Color.FromHex("#33424d");
    private static readonly Color Muted = Color.FromHex("#5b6b76");
    private static readonly Color Rule = Color.FromHex("#d5dde2");
    private static readonly Color Panel = Color.FromHex("#f1f5f7");
    private static readonly Color Red = Color.FromHex("#b42318");
    private static readonly Color Green = Color.FromHex("#1a7f37");
    private static readonly Color SignLine = Color.FromHex("#8a99a3");

    // ── Sample data ──────────────────────────────────────────────────────────
    private const string WorkOrderNumber = "WO-2026-04817";
    private const decimal LabourHours = 2.5m;
    private const decimal LabourRate = 68m;
    private const decimal CallOut = 85m;
    private const decimal VatRate = 0.2m;

    private static readonly (string Label, string Value)[] SiteRows =
    [
        ("Client", "Northgate Retail Park Ltd"),
        ("Site", "Northgate Retail Park, Unit 7"),
        ("Address", "Fenwick Road, York YO10 4AB"),
        ("Location", "Plant room B, Level 1"),
        ("Site contact", "Priya Nair"),
        ("Telephone", "01904 555 0142"),
    ];

    private static readonly (string Label, string Value)[] JobRows =
    [
        ("Work order", WorkOrderNumber),
        ("Raised", "19 Oct 2026, 14:32 (helpdesk call)"),
        ("Category", "Mechanical - HVAC"),
        ("Asset", "AHU-07, air handling unit"),
        ("Attended", "19 Oct 2026, 16:05 (within SLA)"),
        ("Resolved", "19 Oct 2026, 18:40 (within SLA)"),
    ];

    private sealed record Part(string PartNo, string Description, int Qty, decimal Unit);

    private static readonly Part[] Parts =
    [
        new("BLT-B62", "Drive belt, B-section 62 inch", 2, 18.50m),
        new("BRG-2213", "Self-aligning bearing, fan shaft, drive end", 1, 64.40m),
        new("CTR-3P25", "Three-pole contactor, 25 A, 24 V coil", 1, 111.00m),
    ];

    private static string Gbp(decimal amount) => "GBP " + amount.ToString("F2", CultureInfo.InvariantCulture);

    // ── Document ─────────────────────────────────────────────────────────────
    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, 40)
        {
            PageColor = Color.White,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            FooterTemplate = pageNumber => FooterRow(
                "Meridian Facilities Services  |  24/7 Helpdesk 0800 123 4567  |  helpdesk@meridian-fs.example",
                $"Page {pageNumber} of {TextElementBase.TotalPagesToken}"),
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = $"Reactive Work Order {WorkOrderNumber}";
        doc.Metadata.Author = "Meridian Facilities Services";
        doc.Metadata.Subject = "Reactive work order job sheet and completion record (fictional sample).";

        // Accent bar
        doc.Add(new Container { BackgroundColor = Teal, Height = 6 });

        // Title block + barcode / QR
        HorizontalStack titleRow = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 10,
            Margin = new Thickness(0, 12, 0, 10),
        };
        VerticalStack titleStack = new();
        titleStack.Add(Text("MERIDIAN FACILITIES SERVICES", bold: true, size: 9, color: Teal, mb: 4));
        titleStack.Add(new Heading("Reactive Work Order")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Navy,
            Margin = new Thickness(0, 0, 0, 3),
        });
        titleStack.Add(Text("Job sheet and completion record", color: Muted));
        titleRow.Add(titleStack, 1.8f);
        titleRow.Add(Codes(), 1.5f);
        doc.Add(titleRow);

        // Priority banner
        HorizontalStack bannerRow = new() { WidthDistribution = WidthDistribution.Weighted };
        bannerRow.Add(Text("PRIORITY 1  -  EMERGENCY", bold: true, size: 11, color: Color.White), 1);
        bannerRow.Add(
            Text("Attend within 4 hours  |  Resolve within 24 hours", color: Color.White, align: TextAlignment.Right),
            1.4f);
        doc.Add(new Container
        {
            BackgroundColor = Red,
            CornerRadius = 6,
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 0, 0, 12),
            Child = bannerRow,
        });

        // Site and job cards
        HorizontalStack cards = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 12),
        };
        cards.Add(InfoCard("SITE AND CONTACT", SiteRows), 1);
        cards.Add(InfoCard("JOB DETAILS", JobRows), 1);
        doc.Add(cards);

        // Narrative sections
        doc.Add(SectionHeading("Reported fault"));
        doc.Add(PanelBox(
            "AHU-07 serving the retail mall has tripped and will not restart. Mall temperature is rising and the unit " +
            "is making a loud grinding noise before shutdown. Building management system shows a supply fan failure alarm."));

        doc.Add(SectionHeading("Work carried out"));
        doc.Add(PanelBox(
            "Isolated and locked off AHU-07 under permit to work PTW-2291. Found the supply fan drive end bearing seized " +
            "and both drive belts frayed. Replaced bearing and belts, re-tensioned and aligned the pulleys, and replaced " +
            "the failed fan contactor. Restored supply, ran the unit for 45 minutes with no abnormal noise or vibration, " +
            "and confirmed normal alarm state on the BMS. Recommend a full vibration survey of AHU-05 to AHU-09 at the " +
            "next planned visit."));

        // Parts table
        doc.Add(SectionHeading("Parts and materials"));
        doc.Add(PartsTable());

        // Completion status + cost summary
        HorizontalStack summary = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 16,
            Margin = new Thickness(0, 0, 0, 12),
        };
        summary.Add(CompletionStatus(), 1);
        summary.Add(CostSummary(), 1);
        doc.Add(summary);

        // Sign-off
        HorizontalStack sign = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 16 };
        sign.Add(SignBlock("ENGINEER SIGN-OFF", "Tom Ashworth, engineer ID ENG-0412"), 1);
        sign.Add(SignBlock("CLIENT ACCEPTANCE", "Name: ______________________________"), 1);
        doc.Add(sign);

        return doc;
    }

    // ── Sections ─────────────────────────────────────────────────────────────

    /// <summary>Barcode + QR code, both with alt text for PDF/UA.</summary>
    private static HorizontalStack Codes()
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 8 };
        row.Add(new Barcode
        {
            Value = WorkOrderNumber,
            Symbology = BarcodeSymbology.Code128,
            Height = 38,
            ModuleWidth = 0.9f,
            IncludeTextLabel = true,
            TextLabelFontSize = 7,
            AltText = $"Barcode for work order {WorkOrderNumber}",
        }, 3);
        row.Add(new QRCode
        {
            Data = $"https://meridian-fs.example/wo/{WorkOrderNumber}",
            Size = 46,
            ErrorCorrection = QrErrorCorrection.Medium,
            AltText = $"QR code linking to work order {WorkOrderNumber}",
        }, 1);
        return row;
    }

    private static Container InfoCard(string title, (string Label, string Value)[] rows)
    {
        Grid grid = new(0.9f, 1.7f) { CellPadding = 2.5f };
        foreach ((string label, string value) in rows)
        {
            grid.AddRow(Text(label, bold: true, color: Muted), Text(value));
        }

        VerticalStack stack = new();
        stack.Add(Eyebrow(title));
        stack.Add(grid);
        return new Container
        {
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(10, 10, 10, 10),
            Child = stack,
        };
    }

    private static Table PartsTable()
    {
        Table table = new(1.1f, 3.6f, 0.6f, 1f, 1f)
        {
            CellPadding = 5,
            Margin = new Thickness(0, 0, 0, 10),
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Navy,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("Part no."),
            Th("Description"),
            Th("Qty", TextAlignment.Center),
            Th("Unit", TextAlignment.Right),
            Th("Total", TextAlignment.Right));

        foreach (Part p in Parts)
        {
            table.AddRow(
                Text(p.PartNo),
                Text(p.Description),
                Text(p.Qty.ToString(), align: TextAlignment.Center),
                Text(Gbp(p.Unit), align: TextAlignment.Right),
                Text(Gbp(p.Qty * p.Unit), align: TextAlignment.Right));
        }

        return table;
    }

    private static VerticalStack CompletionStatus()
    {
        Grid grid = new(1.1f, 1.5f) { CellPadding = 2.5f };
        grid.AddRow(Text("Outcome", bold: true, color: Muted),
            Text("Completed - fault resolved", bold: true, color: Green));
        grid.AddRow(Text("Follow-up visit", bold: true, color: Muted), Text("Not required"));
        grid.AddRow(Text("Permit to work", bold: true, color: Muted), Text("PTW-2291, closed 18:40"));
        grid.AddRow(Text("Time on site", bold: true, color: Muted), Text($"{LabourHours.ToString(CultureInfo.InvariantCulture)} hours, 1 engineer"));

        VerticalStack stack = new();
        stack.Add(Eyebrow("COMPLETION STATUS"));
        stack.Add(grid);
        return stack;
    }

    private static VerticalStack CostSummary()
    {
        decimal parts = Parts.Sum(p => p.Qty * p.Unit);
        decimal labour = LabourHours * LabourRate;
        decimal subtotal = parts + labour + CallOut;
        decimal vat = subtotal * VatRate;
        decimal total = subtotal + vat;

        Grid grid = new(1.6f, 1f) { CellPadding = 2.5f };
        void Line(string label, decimal amount) =>
            grid.AddRow(Text(label), Text(Gbp(amount), align: TextAlignment.Right));

        Line($"Labour ({LabourHours.ToString(CultureInfo.InvariantCulture)} h at {Gbp(LabourRate)})", labour);
        Line("Parts and materials", parts);
        Line("Emergency call-out charge", CallOut);
        Line("Subtotal", subtotal);
        Line("VAT at 20%", vat);
        grid.AddRow(
            Text("Total", bold: true, size: 11, color: Navy),
            Text(Gbp(total), bold: true, size: 11, color: Navy, align: TextAlignment.Right));

        VerticalStack stack = new();
        stack.Add(Eyebrow("COST SUMMARY"));
        stack.Add(grid);
        return stack;
    }

    private static VerticalStack SignBlock(string title, string nameLine)
    {
        VerticalStack stack = new();
        stack.Add(Eyebrow(title, mb: 4));
        stack.Add(Text(nameLine));
        stack.Add(new Container
        {
            Height = 30,
            Border = Border.BottomOnly(0.75f),
            BorderColors = BorderColors.BottomOnly(SignLine),
        });
        stack.Add(Text("Signature  |  Date and time", size: 7.5f, color: Muted, mt: 2));
        return stack;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TextBlock Text(
        string value,
        bool bold = false,
        float size = 9,
        Color? color = null,
        TextAlignment? align = null,
        float mt = 0,
        float mb = 0)
    {
        TextBlock block = new()
        {
            Text = value,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color ?? Ink,
            Margin = new Thickness(0, mt, 0, mb),
        };
        if (align is not null)
        {
            block.Alignment = align.Value;
        }

        return block;
    }

    private static TextBlock Eyebrow(string value, float mb = 5) =>
        Text(value, bold: true, size: 8, color: Teal, mb: mb);

    private static Heading SectionHeading(string value) => new(value)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        Color = Navy,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Container PanelBox(string body) => new()
    {
        BackgroundColor = Panel,
        CornerRadius = 6,
        Padding = new Thickness(10, 10, 10, 10),
        Margin = new Thickness(0, 0, 0, 10),
        Child = Text(body),
    };

    private static TextBlock Th(string value, TextAlignment align = TextAlignment.Left) =>
        Text(value, bold: true, color: Color.White, align: align);

    private static HorizontalStack FooterRow(string left, string right)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(Text(left, size: 7.5f, color: Muted), 4);
        row.Add(Text(right, bold: true, size: 7.5f, color: Muted, align: TextAlignment.Right), 1);
        return row;
    }
}
