using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>A supply agreement whose clause headings, signature panel and checklist break across pages the way a contract should.</summary>
public static class PageBreakControlExample
{
    private static readonly (string Title, string Text)[] Clauses =
    [
        ("1. Parties and term",
            "This agreement is between Northfield Logistics Ltd (the Customer) and Calder Print & Pack Ltd (the Supplier). It starts on 1 December 2026 and runs for two years, then continues year to year until either party gives three months’ written notice."),
        ("2. Services",
            "The Supplier will print, fold and pack branded delivery cartons to the specifications in Schedule 1, and deliver them to the Customer’s depots on the agreed weekly schedule. The Supplier may not subcontract printing without the Customer’s written consent."),
        ("3. Prices and payment",
            "Prices are fixed for the first twelve months as set out in the price list. After that, the Supplier may increase prices once a year by no more than the change in the Consumer Prices Index over the previous twelve months. The Customer will pay undisputed invoices within 30 days of receipt."),
        ("4. Service levels",
            "The Supplier will deliver at least 98% of orders complete and on the agreed day in each calendar month. If it misses that level in two consecutive months, the Customer may withhold 5% of the next month’s invoice until the level is restored."),
        ("5. Liability",
            "Neither party limits its liability for death or personal injury caused by negligence, or for fraud. Otherwise, each party’s total liability in any contract year is limited to the charges paid or payable in that year, and neither is liable for loss of profit or indirect loss."),
        ("6. Termination",
            "Either party may end this agreement immediately by written notice if the other commits a material breach that it does not put right within 30 days of being asked to, or if the other becomes insolvent. Clauses 5 and 6 survive termination."),
    ];

    private static readonly (string Role, string Name, string Email)[] Contacts =
    [
        ("Orders and schedules", "Priya Shah", "orders@calder.example"),
        ("Quality and artwork", "Tom Ellery", "quality@calder.example"),
        ("Accounts", "Ruth Okafor", "accounts@calder.example"),
    ];

