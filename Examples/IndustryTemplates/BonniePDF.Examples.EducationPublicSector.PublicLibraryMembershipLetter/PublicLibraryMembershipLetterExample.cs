using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.EducationPublicSector.PublicLibraryMembershipLetter;

/// <summary>
/// Fictional UK public-library membership confirmation with a teal library
/// masthead, the member's library card with a scannable number, borrowing limits,
/// branch opening hours, and an alternative-formats notice. It deliberately avoids
/// real government branding.
/// </summary>
[System.ComponentModel.DisplayName("Public Library Membership")]
[System.ComponentModel.Category("Education & Public Sector")]
[System.ComponentModel.Description(
    "Fictional UK public-library membership confirmation with a teal library masthead, the member's library card with a scannable number, borrowing limits, branch opening hours, and an alternative-formats notice. PDF/A-2a + PDF/UA-1.")]
public static class PublicLibraryMembershipLetterExample
{
    private const float Margin = 50;

    private static readonly Color LibraryInk = Color.FromHex("#172033");
    private static readonly Color LibraryGreen = Color.FromHex("#0f766e");
    private static readonly Color LibrarySecondary = Color.FromHex("#526071");
    private static readonly Color LibraryMidGrey = Color.FromHex("#cbd5e1");
    private static readonly Color LibraryLight = Color.FromHex("#f1f5f9");
    // Teal-100: the card's small print, 7.3:1 against the card's teal.
    private static readonly Color CardTint = Color.FromHex("#ccfbf1");

    private static readonly InfoLine[] Membership =
    [
        new("Books", "12 items", "3 week loan, renewable up to 4 times"),
        new("Audiobooks", "6 items", "3 week loan"),
        new("E-books and e-audiobooks", "10 items", "Available through the library app"),
        new("Study rooms", "2 hours/day", "Book online or at any branch desk"),
    ];

    private static readonly InfoLine[] OpeningHours =
    [
        new("Monday to Thursday", "09:00-19:00", "Main library and neighbourhood branches"),
        new("Friday", "09:00-18:00", "Main library and neighbourhood branches"),
        new("Saturday", "10:00-16:00", "Main library only"),
    ];

    private static readonly float[] MembershipColumns = [2.5f, 1f, 2.7f];
    private static readonly float[] HoursColumns = [1.3f, 1f, 2.2f];

