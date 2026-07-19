using System.Collections.Generic;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Export;

/// <summary>
/// The SVG export path runs a separate pipeline from the PDF serializer and so
/// needs its own two-pass TOC resolution. With a <see cref="VisualToc"/> placed
/// before the headings it references (the Dickens-example layout), a single pass
/// would draw the "XX" placeholder for every page number in the preview.
/// </summary>
[Collection("Serial")]
public class SvgTocResolutionTests
{
    private static IReadOnlyList<string> BuildSvgPages()
    {
        using var doc = new BonniePDF.Document();

        var toc = new VisualToc { FontName = StandardFonts.Helvetica, FontSize = 12 };
        toc.HeadingIdentifiers.Add("ch1");
        toc.HeadingIdentifiers.Add("ch2");
        toc.HeadingIdentifiers.Add("ch3");
        doc.Add(toc);
        doc.Add(new PageBreak());

        doc.Add(new Heading("Chapter One") { TocIdentifier = "ch1", FontSize = 20 });
        doc.Add(new TextBlock { Text = "Body one.", FontSize = 12 });
        doc.Add(new PageBreak());
        doc.Add(new Heading("Chapter Two") { TocIdentifier = "ch2", FontSize = 20 });
        doc.Add(new TextBlock { Text = "Body two.", FontSize = 12 });
        doc.Add(new PageBreak());
        doc.Add(new Heading("Chapter Three") { TocIdentifier = "ch3", FontSize = 20 });
        doc.Add(new TextBlock { Text = "Body three.", FontSize = 12 });

        return doc.GenerateSvgPages();
    }

    [Fact]
    public void TocPageShowsResolvedNumbersNotPlaceholder()
    {
        IReadOnlyList<string> pages = BuildSvgPages();
        string tocPage = pages[0];
        Assert.DoesNotContain("XX", tocPage);
        // Chapters land on pages 2, 3, 4 - those digits should be drawn on the TOC page.
        Assert.Contains(">2<", tocPage);
        Assert.Contains(">3<", tocPage);
        Assert.Contains(">4<", tocPage);
    }
}