    private static readonly string[] Checklist =
    [
        "Artwork files approved by both parties",
        "Carton sizes confirmed for each depot",
        "Delivery days agreed for each depot",
        "Pallet labels match the depot receiving system",
        "First production run sampled and signed off",
        "Price list attached and initialled",
        "Escalation contacts exchanged",
        "Insurance certificates received",
        "Supplier added to the purchase-order system",
        "Payment details verified by call-back",
        "Monthly service report template agreed",
        "Business continuity plan received",
        "Data-sharing terms reviewed by both legal teams",
        "Sustainability statement received",
        "Waste and recycling arrangements agreed",
        "Health and safety induction booked for site visits",
        "Depot access passes issued",
        "First quarterly review date set",
        "Carton stock levels agreed for each depot",
        "Minimum order quantities confirmed",
        "Lead times for urgent orders agreed",
        "Returns process for damaged cartons agreed",
        "Print colour standards signed off",
        "Out-of-hours contact numbers exchanged",
        "Invoice format and purchase-order references agreed",
        "Handover meeting minutes circulated",
    ];

    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings
        {
            PageSize = PdfSize.A5,
            Margin = 36,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(ColorPalette.Slate[700]),
            FooterTemplate = PageTemplate.FromElements(@default: new TextBlock("Supply agreement SA-2026-118  ·  page {{CurrentPage}} of {{TotalPages}}")
            {
                FontSize = 7.5f,
                Color = ColorPalette.Slate[500],
                Alignment = TextAlignment.Center
            })
        })
        { Metadata = new DocumentMetadata { Title = "Supply agreement SA-2026-118" } };

        doc.Add(new TextBlock("SUPPLY AGREEMENT")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            LetterSpacing = 1.5f,
            Color = ColorPalette.Teal[700],
            Margin = new Thickness(0, 0, 0, 4)
        });
        doc.Add(new Heading("Northfield Logistics and Calder Print & Pack")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 4)
        });
        doc.Add(new TextBlock("Reference SA-2026-118  ·  dated 14 November 2026")
        {
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 14)
        });

        // EnsureSpace starts a clause on the next page unless there is room for its
        // heading and the first lines of its text, so no heading is stranded at the foot of a page.
        foreach (var (title, text) in Clauses)
        {
            doc.Add(new EnsureSpace
            {
                MinHeight = 70,
                Child = new Heading(title)
                {
                    Level = 2,
                    FontName = StandardFonts.HelveticaBold,
                    FontSize = 11,
                    Color = ColorPalette.Slate[900],
                    Margin = new Thickness(0, 8, 0, 4)
                }
            });
            doc.Add(new TextBlock(text) { LineHeight = 1.45f });
        }

        // PreventPageBreak moves the whole signature panel to the next page if it does not fit.
        HorizontalStack signatories = new() { Spacing = 16, WidthDistribution = WidthDistribution.Equal };
        signatories.Add(Signatory("Signed for Northfield Logistics Ltd"));
        signatories.Add(Signatory("Signed for Calder Print & Pack Ltd"));
        VerticalStack signaturePanel = new() { Margin = new Thickness(0, 16, 0, 0), Spacing = 6 };
        signaturePanel.Add(Note("Kept together: this panel moves to the next page as a unit rather than splitting."));
        signaturePanel.Add(new Container
        {
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[300]),
            CornerRadius = 6,
            Padding = new Thickness(12),
            Child = signatories
        });
        doc.Add(new PreventPageBreak { Child = signaturePanel });

        // ── Schedule 1 ──
        doc.Add(new PageBreak());
        doc.Add(new Heading("Schedule 1: contacts and handover")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 8)
        });

        // ShowEntire never splits its child; use it only for content shorter than a page.
        doc.Add(Note("Never split: a bounded panel, shown whole."));
        VerticalStack contacts = new() { Spacing = 6 };
        foreach (var (role, name, email) in Contacts)
        {
            HorizontalStack line = new() { WidthDistribution = WidthDistribution.Weighted };
            line.Add(new TextBlock(role) { FontName = StandardFonts.HelveticaBold, Color = ColorPalette.Slate[900] });
            line.Add(new TextBlock($"{name}  ·  {email}") { Alignment = TextAlignment.Right });
            contacts.Add(line);
        }
        doc.Add(new ShowEntire
        {
            Child = new Container
            {
                BackgroundColor = ColorPalette.Teal[50],
                CornerRadius = 6,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 4, 0, 14),
                Child = contacts
            }
        });

        // PreventPageBreak lets content taller than the space left continue onto the next page.
        doc.Add(Note("Continues across pages: a checklist longer than the space left carries on overleaf."));
        VerticalStack checklist = new() { Margin = new Thickness(0, 4, 0, 0) };
        for (int i = 0; i < Checklist.Length; i++)
        {
            checklist.Add(new Container
            {
                Border = new Border(0, 0, 0, 0.5f),
                BorderColors = new BorderColors(ColorPalette.Slate[200]),
                Padding = new Thickness(0, 6, 0, 6),
                Child = new TextBlock($"{i + 1:00}   {Checklist[i]}")
            });
        }
        doc.Add(new PreventPageBreak { Child = checklist });
        return doc;
    }

    /// <summary>A small teal note that marks where a page-break rule applies.</summary>
    private static TextBlock Note(string text) => new(text) { FontSize = 7.5f, Color = ColorPalette.Teal[700] };

    /// <summary>One party's signature lines.</summary>
    private static VerticalStack Signatory(string label)
    {
        VerticalStack block = new() { Spacing = 12 };
        block.Add(new TextBlock(label) { FontName = StandardFonts.HelveticaBold, FontSize = 9, Color = ColorPalette.Slate[900] });
        block.Add(new TextBlock("Signature  ______________________") { FontSize = 8.5f });
        block.Add(new TextBlock("Name  __________________________") { FontSize = 8.5f });
        block.Add(new TextBlock("Date  ___________________________") { FontSize = 8.5f });
        return block;
    }
}