    /// <summary>Asset paths required by this example, relative to the shared <c>Examples/assets</c> directory.</summary>
    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "images/svg/borough-crest.svg",
    ];

    /// <summary>Builds the document using the default shared assets. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        string crestSvg = System.Text.Encoding.UTF8.GetString(GetAsset(assets, "images/svg/borough-crest.svg"));

        DateTime now = DateTime.Now;
        CultureInfo enGb = CultureInfo.GetCultureInfo("en-GB");
        string longDate = now.ToString("d MMMM yyyy", enGb);
        string renewalDate = now.AddYears(1).ToString("d MMMM yyyy", enGb);

        DocumentSettings settings = new(PdfSize.A4, Margin)
        {
            PageColor = Color.White,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Sample Libraries  -  Ref NBL-4471-208823  -  Page {pageNumber}",
                FontName = StandardFonts.Helvetica,
                FontSize = 8,
                Color = LibrarySecondary,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(LibraryInk);

        Document doc = new(settings);
        doc.Metadata.Title = "Public library membership confirmation";
        doc.Metadata.Author = "Sample Libraries";
        doc.Metadata.Subject =
            "Public library membership confirmation with borrowing limits and branch hours (fictional).";

        doc.Add(Masthead(crestSvg));
        doc.Add(AddressBlock(longDate));
        doc.Add(H1("Your public library membership"));
        doc.Add(Body(
            $"Welcome to Sample Libraries. Your membership is active from {longDate} " +
            $"and your first card renewal date is {renewalDate}. You can use your card at " +
            "any branch, online catalogue terminal, or through the library app."));

        // The member's card beside their status: the number on the card is the one the branch scans.
        HorizontalStack cardRow = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 24, Margin = new Thickness(0, 8, 0, 22) };
        cardRow.Add(LibraryCard(renewalDate), 1.1f);
        cardRow.Add(StatusCallout(), 1);
        doc.Add(cardRow);

        VerticalStack borrowStack = new() { BreakAllowed = false };
        borrowStack.Add(H2("What you can borrow"));
        borrowStack.Add(InfoTable(Membership, MembershipColumns, LibraryGreen, "Service", "Limit", "Notes"));
        doc.Add(borrowStack);

        VerticalStack hoursStack = new() { BreakAllowed = false };
        hoursStack.Add(H2("Branch opening hours"));
        Table openingHours = InfoTable(OpeningHours, HoursColumns, LibraryGreen, "Days", "Hours", "Availability");
        openingHours.CellPadding = 8;
        hoursStack.Add(openingHours);
        doc.Add(hoursStack);

        AddHelpSections(doc);

        return doc;
    }

    private static void AddHelpSections(Document doc)
    {
        doc.Add(H2("Using your membership"));
        doc.Add(Body(
            "Bring photo ID to any branch desk to collect your library card. You can reserve " +
            "items online, renew loans before their due date, and update your contact details " +
            "through your library account."));

        doc.Add(H2("Lost cards and help"));
        doc.Add(Body(
            "If your card is lost, contact the library so we can pause borrowing on your account. " +
            "For help with renewals, reservations, or digital services, call 020 7946 0842 or " +
            "visit northbridge.example/library."));

        doc.Add(new TextBlock
        {
            Text = "This document is available in Braille, large print, audio and other formats. " +
                   "Contact Membership Services to request an alternative format.",
            FontSize = 9.5f,
            Color = LibrarySecondary,
            Margin = new Thickness(0, 18, 0, 0),
        });
    }

    /// <summary>The library's own masthead: its name and the service writing, on the library's teal.</summary>
    private static Container Masthead(string crestSvg)
    {
        HorizontalStack brandRow = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 10 };
        brandRow.Add(new Svg
        {
            SvgData = crestSvg,
            Width = 28,
            Height = 34,
            AltText = "Sample Borough Council crest",
        });
        brandRow.Add(new TextBlock
        {
            Text = "Sample Libraries",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            Color = Color.White,
            Margin = new Thickness(0, 6, 0, 0),
        });

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Equal };
        row.Add(brandRow);
        row.Add(new TextBlock
        {
            Text = "Membership Services",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 11,
            Color = CardTint,
            Alignment = TextAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0),
        });
        return new Container
        {
            BackgroundColor = LibraryGreen,
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 20),
            Child = row,
        };
    }

    /// <summary>The member's library card: name, renewal date and the barcode the branch scans.</summary>
    private static Container LibraryCard(string renewalDate)
    {
        VerticalStack stack = new() { Spacing = 3 };
        stack.Add(new TextBlock
        {
            Text = "SAMPLE LIBRARIES  ·  LIBRARY CARD",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 7.5f,
            LetterSpacing = 1.2f,
            Color = CardTint,
        });
        stack.Add(new TextBlock
        {
            Text = "Sample Member",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = Color.White,
            Margin = new Thickness(0, 6, 0, 0),
        });
        stack.Add(new TextBlock
        {
            Text = $"Valid until {renewalDate}",
            FontSize = 9,
            Color = CardTint,
            Margin = new Thickness(0, 0, 0, 8),
        });
        stack.Add(new Container
        {
            BackgroundColor = Color.White,
            CornerRadius = 6,
            Padding = new Thickness(10, 8, 10, 8),
            Child = new Barcode
            {
                Value = "NBL-4471-208823",
                Symbology = BarcodeSymbology.Code128,
                ModuleWidth = 1,
                Height = 34,
                AltText = "Barcode of membership number NBL-4471-208823.",
            },
        });
        return new Container
        {
            BackgroundColor = LibraryGreen,
            CornerRadius = 10,
            Padding = new Thickness(18, 14, 18, 14),
            Child = stack,
        };
    }

    private static Grid AddressBlock(string longDate)
    {
        Grid top = new(1, 1)
        {
            CellPadding = 0,
            Border = Border.None,
            Margin = new Thickness(0, 0, 0, 24),
        };

        // One TextBlock with hard line breaks: the address tags as a single
        // paragraph under PDF/UA instead of four.
        TextBlock addr = Body("Sample Member\n14 Sample Court\nSample Town\nST1 6QT", mb: 0);

        VerticalStack meta = new();
        meta.Add(Body(longDate, align: TextAlignment.Right, mb: 0));
        meta.Add(Body("Membership no. NBL-4471-208823", align: TextAlignment.Right, color: LibrarySecondary, size: 10,
            mb: 0));

        top.AddRow(addr, meta);
        return top;
    }

    private static Container StatusCallout()
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock { Text = "Membership status", FontSize = 11, Color = LibrarySecondary });
        stack.Add(new TextBlock
        {
            Text = "Active",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 28,
            Color = LibraryInk,
            Margin = new Thickness(0, 2, 0, 2),
        });
        stack.Add(new TextBlock
        {
            Text = "Borrow, renew, and reserve items from today. Bring photo ID to collect your card.",
            FontSize = 11,
            Color = LibrarySecondary,
        });

        return new Container
        {
            BackgroundColor = LibraryLight,
            Border = Border.LeftOnly(5),
            BorderColors = BorderColors.LeftOnly(LibraryGreen),
            Padding = new Thickness(16, 14, 16, 14),
            Child = stack,
        };
    }

    private static Table InfoTable(InfoLine[] rows, float[] columns, Color headerColor, string first, string second,
        string third)
    {
        Table table = new(columns)
        {
            CellPadding = 9,
            Border = new Border(0.75f),
            BorderColors = new BorderColors(LibraryMidGrey),
            HeaderBackgroundColor = headerColor,
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 4, 0, 22),
        };
        table.AddHeader(Th(first), Th(second), Th(third));

        foreach (InfoLine row in rows)
        {
            table.AddRow(
                Td(row.Label, TextAlignment.Left),
                Td(row.Value, TextAlignment.Left, LibraryInk,
                    bold: string.Equals(first, "Days", StringComparison.Ordinal)),
                Td(row.Note, TextAlignment.Left, LibrarySecondary));
        }

        return table;
    }

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 22,
        Color = LibraryInk,
        Margin = new Thickness(0, 0, 0, 12),
    };

    private static Heading H2(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 15,
        Color = LibraryInk,
        Margin = new Thickness(0, 6, 0, 8),
    };

    private static TextBlock Body(string text, TextAlignment? align = null, Color? color = null, float size = 11,
        float mb = 12)
    {
        TextBlock block = new()
        {
            Text = text,
            FontName = StandardFonts.Helvetica,
            FontSize = size,
            Color = color ?? LibraryInk,
            Margin = new Thickness(0, 0, 0, mb),
        };
        if (align.HasValue)
        {
            block.Alignment = align.Value;
        }

        return block;
    }

    private static TextBlock Th(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        Color = Color.White,
        Alignment = TextAlignment.Left,
    };

    private static TextBlock Td(string text, TextAlignment align, Color? color = null, bool bold = false) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 11,
        Color = color ?? LibraryInk,
        Alignment = align,
    };

    private sealed record InfoLine(string Label, string Value, string Note);

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string assetsDir = FindAssetsDir();
        Dictionary<string, byte[]> dict = new(StringComparer.Ordinal);
        foreach (string key in RequiredAssets)
        {
            string fullPath = Path.Combine(assetsDir, key.Replace('/', Path.DirectorySeparatorChar));
            dict[key] = File.ReadAllBytes(fullPath);
        }

        return dict;
    }

    private static byte[] GetAsset(IReadOnlyDictionary<string, byte[]> assets, string key)
    {
        if (assets.TryGetValue(key, out byte[]? bytes))
            return bytes;
        string fullPath = Path.Combine(FindAssetsDir(), key.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllBytes(fullPath);
    }

    private static string FindAssetsDir()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate))
                return candidate;
            string direct = Path.Combine(dir, "assets");
            if (Directory.Exists(direct))
                return direct;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate Examples/assets directory.");
    }
}
