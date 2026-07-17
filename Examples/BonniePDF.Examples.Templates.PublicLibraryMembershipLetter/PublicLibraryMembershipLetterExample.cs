using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.PublicLibraryMembershipLetter;

/// <summary>
/// Fictional UK public-library membership confirmation with a local-service
/// masthead, membership details, borrowing limits, branch opening hours, and an
/// alternative-formats notice. It deliberately avoids real government branding.
/// </summary>
[System.ComponentModel.DisplayName("Public Library Membership Letter")]
[System.ComponentModel.Category("Community")]
[System.ComponentModel.Description("Fictional UK public-library membership confirmation with local-service masthead, membership details, borrowing limits, branch opening hours, and an alternative-formats notice. PDF/A-2a + PDF/UA-1.")]
public static class PublicLibraryMembershipLetterExample
{
    private const float Margin = 50;

    private static readonly Color LibraryInk = Color.FromHex("#172033");
    private static readonly Color LibraryGreen = Color.FromHex("#0f766e");
    private static readonly Color LibrarySecondary = Color.FromHex("#526071");
    private static readonly Color LibraryMidGrey = Color.FromHex("#cbd5e1");
    private static readonly Color LibraryLight = Color.FromHex("#f1f5f9");

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

    public static Document Generate()
    {
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
                Text = $"Northbridge Libraries  -  Ref NBL-4471-208823  -  Page {pageNumber}",
                FontName = StandardFonts.Helvetica,
                FontSize = 8,
                TextColor = LibrarySecondary,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(LibraryInk);

        Document doc = new(settings);
        doc.Metadata.Title = "Public library membership confirmation";
        doc.Metadata.Author = "Northbridge Libraries";
        doc.Metadata.Subject = "Public library membership confirmation with borrowing limits and branch hours (fictional).";

        doc.Add(Masthead());
        doc.Add(ServiceHeader());
        doc.Add(AddressBlock(longDate));
        doc.Add(H1("Your public library membership"));
        doc.Add(Body(
            $"Welcome to Northbridge Libraries. Your membership is active from {longDate} " +
            $"and your first card renewal date is {renewalDate}. You can use your card at " +
            "any branch, online catalogue terminal, or through the library app."));

        doc.Add(StatusCallout());
        doc.Add(H2("What you can borrow"));
        doc.Add(InfoTable(Membership, LibraryInk, "Service", "Limit", "Notes"));
        doc.Add(H2("Branch opening hours"));
        doc.Add(InfoTable(OpeningHours, LibraryGreen, "Days", "Hours", "Availability"));

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
            TextColor = LibrarySecondary,
            Margin = new Thickness(0, 18, 0, 0),
        });
    }

    private static Container Masthead() => new()
    {
        BackgroundColor = LibraryInk,
        Padding = new Thickness(14, 10, 14, 10),
        Margin = new Thickness(0, 0, 0, 6),
        Child = new TextBlock
        {
            Text = "Northbridge Libraries",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            TextColor = Color.White,
        },
    };

    private static VerticalStack ServiceHeader()
    {
        VerticalStack stack = new();
        stack.AddChild(new TextBlock
        {
            Text = "Membership Services",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            TextColor = LibraryInk,
            Margin = new Thickness(0, 0, 0, 2),
        });
        stack.AddChild(new Container
        {
            BackgroundColor = LibraryGreen,
            Padding = new Thickness(0, 1.5f, 0, 0),
            Margin = new Thickness(0, 0, 0, 20),
            Child = new TextBlock { Text = " ", FontSize = 1 },
        });
        return stack;
    }

    private static Grid AddressBlock(string longDate)
    {
        Grid top = new(1, 1)
        {
            CellPadding = 0,
            Border = Border.None,
            Margin = new Thickness(0, 0, 0, 24),
        };

        VerticalStack addr = new();
        foreach (string line in new[] { "Ms J. Okoro", "14 Elmfield Court", "Northbridge", "NB1 6QT" })
        {
            addr.AddChild(Body(line, mb: 0));
        }

        VerticalStack meta = new();
        meta.AddChild(Body(longDate, align: TextAlignment.Right, mb: 0));
        meta.AddChild(Body("Membership no. NBL-4471-208823", align: TextAlignment.Right, color: LibrarySecondary, size: 10, mb: 0));

        top.AddRow(addr, meta);
        return top;
    }

    private static Container StatusCallout()
    {
        VerticalStack stack = new();
        stack.AddChild(new TextBlock { Text = "Membership status", FontSize = 11, TextColor = LibrarySecondary });
        stack.AddChild(new TextBlock
        {
            Text = "Active",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 28,
            TextColor = LibraryInk,
            Margin = new Thickness(0, 2, 0, 2),
        });
        stack.AddChild(new TextBlock
        {
            Text = "Borrow, renew, and reserve items from today",
            FontSize = 11,
            TextColor = LibrarySecondary,
        });

        return new Container
        {
            BackgroundColor = LibraryLight,
            Border = Border.LeftOnly(5),
            BorderColors = BorderColors.LeftOnly(LibraryGreen),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 8, 0, 22),
            Child = stack,
        };
    }

    private static Table InfoTable(InfoLine[] rows, Color headerColor, string first, string second, string third)
    {
        Table table = new(2.2f, 1, 2.2f)
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
                Td(row.Value, TextAlignment.Left, LibraryInk, bold: string.Equals(first, "Days", StringComparison.Ordinal)),
                Td(row.Note, TextAlignment.Left, LibrarySecondary));
        }

        return table;
    }

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 22,
        TextColor = LibraryInk,
        Margin = new Thickness(0, 0, 0, 12),
    };

    private static Heading H2(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 15,
        TextColor = LibraryInk,
        Margin = new Thickness(0, 6, 0, 8),
    };

    private static TextBlock Body(string text, TextAlignment? align = null, Color? color = null, float size = 11, float mb = 12)
    {
        TextBlock block = new()
        {
            Text = text,
            FontName = StandardFonts.Helvetica,
            FontSize = size,
            TextColor = color ?? LibraryInk,
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
        TextColor = Color.White,
        Alignment = TextAlignment.Left,
    };

    private static TextBlock Td(string text, TextAlignment align, Color? color = null, bool bold = false) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 11,
        TextColor = color ?? LibraryInk,
        Alignment = align,
    };

    private sealed record InfoLine(string Label, string Value, string Note);
}
