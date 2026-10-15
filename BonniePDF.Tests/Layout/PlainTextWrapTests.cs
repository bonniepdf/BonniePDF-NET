using Xunit;

namespace BonniePDF.Tests.Layout;

// TextBlock wraps plain text without going through WrapSegment when the type has not replaced
// it. These pin that a subclass that does replace it is still asked.
public sealed class PlainTextWrapTests
{
    private sealed class OneWordPerLine : TextBlock
    {
        protected override IList<string> WrapSegment(string text, float maxWidth) => text.Split(' ');
    }

    [Fact]
    public void ASubclassWrapSegmentIsUsed()
    {
        var block = new OneWordPerLine { Text = "one two three" };
        Assert.Equal(["one", "two", "three"], block.LinesAt(1000f));
    }

    [Fact]
    public void PlainTextWrapsAsBefore()
    {
        var block = new TextBlock { Text = "one two three", FontSize = 10 };
        Assert.Equal(["one two three"], block.LinesAt(1000f));
        // A width that holds one word per line.
        Assert.Equal(["one", "two", "three"], new TextBlock { Text = "one two three", FontSize = 10 }.LinesAt(25f));
        // Text that wraps to nothing is one blank line.
        Assert.Equal([""], new TextBlock { Text = " ", FontSize = 10 }.LinesAt(1000f));
    }
}
