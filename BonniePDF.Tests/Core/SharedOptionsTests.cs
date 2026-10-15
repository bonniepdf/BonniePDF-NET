using System.Reflection;
using Xunit;

namespace BonniePDF.Tests.Core;

/// <summary>
/// Elements styled alike share one immutable options object. Sharing must never let a change to
/// one element reach another, and must keep values exactly (by bits) as they were set.
/// </summary>
public class SharedOptionsTests
{
    private static object TextOptionsOf(TextElementBase element) =>
        typeof(TextElementBase).GetField("_text", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(element)!;

    [Fact]
    public void ElementsStyledAlikeShareOptions()
    {
        var a = new TextBlock("one") { FontSize = 10, Color = Color.Red, Alignment = TextAlignment.Right };
        var b = new TextBlock("two") { FontSize = 10, Color = Color.Red, Alignment = TextAlignment.Right };
        Assert.Same(TextOptionsOf(a), TextOptionsOf(b));

        // The same values reached in another order are still the same options.
        var c = new TextBlock("three") { Alignment = TextAlignment.Right, Color = Color.Red, FontSize = 10 };
        Assert.Same(TextOptionsOf(a), TextOptionsOf(c));
    }

    [Fact]
    public void ChangingOneElementLeavesOthersUnchanged()
    {
        var a = new TextBlock("one") { FontSize = 10, LetterSpacing = 1 };
        var b = new TextBlock("two") { FontSize = 10, LetterSpacing = 1 };
        b.FontSize = 14;
        b.MaxLines = 2;
        Assert.Equal(10, a.FontSize);
        Assert.Null(a.MaxLines);
        Assert.Equal(1, a.LetterSpacing);
        Assert.Equal(14, b.FontSize);

        var cellA = new TableCell { Content = new TextBlock("c"), Padding = new Thickness(2) };
        var cellB = new TableCell { Content = new TextBlock("c"), Padding = new Thickness(2) };
        cellB.Padding = new Thickness(5);
        Assert.Equal(2, cellA.Padding!.Value.Left);

        var boxA = new Container { Margin = new Thickness(3), BackgroundColor = Color.Blue };
        var boxB = new Container { Margin = new Thickness(3), BackgroundColor = Color.Blue };
        boxB.Margin = new Thickness(4);
        boxB.BackgroundColor = null;
        Assert.Equal(3, boxA.Margin.Left);
        Assert.Equal(Color.Blue, boxA.BackgroundColor);
    }

    [Fact]
    public void ValuesKeepTheirExactBits()
    {
        var positive = new TextBlock("a") { LetterSpacing = 0f };
        var negative = new TextBlock("b") { LetterSpacing = -0f };
        Assert.True(float.IsNegative(negative.LetterSpacing));
        Assert.False(float.IsNegative(positive.LetterSpacing));
        Assert.NotSame(TextOptionsOf(positive), TextOptionsOf(negative));

        // A remembered change from +0 must not answer a change to -0.
        var again = new TextBlock("c") { LetterSpacing = -0f };
        Assert.True(float.IsNegative(again.LetterSpacing));
    }

    [Fact]
    public void ConcurrentElementsGetTheirOwnValues()
    {
        Parallel.For(0, 2000, i =>
        {
            float size = 6 + i % 37;
            var block = new TextBlock("x") { FontSize = size, Alignment = (TextAlignment)(i % 3) };
            block.LetterSpacing = i % 5;
            Assert.Equal(size, block.FontSize);
            Assert.Equal((TextAlignment)(i % 3), block.Alignment);
            Assert.Equal(i % 5, block.LetterSpacing);
        });
    }
}
