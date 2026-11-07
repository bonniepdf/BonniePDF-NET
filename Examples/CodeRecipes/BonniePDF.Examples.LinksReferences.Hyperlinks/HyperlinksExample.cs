using BonniePDF;

namespace BonniePDF.Examples.LinksReferences.Hyperlinks;

/// <summary>
/// Hyperlinks as readers meet them: a resources page with web links and descriptions,
/// a button-style link, and email and phone links, all in one link colour. Any URI
/// scheme works, so <c>mailto:</c> and <c>tel:</c> links open the mail app or dialler.
/// PDF/A-2a + PDF/UA-1; every link is tagged for screen readers.
/// </summary>
[System.ComponentModel.DisplayName("Hyperlinks")]
[System.ComponentModel.Category("Links & References")]
[System.ComponentModel.Description("A resources page: web links with descriptions, an underlined link inside a sentence, a button-style link, and email and phone links, all in one link colour.")]
public static class HyperlinksExample
{
    /// <summary>One colour for every link, so readers learn what is clickable.</summary>
    private static readonly Color Link = ColorPalette.Blue[700];

    private static readonly (string Title, string Uri, string Description)[] Resources =
    [
        ("Getting started guide", "https://www.bonniepdf.dev/docs", "Install the library and build a first document in a few minutes."),
        ("PDF Association", "https://pdfa.org", "The industry body behind the PDF/A and PDF/UA standards."),
        ("veraPDF", "https://verapdf.org", "The open-source validator for PDF/A and PDF/UA conformance."),
        ("W3C Web Accessibility Initiative", "https://www.w3.org/WAI/", "Guidance on writing link text that makes sense out of context."),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new();
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);
        doc.Settings.DefaultZoom = ZoomLevel.FitPage;
        doc.Settings.DisplayDocTitle = true;

        doc.Metadata.Title = "Hyperlink Examples";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrating hyperlink support in PDFs";

        doc.Add(new Heading("Useful resources")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 26,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 6)
        });
        doc.Add(new TextBlock("Every blue line on this page is a link. Each one names where it goes, so it makes sense read on its own.")
        {
            Margin = new Thickness(0, 0, 0, 24)
        });

        // Web links, each with a one-line description.
        doc.Add(SectionHeading("Read more"));
        foreach (var (title, uri, description) in Resources)
        {
            VerticalStack stack = new() { Spacing = 3 };
            // .NET constructor: Hyperlink(uri, text) - TypeScript takes (text, uri).
            stack.Add(new Hyperlink(uri, title) { FontName = StandardFonts.HelveticaBold, FontSize = 12, Color = Link });
            stack.Add(new TextBlock(description) { FontSize = 10 });
            stack.Add(new TextBlock(uri) { FontSize = 9, Color = ColorPalette.Slate[500] });

            doc.Add(new Container
            {
                Border = new Border(0, 0, 0, 1),
                BorderColors = new BorderColors(ColorPalette.Slate[200]),
                Padding = new Thickness(0, 10, 0, 10),
                Child = stack
            });
        }

        // A link inside a sentence: a RichText span with a URI, styled as the link's look.
        doc.Add(SectionHeading("Links in a sentence", 28));
        doc.Add(new RichText { DefaultStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]) }
            .Span("Orders are accepted on our ")
            .Link("terms of business", "https://www.bonniepdf.dev/legal", new TextStyle().WithColor(Link).Underline())
            .Span(", which set out delivery times, returns and how we handle your data. The link sits in the sentence and wraps with it."));

        // A button-style link: a Hyperlink inside a filled, rounded container.
        doc.Add(SectionHeading("Start a trial", 28));
        doc.Add(new TextBlock("A call to action reads as a button when the link sits in a filled shape.")
        {
            Margin = new Thickness(0, 0, 0, 10)
        });
        doc.Add(new Container
        {
            Width = 200,
            BackgroundColor = Link,
            CornerRadius = 6,
            Padding = new Thickness(18, 10, 18, 10),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new Hyperlink("https://www.bonniepdf.dev/pricing", "Download the trial")
            {
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                Color = Color.White
            }
        });

        // Email and phone links.
        doc.Add(SectionHeading("Get in touch", 28));
        HorizontalStack contacts = new() { Spacing = 16, WidthDistribution = WidthDistribution.Equal };
        contacts.Add(ContactCard("Email", "support@example.com", "mailto:support@example.com", "Opens your mail app with the address filled in."));
        contacts.Add(ContactCard("Phone", "0117 496 0000", "tel:+441174960000", "Rings the number on a phone; desktop viewers ask first."));
        doc.Add(contacts);

        return doc;
    }

    /// <summary>A level-2 heading with the shared style used across this example.</summary>
    private static Heading SectionHeading(string text, float marginTop = 0) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 15,
        Color = ColorPalette.Slate[900],
        Margin = new Thickness(0, marginTop, 0, 4)
    };

    /// <summary>A tinted card holding one contact link.</summary>
    private static Container ContactCard(string label, string text, string uri, string note)
    {
        VerticalStack stack = new() { Spacing = 4 };
        stack.Add(new TextBlock(label.ToUpperInvariant())
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            LetterSpacing = 1.2f,
            Color = ColorPalette.Slate[600]
        });
        stack.Add(new Hyperlink(uri, text) { FontSize = 13, Color = Link });
        stack.Add(new TextBlock(note) { FontSize = 9 });

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[100],
            CornerRadius = 8,
            Padding = new Thickness(14),
            Child = stack
        };
    }
}
