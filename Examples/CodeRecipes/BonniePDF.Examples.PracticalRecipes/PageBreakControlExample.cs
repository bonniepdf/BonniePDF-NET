using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>Keep headings and signature blocks together without losing oversized content.</summary>
public static class PageBreakControlExample
{
    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings { PageSize = PdfSize.A5, Margin = 36, Language = "en-GB",
                FooterTemplate = PageTemplate.FromElements(@default: new TextBlock("Page {{CurrentPage}} of {{TotalPages}}") { FontSize = 9 }) }) { Metadata = new DocumentMetadata { Title = "Control page breaks" } };
        doc.Add(new Heading("Control page breaks") { Level = 1, FontSize = 23 });
        doc.Add(new TextBlock("Keep a heading with its first paragraph, and move a signature block onto the next page as a unit."));
        doc.Add(new Spacer(340));
        doc.Add(new EnsureSpace { MinHeight = 140, Child = new Heading("Approval") { Level = 2, FontSize = 16 } });
        doc.Add(new TextBlock("EnsureSpace reserves enough room to start this section. It does not keep the whole section together."));
        doc.Add(new PreventPageBreak { Child = Signature("PreventPageBreak keeps this block together.") });
        doc.Add(new PageBreak());
        doc.Add(new Heading("An indivisible panel") { Level = 2, FontSize = 16 });
        doc.Add(new ShowEntire { Child = Signature("ShowEntire never splits its child.") });
        doc.Add(new TextBlock("Use ShowEntire only for bounded content: a child taller than a page is cut off. PreventPageBreak lets oversized content continue across pages."));
        var checklist = new VerticalStack();
        for (int i = 0; i < 28; i++) checklist.Add(new TextBlock($"Checklist {i + 1}: retain this line even when the group exceeds a page.") { Margin = new Thickness(0, 0, 0, 8) });
        doc.Add(new PreventPageBreak { Child = checklist });
        return doc;
    }

    private static VerticalStack Signature(string label)
    {
        var block = new VerticalStack { Spacing = 10, Margin = new Thickness(0, 12, 0, 12) };
        block.Add(new TextBlock(label));
        block.Add(new TextBlock("Approved by: ____________________"));
        block.Add(new TextBlock("Date: ____________________"));
        return block;
    }
}
