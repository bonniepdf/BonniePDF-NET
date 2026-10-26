using BonniePDF;
namespace BonniePDF.Examples.PageSetup.HeadersFooters;

/// <summary>Declarative first-page, odd/even headers and automatic page numbers.</summary>
public static class HeadersFootersExample
{
    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings
        {
            Language = "en-GB",
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Margin = 50,
            HeaderTemplate = PageTemplate.FromElements(firstPage: Label("NORTHFIELD · FIRST PAGE"), odd: Label("NORTHFIELD · ODD PAGE"), even: Label("NORTHFIELD · EVEN PAGE")),
            FooterTemplate = PageTemplate.FromElements(@default: Label("Page {{CurrentPage}} of {{TotalPages}}"))
        })
        { Metadata = new DocumentMetadata { Title = "Headers and footers" } };
        (string Title, string Body)[] sections = [
            ("First-page branding", "The firstPage element overrides the odd-page header on page one. Use it for a cover-specific brand or document title."),
            ("Even-page header", "The even element applies to page two. The footer uses the current-page and total-pages tokens, resolved after layout."),
            ("Odd-page header", "The odd element applies to page three. With no matching firstPage or parity element, default is used; without a default, the slot is empty.")
        ];
        for (int i = 0; i < sections.Length; i++)
        {
            if (i != 0) doc.Add(new PageBreak());
            doc.Add(new Heading(sections[i].Title) { Level = i == 0 ? 1 : 2, FontSize = 24, Margin = new Thickness(0, 0, 0, 16) });
            doc.Add(new TextBlock(sections[i].Body) { FontSize = 12 });
        }
        return doc;
    }
    private static TextBlock Label(string text) => new(text) { FontSize = 9, Alignment = TextAlignment.Center };
}
