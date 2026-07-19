using BonniePDF.Elements.Text;
using Xunit;

namespace BonniePDF.Tests.Document;

/// <summary>
/// Raw break characters inside RichText span text (shared conformance
/// behaviour — the TypeScript sibling asserts the same expectations in
/// richtext-linebreaks.test.ts). A <c>\n</c> in span text follows the same
/// normative string rule as TextBlock: CRLF/CR normalise to LF, N breaks
/// produce N+1 segments, and an empty segment (including a paragraph-final
/// one) yields one blank line. The structural API is unchanged:
/// <c>Line("a")</c> remains exactly one line.
/// </summary>
public class RichTextLineBreakTests
{
    // Default style is Helvetica 12pt; line height is tallest-font * 1.2.
    private const float LineHeight = 12f * 1.2f; // 14.4

    [Fact]
    public void SplitsSpanOnNewlineIntoSeparateLines()
    {
        Assert.Equal(2 * LineHeight, new RichText().Span("a\nb").MeasureHeight(500), 3);
        Assert.Equal(3 * LineHeight, new RichText().Span("a\nb\nc").MeasureHeight(500), 3);
    }

    [Fact]
    public void PreservesBlankLinesFromConsecutiveBreaks()
    {
        // "a\n\nb" => a, blank, b
        Assert.Equal(3 * LineHeight, new RichText().Span("a\n\nb").MeasureHeight(500), 3);
    }

    [Fact]
    public void ParagraphFinalNewlineYieldsTrailingBlankLine()
    {
        Assert.Equal(2 * LineHeight, new RichText().Span("a\n").MeasureHeight(500), 3);
    }

    [Fact]
    public void StructuralApiUnchangedLineStaysOneLinePerCall()
    {
        Assert.Equal(LineHeight, new RichText().Line("a").MeasureHeight(500), 3);
        Assert.Equal(2 * LineHeight, new RichText().Line("a").Line("b").MeasureHeight(500), 3);
    }

    [Fact]
    public void NormalisesCrlfAndBareCrToSingleBreak()
    {
        Assert.Equal(2 * LineHeight, new RichText().Span("a\r\nb").MeasureHeight(500), 3);
        Assert.Equal(2 * LineHeight, new RichText().Span("a\rb").MeasureHeight(500), 3);
    }

    [Fact]
    public void BreakInOneSpanLetsFollowingSpanContinueOnNewLine()
    {
        // "a\n" + "b" => two lines, no trailing blank (the paragraph does
        // not end at the break).
        Assert.Equal(2 * LineHeight, new RichText().Span("a\n").Span("b").MeasureHeight(500), 3);
    }

    [Fact]
    public void NewlineOnlySpanActsAsPureBreakBetweenSpans()
    {
        Assert.Equal(2 * LineHeight, new RichText().Span("a").Span("\n").Span("b").MeasureHeight(500), 3);
    }

    [Fact]
    public void DropsSpacesAroundTheBreak()
    {
        // Trailing space before the break and leading space after it do not
        // create extra width or lines.
        Assert.Equal(2 * LineHeight, new RichText().Span("a \n b").MeasureHeight(500), 3);
    }

    [Fact]
    public void WordWrapsWithinEachSegmentIndependently()
    {
        // Segment "aaaa bbbb" is forced to wrap; "cccc" is its own hard line.
        const float oneWordWidth = 40; // wide enough for one 4-char word at 12pt, not two
        Assert.Equal(3 * LineHeight, new RichText().Span("aaaa bbbb\ncccc").MeasureHeight(oneWordWidth), 3);
    }
}
