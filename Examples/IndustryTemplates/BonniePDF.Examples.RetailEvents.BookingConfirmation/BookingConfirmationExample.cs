using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.RetailEvents.BookingConfirmation;

/// <summary>
/// The email attachment people screenshot at the airport. It has two readers in
/// one sitting: someone checking the times against what they expected, and a
/// phone that has to open the booking again with no signal for a moment, so the
/// reference is both printed large and encoded as a QR code.
///
/// The detail that makes it a confirmation rather than a receipt: every journey
/// leg is a self-contained card (flight, departure, arrival, duration, cabin,
/// baggage) so a reader can find the one they need without scanning a table, and
/// the price breakdown is summed from its lines rather than typed in, so the
/// total on the page is the total of the rows above it.
///
/// Sample organisations, flights and bookings throughout.
/// </summary>
[System.ComponentModel.DisplayName("Booking confirmation")]
[System.ComponentModel.Category("Retail & Events")]
[System.ComponentModel.Description(
    "A travel booking confirmation: outbound and return flights as segment cards, a hotel stay, travellers with seats, a priced breakdown that adds up, and a QR code to manage the booking. PDF/A-2a + PDF/UA-1.")]
public static class BookingConfirmationExample
{
    private const string LisbonPhotoKey = "images/travel/lisbon.jpg";

    public static IReadOnlyList<string> RequiredAssets { get; } = [LisbonPhotoKey];

    private const string Reference = "BKG-7Q4M2X";
    private const string Destination = "Lisbon";
    private const string ManageUrl = "https://trips.example.com/manage/BKG-7Q4M2X";
    private const string CardEnding = "4821";

    private static readonly DateTime Booked = new(2026, 8, 2);

    private sealed record Passenger(string Name, string Ticket, string SeatOut, string SeatBack);

    private sealed record Airport(string Code, string City, string Terminal, string Time);

    private sealed record FlightLeg(
        string Label,
        DateTime Date,
        string Flight,
        string Carrier,
        Airport From,
        Airport To,
        string Duration,
        string Cabin,
        string Baggage);

    /// <summary>Pence. Everything on the page is added up from these lines.</summary>
    private sealed record PriceLine(string Description, int Pence);

    private static readonly Passenger[] Passengers =
    [
        new("SAMPLE / ALEX MR", "125-2400193847", "14C", "9A"),
        new("SAMPLE / JORDAN MS", "125-2400193848", "14D", "9B"),
    ];

    private static readonly FlightLeg[] Legs =
    [
        new(
            "Outbound",
            new DateTime(2026, 10, 10),
            "SA 412",
            "Sample Air",
            new Airport("SCL", "Sample City", "Terminal 2", "07:40"),
            new Airport("LIS", "Lisbon", "Terminal 1", "10:35"),
            "2 h 55 min",
            "Economy",
            "1 checked bag (23 kg) and 1 cabin bag each"),
        new(
            "Return",
            new DateTime(2026, 10, 17),
            "SA 413",
            "Sample Air",
            new Airport("LIS", "Lisbon", "Terminal 1", "11:25"),
            new Airport("SCL", "Sample City", "Terminal 2", "14:20"),
            "2 h 55 min",
            "Economy",
            "1 checked bag (23 kg) and 1 cabin bag each"),
    ];

    private const string StayName = "Hotel Sample Lisboa";
    private const string StayCheckInFrom = "15:00";
    private const string StayCheckOutBy = "11:00";
    private const string StayRoom = "Double room, city view";
    private const string StayBoard = "Breakfast included";
    private const string StayConfirmation = "HTL-558201";

    private static readonly string[] StayAddress = ["Rua Exemplo 12", "1100-000 Lisbon", "Portugal"];

    private static readonly DateTime StayCheckIn = new(2026, 10, 10);
    private static readonly DateTime StayCheckOut = new(2026, 10, 17);

