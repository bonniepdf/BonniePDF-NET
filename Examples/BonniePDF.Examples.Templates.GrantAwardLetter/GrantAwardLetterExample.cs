using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.GrantAwardLetter;

/// <summary>
/// Fictional public-sector grant funding decision letter: a fund masthead, a
/// headline "Awarded" decision block with key dates, a tagged award-breakdown
/// table, next steps, a review route, contact options, and an alternative-formats
/// notice. Bookmarked, accessibility-first PDF/A-2a + PDF/UA-1. It deliberately
/// avoids real funder branding and states plainly that it is not an official
/// record and not financial advice.
/// </summary>
[System.ComponentModel.DisplayName("Grant Award Letter")]
[System.ComponentModel.Category("Community")]
[System.ComponentModel.Description("Fictional public-sector grant funding decision with an \"Awarded\" decision block, an award-breakdown table, key dates, next steps, a review route, contact options, and an alternative-formats notice. PDF/A-2a + PDF/UA-1.")]
public static class GrantAwardLetterExample
{
    private const float Margin = 50;

    private static readonly Color Ink = Color.FromHex("#1b2a3a");
    private static readonly Color BodyText = Color.FromHex("#2f4858");
    private static readonly Color Muted = Color.FromHex("#5d6f7c");
    private static readonly Color Brand = Color.FromHex("#2b4c7e");
    private static readonly Color BrandDark = Color.FromHex("#1f3a63");
    private static readonly Color Pale = Color.FromHex("#eef2f8");
    private static readonly Color MidGrey = Color.FromHex("#cbd5e1");
    private static readonly Color Award = Color.FromHex("#0f766e");
    private static readonly Color Notice = Color.FromHex("#fff4ce");
    private static readonly Color NoticeBorder = Color.FromHex("#d69e00");

    // Fictional award breakdown. The components add up to the total award, which
    // is then split into equal instalments.
    private static readonly Money[] Breakdown =
    [
        new("Core project grant", "£6,000", "Delivery of the agreed project plan"),
        new("Equipment supplement", "£1,500", "One-off capital items"),
        new("Volunteer expenses", "£400", "Travel and refreshments"),
    ];

    private const string AwardTotal = "£7,900";
    private const string InstalmentAmount = "£3,950";

