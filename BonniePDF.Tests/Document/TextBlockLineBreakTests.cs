using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Document;

/// <summary>
/// Hard line breaks in TextBlock text (shared conformance behaviour — the
/// TypeScript sibling asserts the same line counts in
/// textblock-linebreaks.test.ts). Height is lines.Count * lineHeight, so
/// MeasureHeight at a generous width is a direct proxy for the number of
/// laid-out lines.
/// </summary>
public class TextBlockLineBreakTests
{
    // TextBlock defaults to Helvetica 12pt; line height is fontSize * 1.2.
    private const float LineHeight = 12f * 1.2f; // 14.4
    private const float Wide = 5000f;            // wide enough that no soft-wrapping occurs

    private static float W(string text) => FontMetrics.GetStringWidth(text, "Helvetica", 12);

    [Fact]
    public void SplitsOnNewlineIntoSeparateLines()
    {
        Assert.Equal(3 * LineHeight, new TextBlock("a\nb\nc").MeasureHeight(Wide), 3);
    }

    [Fact]
    public void PreservesBlankLinesFromConsecutiveBreaks()
    {
        // "a\n\nb" => ["a", "", "b"]
        Assert.Equal(3 * LineHeight, new TextBlock("a\n\nb").MeasureHeight(Wide), 3);
    }

    [Fact]
    public void KeepsTrailingBlankLineForTrailingBreak()
    {
        // "a\n" => ["a", ""]
        Assert.Equal(2 * LineHeight, new TextBlock("a\n").MeasureHeight(Wide), 3);
    }

    [Fact]
    public void NormalisesCrlfAndBareCrToSingleBreak()
    {
        Assert.Equal(2 * LineHeight, new TextBlock("a\r\nb").MeasureHeight(Wide), 3);
        Assert.Equal(2 * LineHeight, new TextBlock("a\rb").MeasureHeight(Wide), 3);
    }

    [Fact]
    public void LeavesBreakFreeTextWrappingUnchanged()
    {
        Assert.Equal(LineHeight, new TextBlock("hello").MeasureHeight(Wide), 3);
    }

    [Fact]
    public void WordWrapsWithinEachHardBreakSegment()
    {
        const string two = "aaaa bbbb";
        float width = W(two) - 1; // too narrow for both words on one line
        // Segment 1 wraps to 2 lines, segment 2 ("cccc") is 1 line => 3 total.
        Assert.Equal(3 * LineHeight, new TextBlock($"{two}\ncccc").MeasureHeight(width), 3);
    }

    [Fact]
    public void EmptyStringOccupiesNoLines()
    {
        Assert.Equal(0f, new TextBlock("").MeasureHeight(Wide), 3);
    }
}