    private static readonly PriceLine[] PriceLines =
    [
        new("Flights, outbound — 2 adults × £128.00", 25_600),
        new("Flights, return — 2 adults × £119.50", 23_900),
        new("Airport taxes and carrier charges", 13_840),
        new("Hotel Sample Lisboa — 7 nights, breakfast included", 84_350),
        new("Seat selection — 4 seats", 3_800),
    ];

    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        byte[] lisbonPhoto = GetAsset(assets, LisbonPhotoKey);
        int total = PriceLines.Sum(l => l.Pence);
        int nights = (StayCheckOut - StayCheckIn).Days;

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 38 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Booking {Reference} · sample data, not a real booking",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Booking confirmation {Reference} — {Destination}";
        doc.Metadata.Author = "Sample Trips";
        doc.Metadata.Subject =
            "Travel booking confirmation: flights, hotel, passengers, a priced breakdown, and a QR code to manage the booking (sample data).";
        doc.Metadata.Keywords = "booking confirmation, itinerary, flights, hotel, travel, qr code, pdf/ua, pdf/a";

        doc.Add(AccentBar());

        VerticalStack heading = new();
        heading.Add(Cell("BOOKING CONFIRMATION", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        heading.Add(H1($"Your trip to {Destination}"));
        heading.Add(Cell(
            $"{GbDate(StayCheckIn)} to {GbDate(StayCheckOut)} · {nights} nights · {Passengers.Length} adults",
            color: Muted,
            marginBottom: 10f));
        heading.Add(Cell("BOOKING REFERENCE", size: 6.5f, bold: true, color: Muted, marginBottom: 2f));
        heading.Add(Cell(Reference, size: 20f, bold: true, color: Heading));

        Container code = new()
        {
            HorizontalAlignment = HorizontalAlignment.End,
            Child = new QRCode
            {
                Data = ManageUrl,
                Size = 84,
                QuietZone = 1,
                ErrorCorrection = QrErrorCorrection.Quartile,
                AltText = $"QR code that opens booking {Reference} for management",
            },
        };
        HorizontalStack top = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 14,
            Margin = new Thickness(0, 0, 0, 10),
        };
        top.Add(heading, 3);
        top.Add(code, 1);
        doc.Add(top);

        doc.Add(new Container
        {
            Height = 92,
            CornerRadius = 6,
            ClipContent = true,
            Margin = new Thickness(0, 0, 0, 10),
            Child = new Image
            {
                ImageData = lisbonPhoto,
                Height = 92,
                FitMode = ImageFitMode.Cover,
                AltText = "Terracotta rooftops and pastel facades of Lisbon old town at golden hour.",
            },
        });

        doc.Add(Panel(
            "Confirmed",
            [
                Cell(
                    $"Booked on {GbDate(Booked)} and paid in full by card ending {CardEnding}. "
                    + "Keep this confirmation: you will be asked for the reference at check-in and at the hotel.",
                    size: 9f),
            ],
            Positive));

        doc.Add(H2("Your flights"));
        foreach (FlightLeg leg in Legs)
        {
            doc.Add(LegCard(leg));
        }

        doc.Add(H2("Travellers"));
        Table passengers = new(3f, 2.2f, 1.2f, 1.2f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        passengers.AddHeader(Th("Passenger"), Th("E-ticket number"), Th("Seat out"), Th("Seat back"));
        foreach (Passenger p in Passengers)
        {
            passengers.AddRow(Td(p.Name, bold: true), Td(p.Ticket), Td(p.SeatOut), Td(p.SeatBack));
        }

        doc.Add(passengers);

        doc.Add(H2("Your hotel"));
        VerticalStack stay = new();
        stay.Add(Cell(StayName, size: 11f, bold: true, color: Heading, marginBottom: 3f));
        stay.Add(Cell(string.Join('\n', StayAddress), size: 9f, color: Muted));
        doc.Add(WeightedRow(
            [
                stay,
                Field("Check-in", $"{GbDate(StayCheckIn)}, from {StayCheckInFrom}"),
                Field("Check-out", $"{GbDate(StayCheckOut)}, by {StayCheckOutBy}"),
                Field("Room", $"{StayRoom}\n{StayBoard}"),
            ],
            spacing: 14f,
            marginBottom: 4f));
        doc.Add(Cell($"Hotel confirmation number {StayConfirmation}", size: 8.5f, color: Muted, marginBottom: 4f));

        // The price breakdown starts a fresh page so its heading never strands at the foot of the last one.
        doc.Add(new PageBreak());
        doc.Add(H2("What you paid"));
        Table price = new(6f, 1.6f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        price.AddHeader(Th("Item"), Th("Amount", TextAlignment.Right));
        foreach (PriceLine line in PriceLines)
        {
            price.AddRow(Td(line.Description), Td(Money(line.Pence), align: TextAlignment.Right));
        }

        price.AddRow(Td("Total paid", bold: true), Td(Money(total), bold: true, align: TextAlignment.Right));
        doc.Add(price);

        doc.Add(Panel("Before you travel",
        [
            Cell(
                "Check in online from 48 hours before departure and arrive at the airport two hours ahead. Every traveller "
                + "needs a passport valid for the whole stay. The hotel will ask for a card as a deposit at check-in; it is "
                + "released when you check out.",
                size: 9f),
        ]));
        doc.Add(Cell($"To change or cancel, scan the code above or visit {ManageUrl}.", size: 8.5f, color: Muted));

        return doc;
    }

    /// <summary>One journey leg as a self-contained card.</summary>
    private static Container LegCard(FlightLeg leg)
    {
        HorizontalStack route = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 8),
        };
        route.Add(Endpoint("Departs", leg.From), 2);

        VerticalStack middle = new();
        middle.Add(Cell("DURATION", size: 6.5f, bold: true, color: Muted, align: TextAlignment.Center, marginBottom: 2f));
        middle.Add(Cell(leg.Duration, size: 9.5f, align: TextAlignment.Center));
        middle.Add(Cell($"{leg.Flight} · {leg.Cabin}", size: 8f, color: Muted, align: TextAlignment.Center, marginTop: 2f));
        route.Add(middle, 2);
        route.Add(Endpoint("Arrives", leg.To), 2);

        VerticalStack body = new();
        body.Add(Cell(
            $"{leg.Label.ToUpperInvariant()} · {GbDate(leg.Date)} · {leg.Carrier}",
            size: 8.5f,
            bold: true,
            color: Accent,
            marginBottom: 6f));
        body.Add(route);
        body.Add(Cell($"Baggage: {leg.Baggage}", size: 8.5f, color: Muted));

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(9),
            Margin = new Thickness(0, 0, 0, 8),
            Child = body,
        };
    }

    private static VerticalStack Endpoint(string role, Airport airport)
    {
        VerticalStack stack = new();
        stack.Add(Cell(role.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 2f));
        stack.Add(Cell(airport.Time, size: 17f, bold: true, color: Heading, marginBottom: 1f));
        stack.Add(Cell($"{airport.City} ({airport.Code})", size: 9f, marginBottom: 1f));
        stack.Add(Cell(airport.Terminal, size: 8f, color: Muted));
        return stack;
    }

    /// <summary>Pence as pounds: £1,234.56.</summary>
    private static string Money(int pence) => NumberFormatter.Format(pence / 100m, "N2", "£");

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Blue[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Color Positive = ColorPalette.Emerald[700];

    /// <summary>A styled text cell with document defaults.</summary>
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
        Margin = new Thickness(0, 10, 0, 6),
    };

    /// <summary>The full-width accent rule at the top of each document.</summary>
    private static Container AccentBar(Color? color = null, float height = 4f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 10),
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

    /// <summary>A bordered panel with a heading - notes and declarations.</summary>
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
            Padding = new Thickness(9),
            Margin = new Thickness(0, 2, 0, 8),
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

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string dir = FindAssetsDir();
        return RequiredAssets.ToDictionary(key => key, key => File.ReadAllBytes(Path.Combine(dir, key)), StringComparer.Ordinal);
    }

    private static byte[] GetAsset(IReadOnlyDictionary<string, byte[]> assets, string key)
    {
        if (assets.TryGetValue(key, out byte[]? bytes)) return bytes;
        string candidate = Path.Combine(FindAssetsDir(), key);
        if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
        throw new InvalidOperationException($"Supply {key}.");
    }

    private static string FindAssetsDir()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate)) return candidate;
            string local = Path.Combine(dir, "assets");
            if (Directory.Exists(local)) return local;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException("Could not locate Examples/assets directory.");
    }
}
