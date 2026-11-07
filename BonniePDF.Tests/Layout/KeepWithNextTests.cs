using Xunit;

namespace BonniePDF.Tests.Layout;

public sealed class KeepWithNextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovesAHeadingWithTheStartOfALongerParagraph(bool nested)
    {
        using var doc = new BonniePDF.Document(new DocumentSettings(new PdfSize(180, 180), 15));
        var flow = new VerticalStack();
        IElement[] children =
        [
            new Spacer(126),
            new Heading("Next section"),
            new TextBlock(Enumerable.Range(0, 12).Select(i => $"Body line {i}"))
        ];
        foreach (IElement child in children)
        {
            if (nested) flow.Add(child);
            else doc.Add(child);
        }
        if (nested) doc.Add(flow);

        var pages = doc.GenerateSvgPages();
        Assert.DoesNotContain("Next section", pages[0]);
        Assert.Contains("Next section", pages[1]);
        Assert.Contains("Body line 0", pages[1]);
        Assert.Contains("Body line 11", pages[2]);
    }
}
