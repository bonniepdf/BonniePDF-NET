using BonniePDF;

namespace BonniePDF.Examples.RetailEvents.EventTicket;

/// <summary>
/// Event ticket / boarding-pass style card: PDF/A-2a + PDF/UA-1 with a Code 128
/// ticket-id barcode and a QR entry pass (both with alt text), and a
/// perforation-style tear-off stub. Demonstrates QR + barcode + visual polish on
/// a custom landscape ticket page size. The event name is the document's H1.
/// </summary>
[System.ComponentModel.DisplayName("Event Ticket")]
[System.ComponentModel.Category("Retail & Events")]
[System.ComponentModel.Description(
    "Event ticket / boarding-pass style card (PDF/UA-1 + PDF/A-2a) with a QR entry pass, a Code 128 ticket-id barcode, and a perforation-style tear-off stub on a custom ticket page size.")]
public static class EventTicketExample
{
    private const string BgImageKey = "images/danny-howe-bn-D2bCvpik-unsplash.jpg";

    private static readonly Color Ink = ColorPalette.Slate[900];
    private static readonly Color Cream = ColorPalette.Slate[100];
    private static readonly Color Mute = ColorPalette.Slate[400];

    private static readonly Color BodyCol = ColorPalette.Slate[300];

    // 400, not 500: this text reads against the dark stub panel, where 500 falls
    // to 3.32:1 against the 4.5:1 WCAG AA threshold; 400 clears it (5.13:1).
    private static readonly Color Accent = ColorPalette.Violet[400];
    private static readonly Color Stub = ColorPalette.Slate[800];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        // Landscape boarding-pass style ticket: 640 x 300 pt.
        Document doc = new(new DocumentSettings
        {
            PageSize = new PdfSize(640, 300, "Event Ticket"),
            PreservePageDimensions = true,
            Margin = 0,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(BodyCol),
        });

        ArgumentNullException.ThrowIfNull(assets);
        if (!assets.TryGetValue(BgImageKey, out byte[]? bgData) || bgData.Length == 0)
            throw new InvalidOperationException($"Missing required asset: {BgImageKey}");

        doc.Settings.PageBackground = _ => new Container
        {
            Height = 300,
            Child = new Image
            {
                ImageData = bgData,
                Width = 640,
                Height = 300,
                FitMode = ImageFitMode.Cover,
                IsDecorative = true,
            },
        };

        doc.Metadata.Title = "Event Ticket - Sample Sound Festival";
        doc.Metadata.Author = "Sample Live";
        doc.Metadata.Subject = "Event ticket - accessible (PDF/UA) and archival (PDF/A-2a)";

        // ── Main body (dark) ──────────────────────────────────────────────────
        VerticalStack info = new();
        info.Add(Tb("SAMPLE LIVE presents", 8, Accent, bold: true, bottom: 6));
        info.Add(new Heading("Sample Sound Festival")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 19,
            Color = Cream,
            Margin = new Thickness(0, 0, 0, 3),
        });
        info.Add(Tb("Main Stage, Sample Park, Sample City", 10, BodyCol, bottom: 12));

        Grid facts = new(1f, 1f, 1f) { Border = Border.None, CellPadding = 0 };
        facts.AddRow(
            new Container { Child = Field("DATE", "18 Jul 2026") },
            new Container { Child = Field("GATES", "16:00") },
            new Container { Child = Field("ADMITS", "One") });
        info.Add(facts);

        // Ticket-id barcode runs along the full width of the body.
        Barcode ticketBarcode = new()
        {
            Value = "ASF26-4471-XK",
            Symbology = BarcodeSymbology.Code128,
            Height = 36,
            ModuleWidth = 1.2f,
            ForegroundColor = Ink,
            BackgroundColor = Cream,
            TextLabelFontSize = 7,
            AltText = "Ticket barcode (Code 128): ASF26-4471-XK",
        };
        info.Add(new Container { Child = ticketBarcode, Margin = new Thickness(0, 16, 0, 0) });

        Container body = new()
        {
            BackgroundColor = bgData is { Length: > 0 } ? Ink.WithAlpha(0.82f) : Ink,
            Height = 300,
            Padding = new Thickness(22, 18, 22, 16),
            Child = info,
        };

        // ── Tear-off stub (lighter) with the QR entry pass ────────────────────
        VerticalStack stub = new();
        stub.Add(Tb("ADMIT ONE", 8, Accent, bold: true, bottom: 8, align: TextAlignment.Center));

        QRCode qr = new()
        {
            Data = "https://tix.sample.example/t/ASF26-4471-XK",
            Size = 96,
            QuietZone = 1,
            ErrorCorrection = QrErrorCorrection.Quartile,
            ForegroundColor = Ink,
            AltText = "Entry QR code for ticket ASF26-4471-XK - scan at the gate",
        };
        Container qrCard = new()
        {
            BackgroundColor = Color.White,
            CornerRadius = 6,
            ClipContent = true,
            Padding = new Thickness(6),
            Child = qr,
        };
        stub.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
            Child = qrCard,
        });

        stub.Add(Tb("SECTION", 7, Mute, bold: true, bottom: 2, align: TextAlignment.Center));
        stub.Add(Tb("GA - STANDING", 12, Cream, bold: true, bottom: 8, align: TextAlignment.Center));
        stub.Add(Tb("TICKET", 7, Mute, bold: true, bottom: 2, align: TextAlignment.Center));
        stub.Add(Tb("ASF26-4471-XK", 10, Cream, bold: true, align: TextAlignment.Center));

        Container stubBox = new()
        {
            BackgroundColor = Stub,
            Height = 300,
            Padding = new Thickness(16, 18, 16, 18),
            Border = Border.LeftOnly(1),
            BorderColors = BorderColors.LeftOnly(Accent),
            Child = stub,
        };

        // ── Compose body | stub ───────────────────────────────────────────────
        Grid ticket = new(2.5f, 1.1f) { Border = Border.None, CellPadding = 0 };
        ticket.AddRow(body, stubBox);
        doc.Add(ticket);

        return doc;
    }

    private static VerticalStack Field(string label, string value)
    {
        VerticalStack v = new();
        v.Add(Tb(label, 7, Mute, bold: true, bottom: 2));
        v.Add(Tb(value, 11, Cream, bold: true));
        return v;
    }

    private static TextBlock Tb(string content, float size, Color color, bool bold = false,
        TextAlignment? align = null, float bottom = 0)
    {
        TextBlock t = new()
        {
            Text = content,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
        };
        if (align.HasValue) t.Alignment = align.Value;
        if (bottom > 0) t.Margin = new Thickness(0, 0, 0, bottom);
        return t;
    }
}
