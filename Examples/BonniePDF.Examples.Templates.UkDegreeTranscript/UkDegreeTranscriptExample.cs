using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.UkDegreeTranscript;

/// <summary>
/// Fictional UK university degree transcript with institution header, student
/// details, honours classification banner, module register, and QR verification.
/// </summary>
[System.ComponentModel.DisplayName("UK Degree Transcript")]
[System.ComponentModel.Category("Education")]
[System.ComponentModel.Description("Official-style fictional UK university degree transcript with honours classification, FHEQ module register, credits, marks, and QR verification link. PDF/A-2a + PDF/UA-1.")]
public static class UkDegreeTranscriptExample
{
    private static readonly Color Navy = ColorPalette.Blue[900];
    private static readonly Color Ink = ColorPalette.Slate[700];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Rule = ColorPalette.Slate[200];
    private static readonly Color Gold = ColorPalette.Amber[500];

    private static readonly Module[] Modules =
    [
        new(4, "CS4101", "Programming Fundamentals", 20, 68),
        new(4, "CS4102", "Discrete Mathematics", 20, 61),
        new(4, "CS4103", "Computer Systems", 20, 57),
        new(4, "CS4104", "Web Technologies", 20, 72),
        new(4, "CS4105", "Databases", 20, 64),
        new(4, "CS4106", "Professional Skills", 20, 66),
        new(5, "CS5201", "Algorithms & Data Structures", 20, 63),
        new(5, "CS5202", "Software Engineering", 20, 71),
        new(5, "CS5203", "Operating Systems", 20, 59),
        new(5, "CS5204", "Human-Computer Interaction", 20, 74),
        new(5, "CS5205", "Networks & Security", 20, 62),
        new(5, "CS5206", "Group Project", 20, 69),
        new(6, "CS6301", "Machine Learning", 20, 66),
        new(6, "CS6302", "Distributed Systems", 20, 61),
        new(6, "CS6303", "Advanced Databases", 20, 58),
        new(6, "CS6304", "Individual Dissertation", 40, 71),
        new(6, "CS6305", "Ethics & Society in Computing", 20, 67),
    ];

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, 50)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"University of Wessexbridge  -  Transcript 20458812  -  Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 8,
                TextColor = Muted,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = "Academic Transcript - Alexandra R. Bennett";
        doc.Metadata.Author = "University of Wessexbridge";
        doc.Metadata.Subject = "Official degree transcript with UK honours classification (fictional).";

        doc.Add(Header());
        doc.Add(BlueRule());
        doc.Add(StudentDetails());
        doc.Add(AwardBanner());
        doc.Add(H2("Module results"));
        doc.Add(ModuleTable());
        doc.Add(Verification());

        return doc;
    }

    private static Grid Header()
    {
        Grid header = new(2.4f, 1) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 0, 0, 6) };

        VerticalStack uni = new();
        uni.AddChild(new TextBlock { Text = "UNIVERSITY OF WESSEXBRIDGE", FontName = StandardFonts.HelveticaBold, FontSize = 16, TextColor = Navy });
        uni.AddChild(new TextBlock { Text = "Founded 1887 - Registrar's Office", FontSize = 9, TextColor = Muted, Margin = new Thickness(0, 2, 0, 0) });

        header.AddRow(
            uni,
            new Container
            {
                Child = new TextBlock
                {
                    Text = "ACADEMIC TRANSCRIPT",
                    FontName = StandardFonts.HelveticaBold,
                    FontSize = 12,
                    TextColor = Gold,
                    Alignment = TextAlignment.Right,
                },
            });
        return header;
    }

    private static Container BlueRule() => new()
    {
        BackgroundColor = Navy,
        Padding = new Thickness(0, 1.5f, 0, 0),
        Margin = new Thickness(0, 0, 0, 16),
        Child = new TextBlock { Text = " ", FontSize = 1 },
    };

    private static Grid StudentDetails()
    {
        Grid details = new(1, 1) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 0, 0, 18) };
        VerticalStack left = new();
        left.AddChild(Kv("Student", "Alexandra R. Bennett"));
        left.AddChild(Kv("Student ID", "20458812"));
        left.AddChild(Kv("Date of birth", "14 March 2003"));

        VerticalStack right = new();
        right.AddChild(Kv("Programme", "BSc (Hons) Computer Science"));
        right.AddChild(Kv("Awarding body", "University of Wessexbridge"));
        right.AddChild(Kv("Date conferred", "15 July 2026"));

        details.AddRow(left, right);
        return details;
    }

    private static Container AwardBanner()
    {
        int total = Modules.Sum(m => m.Credits);
        double weighted = Modules.Where(m => m.Level >= 5).Sum(m => m.Mark * (m.Level == 6 ? 2 : 1) * m.Credits) /
                          (double)Modules.Where(m => m.Level >= 5).Sum(m => (m.Level == 6 ? 2 : 1) * m.Credits);

        VerticalStack stack = new();
        stack.AddChild(new TextBlock { Text = "AWARD", FontName = StandardFonts.HelveticaBold, FontSize = 8, TextColor = Muted });
        stack.AddChild(new TextBlock
        {
            Text = "Upper Second-Class Honours (2:1)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = Navy,
            Margin = new Thickness(0, 2, 0, 2),
        });
        stack.AddChild(new TextBlock
        {
            Text = $"Weighted average {weighted.ToString("0.0", CultureInfo.InvariantCulture)}%  -  {total} credits  -  FHEQ Level 6",
            FontSize = 9,
            TextColor = Ink,
        });

        return new Container
        {
            BackgroundColor = ColorPalette.Blue[50],
            Border = Border.LeftOnly(5),
            BorderColors = BorderColors.LeftOnly(Navy),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 20),
            Child = stack,
        };
    }

    private static Table ModuleTable()
    {
        Table table = new(0.7f, 1, 3, 0.9f, 0.9f)
        {
            CellPadding = 7,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Navy,
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 4, 0, 18),
        };
        table.AddHeader(Th("Level", TextAlignment.Left), Th("Code", TextAlignment.Left), Th("Module", TextAlignment.Left), Th("Credits", TextAlignment.Right), Th("Mark", TextAlignment.Right));

        foreach (Module module in Modules)
        {
            table.AddRow(
                Td(module.Level.ToString(CultureInfo.InvariantCulture), TextAlignment.Left, Muted),
                Td(module.Code, TextAlignment.Left, Ink),
                Td(module.Title, TextAlignment.Left, Ink),
                Td(module.Credits.ToString(CultureInfo.InvariantCulture), TextAlignment.Right, Ink),
                Td(module.Mark.ToString(CultureInfo.InvariantCulture) + "%", TextAlignment.Right, module.Mark >= 70 ? ColorPalette.Emerald[600] : Ink));
        }

        return table;
    }

    private static HorizontalStack Verification()
    {
        VerticalStack text = new();
        text.AddChild(H2("Verification"));
        text.AddChild(Body(
            "This is an official record issued by the University of Wessexbridge. Scan the code or visit " +
            "verify.wessexbridge.ac.uk and enter transcript reference 20458812 to confirm authenticity. " +
            "The grading scheme is defined in the University Academic Regulations."));

        QRCode qr = new()
        {
            Data = "https://verify.wessexbridge.ac.uk/t/20458812",
            Size = 92,
            QuietZone = 1,
            ErrorCorrection = QrErrorCorrection.Quartile,
            AltText = "QR code to verify transcript reference 20458812.",
        };

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.AddChild(text, 3);
        row.AddChild(new Container { Child = qr, HorizontalAlignment = HorizontalAlignment.End }, 1);
        return row;
    }

    private static Heading H2(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        TextColor = Navy,
        Margin = new Thickness(0, 4, 0, 8),
    };

    private static TextBlock Body(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 9.5f,
        TextColor = Ink,
        Alignment = TextAlignment.Justify,
        Margin = new Thickness(0, 0, 12, 0),
    };

    private static HorizontalStack Kv(string label, string value)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 0, 0, 5) };
        row.AddChild(new TextBlock { Text = label, FontName = StandardFonts.HelveticaBold, FontSize = 9, TextColor = Muted }, 2);
        row.AddChild(new TextBlock { Text = value, FontSize = 10, TextColor = Ink }, 3);
        return row;
    }

    private static TextBlock Th(string text, TextAlignment align) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 9,
        TextColor = Color.White,
        Alignment = align,
    };

    private static TextBlock Td(string text, TextAlignment align, Color color) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 9.5f,
        TextColor = color,
        Alignment = align,
    };

    private sealed record Module(int Level, string Code, string Title, int Credits, int Mark);
}