    private static readonly Detail[] DecisionDetails =
    [
        new("Decision", "Awarded"),
        new("Grant", "Community Project Grant"),
        new("Award period", "1 Sep 2026 to 31 Aug 2027"),
        new("First instalment", "Friday 26 September 2026"),
    ];

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, Margin)
        {
            PageColor = Color.White,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DisplayDocTitle = true,
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"SAMPLE DOCUMENT - FICTIONAL DATA  |  Not issued by any funding body  |  Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 7.5f,
                Color = Muted,
                Alignment = TextAlignment.Center,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(BodyText);

        Document doc = new(settings);
        doc.Metadata.Title = "Grant award decision letter";
        doc.Metadata.Author = "Sample Community Fund";
        doc.Metadata.Subject = "Fictional community grant award with award breakdown, payment schedule, next steps, and review route.";
        doc.Metadata.Keywords = "sample, public sector, grant, award, funding, accessibility, fictional data";

        doc.Add(Masthead());
        doc.Add(ServiceHeader());
        doc.Add(AddressBlock());

        doc.Add(H1("Decision on your grant application", "decision"));
        doc.Add(Body("Dear Sample Recipient,", bold: true, mb: 8));
        doc.Add(Body(
            "We are pleased to tell you that your application to the Sample Community Fund has been " +
            "successful. This letter explains what we have awarded, how your grant is paid, when it " +
            "starts, and what to do next."));

        doc.Add(DecisionCallout());

        doc.Add(H2("How your grant is made up", "breakdown"));
        doc.Add(BreakdownTable());
        doc.Add(Body(
            $"Your grant is paid in two instalments of {InstalmentAmount}. The first instalment is " +
            "released once we receive your signed grant agreement.",
            color: Muted, size: 10.5f, mb: 22));

        doc.Add(H2("What happens next", "next-steps"));
        doc.Add(NextSteps());

        AddClosingSections(doc);

        return doc;
    }

    private static void AddClosingSections(Document doc)
    {
        doc.Add(H2("If you disagree with this decision", "review"));
        doc.Add(Body(
            "If you think this decision is wrong, or you would like us to reconsider the amount awarded, " +
            "you can ask us to review it. Contact us within one month of the date of this letter and " +
            "explain why. If you still disagree after the review, you can ask the Fund’s independent " +
            "panel to look at your case."));

        doc.Add(TimeLimitNotice());

        doc.Add(H2("How to contact us", "contact"));
        doc.Add(Body(
            "For this fictional example, contact the Grants & Awards Service on 0300 123 0000 " +
            "(textphone 0300 123 0001) or write to us at the address on this letter. Tell us if " +
            "you need this letter in large print, audio, easy read, Braille, or another language."));

        doc.Add(Disclaimer());
    }

    private static Container Masthead() => new()
    {
        BackgroundColor = Ink,
        Padding = new Thickness(14, 10, 14, 10),
        Margin = new Thickness(0, 0, 0, 6),
        Child = new TextBlock
        {
            Text = "Sample Community Fund",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            Color = Color.White,
        },
    };

    private static VerticalStack ServiceHeader()
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock
        {
            Text = "Grants & Awards Service",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            Color = Ink,
            Margin = new Thickness(0, 0, 0, 2),
        });
        stack.Add(new Container
        {
            BackgroundColor = Brand,
            Height = 1.5f,
            Margin = new Thickness(0, 0, 0, 20),
        });
        return stack;
    }

    private static Grid AddressBlock()
    {
        Grid top = new(1, 1)
        {
            CellPadding = 0,
            Border = Border.None,
            Margin = new Thickness(0, 0, 0, 24),
        };

        // One TextBlock with hard line breaks: the address tags as a single
        // paragraph under PDF/UA instead of five.
        TextBlock addr = Body("Sample Recipient\nSample Community Project\n14 Sample Walk\nSample Town\nST3 2LP", mb: 0);

        VerticalStack meta = new();
        meta.Add(Body("Issued 4 August 2026", align: TextAlignment.Right, mb: 0));
        meta.Add(Body("Reference SAMPLE-GRANT-2026-00417", align: TextAlignment.Right, color: Muted, size: 10, mb: 0));

        top.AddRow(addr, meta);
        return top;
    }

    private static Container DecisionCallout()
    {
        Grid details = new(1, 1) { CellPadding = 5 };
        details.AddRow(DetailStack(DecisionDetails[0]), DetailStack(DecisionDetails[1]));
        details.AddRow(DetailStack(DecisionDetails[2]), DetailStack(DecisionDetails[3]));

        VerticalStack stack = new();
        stack.Add(new TextBlock { Text = "Our decision", FontSize = 11, Color = Muted });
        stack.Add(new TextBlock
        {
            Text = "Awarded",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 28,
            Color = Award,
            Margin = new Thickness(0, 2, 0, 10),
        });
        stack.Add(details);

        return new Container
        {
            BackgroundColor = Pale,
            Border = Border.LeftOnly(5),
            BorderColors = BorderColors.LeftOnly(Award),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 8, 0, 22),
            Child = stack,
        };
    }

    private static Table BreakdownTable()
    {
        Table calc = new(2.6f, 1, 2.2f)
        {
            CellPadding = 9,
            Border = new Border(0.75f),
            BorderColors = new BorderColors(MidGrey),
            HeaderBackgroundColor = Ink,
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 4, 0, 8),
        };
        calc.AddHeader(Th("Component"), Th("Amount", TextAlignment.Right), Th("Notes"));
        foreach (Money line in Breakdown)
        {
            calc.AddRow(Td(line.Label), Td(line.Amount, Ink, TextAlignment.Right), Td(line.Note, Muted));
        }

        calc.AddRow(
            Td("Total award", Ink, TextAlignment.Left, bold: true),
            Td(AwardTotal, Ink, TextAlignment.Right, bold: true),
            Td("Paid in two instalments", Muted));
        return calc;
    }

    private static UnorderedList NextSteps()
    {
        UnorderedList list = new()
        {
            FontName = StandardFonts.Helvetica,
            FontSize = 10.5f,
            Color = BodyText,
            BulletColor = Brand,
            ItemSpacing = 4,
            Margin = new Thickness(0, 0, 0, 22),
        };
        list.AddItem("Sign and return the enclosed grant agreement within one month to release your first instalment.");
        list.AddItem("Send us your project bank account details so we can pay the grant.");
        list.AddItem("Tell us as soon as possible if anything about your project changes during the award period.");
        list.AddItem("Keep this letter for your records.");
        return list;
    }

    private static Container TimeLimitNotice()
    {
        VerticalStack stack = new() { Spacing = 5 };
        stack.Add(new TextBlock
        {
            Text = "Time limit",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 11,
            Color = Ink,
        });
        stack.Add(new TextBlock
        {
            Text = "Return your signed grant agreement within one month of the date of this letter to " +
                   "release your first instalment. This is a fictional sample and does not provide legal " +
                   "or financial advice.",
            FontSize = 10,
            Color = BodyText,
        });

        return new Container
        {
            BackgroundColor = Notice,
            Border = new Border(1),
            BorderColors = new BorderColors(NoticeBorder),
            Padding = new Thickness(13),
            Margin = new Thickness(0, 0, 0, 22),
            Child = stack,
        };
    }

    private static Container Disclaimer() => new()
    {
        BackgroundColor = Color.FromHex("#f4f7f9"),
        Padding = new Thickness(11),
        Child = new TextBlock
        {
            Text = "Demonstration document generated by BonniePDF. All people, identifiers, organisations, " +
                   "amounts, locations, and contact details are fictional. Not issued by any funding body " +
                   "and not an official record.",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 8.5f,
            Color = Muted,
            Alignment = TextAlignment.Center,
        },
    };

    private static Heading H1(string text, string tocIdentifier) => new(text)
    {
        TocIdentifier = tocIdentifier,
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 22,
        Color = Ink,
        Margin = new Thickness(0, 0, 0, 12),
    };

    private static Heading H2(string text, string tocIdentifier) => new(text)
    {
        TocIdentifier = tocIdentifier,
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 15,
        Color = Ink,
        Margin = new Thickness(0, 6, 0, 8),
    };

    private static TextBlock Body(string text, TextAlignment? align = null, Color? color = null, float size = 11, float mb = 12, bool bold = false)
    {
        TextBlock block = new()
        {
            Text = text,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color ?? BodyText,
            Margin = new Thickness(0, 0, 0, mb),
        };
        if (align.HasValue)
        {
            block.Alignment = align.Value;
        }

        return block;
    }

    private static VerticalStack DetailStack(Detail item)
    {
        VerticalStack stack = new() { Spacing = 3 };
        stack.Add(new TextBlock
        {
            Text = item.Label.ToUpperInvariant(),
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            Color = BrandDark,
        });
        stack.Add(new TextBlock
        {
            Text = item.Value,
            FontName = StandardFonts.Helvetica,
            FontSize = 10.5f,
            Color = Ink,
        });
        return stack;
    }

    private static TextBlock Th(string text, TextAlignment align = TextAlignment.Left) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        Color = Color.White,
        Alignment = align,
    };

    private static TextBlock Td(string text, Color? color = null, TextAlignment align = TextAlignment.Left, bool bold = false) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 11,
        Color = color ?? Ink,
        Alignment = align,
    };

    private sealed record Money(string Label, string Amount, string Note);

    private sealed record Detail(string Label, string Value);
}
